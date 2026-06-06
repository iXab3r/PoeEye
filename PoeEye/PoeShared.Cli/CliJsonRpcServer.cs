using System.Text.Json;
using System.Text.Json.Serialization;

namespace PoeShared.Cli;

internal sealed class CliJsonRpcServer<TAppOptions>
    where TAppOptions : class, new()
{
    private readonly CliApplication<TAppOptions> _application;
    private readonly TextReader _input;
    private readonly TextWriter _output;
    private readonly string[] _inheritedArgs;

    public CliJsonRpcServer(
        CliApplication<TAppOptions> application,
        TextReader input,
        TextWriter output,
        string[] inheritedArgs)
    {
        _application = application;
        _input = input;
        _output = output;
        _inheritedArgs = inheritedArgs;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await _input.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var response = await HandleLineAsync(line, cancellationToken);
            await _output.WriteLineAsync(JsonSerializer.Serialize(response, CliJson.Options));
        }
    }

    private async Task<CliJsonRpcResponse> HandleLineAsync(
        string line,
        CancellationToken cancellationToken)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(line);
        }
        catch (JsonException exception)
        {
            return CliJsonRpcResponse.Fail(null, -32700, "Parse error.", exception.Message);
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return CliJsonRpcResponse.Fail(null, -32600, "Invalid Request.", "Request must be a JSON object.");
            }

            var id = root.TryGetProperty("id", out var idElement) ? idElement.Clone() : (JsonElement?) null;

            if (!root.TryGetProperty("jsonrpc", out var versionElement) ||
                versionElement.GetString() != "2.0")
            {
                return CliJsonRpcResponse.Fail(id, -32600, "Invalid Request.", "jsonrpc must be \"2.0\".");
            }

            if (!root.TryGetProperty("method", out var methodElement) ||
                methodElement.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(methodElement.GetString()))
            {
                return CliJsonRpcResponse.Fail(id, -32600, "Invalid Request.", "method is required.");
            }

            string[] commandArgs;
            try
            {
                var paramsElement = root.TryGetProperty("params", out var value) ? value : (JsonElement?) null;
                commandArgs = CliJsonRpcArgsBuilder.Build(methodElement.GetString()!, paramsElement);
            }
            catch (InvalidOperationException exception)
            {
                return CliJsonRpcResponse.Fail(id, -32602, "Invalid params.", exception.Message);
            }

            var result = await _application.InvokeForResultAsync(
                [.. _inheritedArgs, .. commandArgs],
                cancellationToken);
            if (!result.Success)
            {
                return CliJsonRpcResponse.Fail(
                    id,
                    MapErrorCode(result.Error?.Code),
                    result.Error?.Message ?? "Command failed.",
                    result.ToEnvelope());
            }

            return CliJsonRpcResponse.Ok(id, result.ToEnvelope());
        }
    }

    private static int MapErrorCode(string? code)
    {
        return code switch
        {
            "unknownCommand" => -32601,
            "invalidArguments" => -32602,
            "notImplemented" => -32001,
            _ => -32000
        };
    }
}

internal static class CliJsonRpcArgsBuilder
{
    public static string[] Build(string method, JsonElement? paramsElement)
    {
        var args = method
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        if (args.Count == 0)
        {
            throw new InvalidOperationException("method must name a command.");
        }

        if (paramsElement is null || paramsElement.Value.ValueKind == JsonValueKind.Null)
        {
            return args.ToArray();
        }

        var parameters = paramsElement.Value;
        if (parameters.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in parameters.EnumerateObject())
            {
                AppendNamedParameter(args, property);
            }

            return args.ToArray();
        }

        if (parameters.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in parameters.EnumerateArray())
            {
                args.Add(ReadScalar(item));
            }

            return args.ToArray();
        }

        throw new InvalidOperationException("params must be an object, array, or null.");
    }

    private static void AppendNamedParameter(List<string> args, JsonProperty property)
    {
        if (property.Value.ValueKind is JsonValueKind.Null or JsonValueKind.False)
        {
            return;
        }

        var optionName = "--" + ToKebabCase(property.Name);
        if (property.Value.ValueKind == JsonValueKind.True)
        {
            args.Add(optionName);
            return;
        }

        args.Add(optionName);
        args.Add(ReadScalar(property.Value));
    }

    private static string ReadScalar(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => throw new InvalidOperationException("params values must be scalar.")
        };
    }

    private static string ToKebabCase(string value)
    {
        var chars = new List<char>(value.Length + 4);
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (char.IsUpper(c))
            {
                if (i > 0 && value[i - 1] != '-')
                {
                    chars.Add('-');
                }

                chars.Add(char.ToLowerInvariant(c));
                continue;
            }

            chars.Add(c);
        }

        return new string(chars.ToArray());
    }
}

internal sealed record CliJsonRpcResponse(
    [property: JsonPropertyName("jsonrpc")] string JsonRpc,
    [property: JsonPropertyName("id")] JsonElement? Id,
    [property: JsonPropertyName("result")] CliCommandEnvelope? Result,
    [property: JsonPropertyName("error")] CliJsonRpcError? Error)
{
    public static CliJsonRpcResponse Ok(JsonElement? id, CliCommandEnvelope result)
    {
        return new CliJsonRpcResponse("2.0", id, result, null);
    }

    public static CliJsonRpcResponse Fail(JsonElement? id, int code, string message, object? data)
    {
        return new CliJsonRpcResponse("2.0", id, null, new CliJsonRpcError(code, message, data));
    }
}

internal sealed record CliJsonRpcError(
    [property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("data")] object? Data);
