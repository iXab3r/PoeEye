using NUnit.Framework;
using PoeShared.Cli;
using Shouldly;

namespace PoeShared.Cli.Tests;

[TestFixture]
public sealed class CliApplicationFixture
{
    /// <summary>
    /// WHAT: The shared CLI app should expose text help at the root command level.
    /// HOW: Runs a fake CLI with --help and verifies that registered command names appear in stdout.
    /// </summary>
    [Test]
    public async Task Should_print_root_help_with_registered_commands()
    {
        // Given
        var harness = CreateHarness();

        // When
        var exitCode = await harness.RunAsync(["--help"]);

        // Then
        exitCode.ShouldBe(0);
        harness.Output.ToString().ShouldContain("echo");
        harness.Output.ToString().ShouldContain("math");
    }

    /// <summary>
    /// WHAT: The shared CLI app should expose text help for nested command groups.
    /// HOW: Runs a fake CLI with a parent command plus --help and verifies that the child command is listed.
    /// </summary>
    [Test]
    public async Task Should_print_nested_help_with_child_commands()
    {
        // Given
        var harness = CreateHarness();

        // When
        var exitCode = await harness.RunAsync(["math", "--help"]);

        // Then
        exitCode.ShouldBe(0);
        harness.Output.ToString().ShouldContain("add");
    }

    /// <summary>
    /// WHAT: The shared CLI app should serialize command results as the common JSON envelope.
    /// HOW: Runs a fake command with --json and verifies the output contains the envelope and command data.
    /// </summary>
    [Test]
    public async Task Should_emit_json_command_envelope()
    {
        // Given
        var harness = CreateHarness();

        // When
        var exitCode = await harness.RunAsync(["--json", "--tenant", "alpha", "echo", "--message", "hello"]);

        // Then
        exitCode.ShouldBe(0);
        harness.Output.ToString().ShouldContain("\"ok\":true");
        harness.Output.ToString().ShouldContain("\"command\":\"echo\"");
        harness.Output.ToString().ShouldContain("\"tenant\":\"alpha\"");
        harness.Output.ToString().ShouldContain("\"message\":\"hello\"");
    }

    /// <summary>
    /// WHAT: JSON mode should keep command parse failures machine-readable on stdout without stderr noise.
    /// HOW: Runs a fake command with a missing required option and verifies the JSON error envelope and empty stderr.
    /// </summary>
    [Test]
    public async Task Should_emit_json_parse_errors_without_stderr_noise()
    {
        // Given
        var harness = CreateHarness();

        // When
        var exitCode = await harness.RunAsync(["--json", "echo"]);

        // Then
        exitCode.ShouldBe(1);
        harness.Output.ToString().ShouldContain("\"ok\":false");
        harness.Output.ToString().ShouldContain("\"code\":\"invalidArguments\"");
        harness.Output.ToString().ShouldContain("--message");
        harness.Error.ToString().ShouldBeEmpty();
    }

    /// <summary>
    /// WHAT: The shared CLI app should bind positional arguments for leaf commands.
    /// HOW: Runs a fake nested add command with two positional integers and verifies the computed sum.
    /// </summary>
    [Test]
    public async Task Should_bind_positional_arguments()
    {
        // Given
        var harness = CreateHarness();

        // When
        var exitCode = await harness.RunAsync(["--json", "math", "add", "2", "3"]);

        // Then
        exitCode.ShouldBe(0);
        harness.Output.ToString().ShouldContain("\"sum\":5");
    }

    /// <summary>
    /// WHAT: The shared CLI app should expose commands through JSON-RPC stdio mode.
    /// HOW: Starts a fake CLI with --json-rpc and one stdin request, then verifies the JSON-RPC response.
    /// </summary>
    [Test]
    public async Task Should_execute_command_over_json_rpc_stdio()
    {
        // Given
        var harness = CreateHarness(
            """
            {"jsonrpc":"2.0","id":1,"method":"echo","params":{"message":"hello"}}
            
            """);

        // When
        var exitCode = await harness.RunAsync(["--json-rpc", "--tenant", "alpha"]);

        // Then
        exitCode.ShouldBe(0);
        harness.Output.ToString().ShouldContain("\"jsonrpc\":\"2.0\"");
        harness.Output.ToString().ShouldContain("\"result\"");
        harness.Output.ToString().ShouldContain("\"message\":\"hello\"");
    }

    /// <summary>
    /// WHAT: JSON-RPC mode should return command errors as JSON-RPC errors without stderr noise.
    /// HOW: Starts a fake CLI with --json-rpc and a request missing a required command option, then verifies stdout has the error response.
    /// </summary>
    [Test]
    public async Task Should_emit_json_rpc_command_errors_without_stderr_noise()
    {
        // Given
        var harness = CreateHarness(
            """
            {"jsonrpc":"2.0","id":1,"method":"echo","params":{}}

            """);

        // When
        var exitCode = await harness.RunAsync(["--json-rpc"]);

        // Then
        exitCode.ShouldBe(0);
        harness.Output.ToString().ShouldContain("\"jsonrpc\":\"2.0\"");
        harness.Output.ToString().ShouldContain("\"error\"");
        harness.Output.ToString().ShouldContain("\"code\":-32602");
        harness.Error.ToString().ShouldBeEmpty();
    }

    /// <summary>
    /// WHAT: The shared CLI app should execute multiple commands from the interactive loop.
    /// HOW: Starts interactive mode with stdin commands and verifies that command output is produced before quit.
    /// </summary>
    [Test]
    public async Task Should_dispatch_interactive_commands()
    {
        // Given
        var harness = CreateHarness(
            """
            echo --message hello
            quit
            
            """);

        // When
        var exitCode = await harness.RunAsync(["--interactive", "--json"]);

        // Then
        exitCode.ShouldBe(0);
        harness.Output.ToString().ShouldContain("\"message\":\"hello\"");
    }

    private static CliHarness CreateHarness(string input = "")
    {
        var builder = new CliApplicationBuilder<SampleAppOptions>("sample", "Sample CLI.");
        builder.AddGlobalOption<string>("--tenant", "Tenant id.", (options, value) => options.Tenant = value);
        builder.Root.SetHandler((_, _) =>
            Task.FromResult(CliCommandResult.Fail("parse", "missingCommand", "No command was specified.", 1)));
        builder.Root.AddCommand("echo", "Echo a message.", command =>
        {
            command.SetHandler<EchoOptions>(
                options => options.AddOption<string>(
                    "--message",
                    "Message to echo.",
                    (target, value) => target.Message = value,
                    required: true),
                (options, context) => Task.FromResult(CliCommandResult.Ok(
                    "echo",
                    new
                    {
                        context.AppOptions.Tenant,
                        options.Message
                    })));
        });
        builder.Root.AddCommand("math", "Math commands.", math =>
        {
            math.AddCommand("add", "Add two numbers.", add =>
            {
                add.SetHandler<AddOptions>(
                    options =>
                    {
                        options.AddArgument<int>("left", "Left operand.", (target, value) => target.Left = value);
                        options.AddArgument<int>("right", "Right operand.", (target, value) => target.Right = value);
                    },
                    (options, _) => Task.FromResult(CliCommandResult.Ok(
                        "math add",
                        new
                        {
                            Sum = options.Left + options.Right
                        })));
            });
        });

        var app = builder.Build();
        var output = new StringWriter();
        var error = new StringWriter();
        app.UseConsole(new StringReader(input), output, error);
        return new CliHarness(app, output, error);
    }

    private sealed record CliHarness(
        CliApplication<SampleAppOptions> App,
        StringWriter Output,
        StringWriter Error)
    {
        public Task<int> RunAsync(string[] args)
        {
            return App.RunAsync(args);
        }
    }

    private sealed class SampleAppOptions
    {
        public string? Tenant { get; set; }
    }

    private sealed class EchoOptions
    {
        public string? Message { get; set; }
    }

    private sealed class AddOptions
    {
        public int Left { get; set; }

        public int Right { get; set; }
    }
}
