<Query Kind="Program" />

void Main()
{
	var scriptDir = Path.GetDirectoryName(Util.CurrentQueryPath);
	var homeDir = Path.Combine(scriptDir, "PoeEye");
	var toolsDir = Path.Combine(scriptDir, "Tools");
	var grPath = Path.Combine(toolsDir, "github-release.exe");

	Environment.SetEnvironmentVariable("GITHUB_TOKEN", "ff9f308bf12ef18962ce1f698b8d5073c9190c7b");
	new { scriptDir, grPath }.Dump();

	Util.Cmd(grPath, $"", false).Dump();
}
