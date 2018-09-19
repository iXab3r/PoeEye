<Query Kind="Program" />

void Main()
{
	var scriptDir = Path.GetDirectoryName(Util.CurrentQueryPath);
	var homeDir = Path.Combine(scriptDir, "PoeEye");
	var toolsDir = Path.Combine(scriptDir, "Tools");
	var nugetPath = Path.Combine(toolsDir, "nuget.exe");
	
	var nuspecFileName = @"PoeEye.nuspec";
	var nuspecFilePath = Path.Combine(homeDir, nuspecFileName);
	var version = GetSpecVersion(nuspecFilePath);
	
	var nupkgFileName = $@"PoeEye.{version}.nupkg";
	var nupkgFilePath = Path.Combine(scriptDir, nupkgFileName);

	Util.Cmd(nugetPath, $"pack {nuspecFilePath} -OutputDirectory \"{scriptDir}\" -Properties Configuration=Release", false);
}

private static string GetSpecVersion(string nuspecFilePath)
{
	new[] { nuspecFilePath }.Dump("Reading version from .nuspec file...");
	var nuspecDocument = XElement.Load(nuspecFilePath);
	var ns = nuspecDocument.GetDefaultNamespace();

	var version = nuspecDocument.Descendants(ns + "metadata").Single().Descendants(ns + "version").Single().Value;

	return version;
}
