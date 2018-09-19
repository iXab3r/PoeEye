<Query Kind="Program" />

void Main()
{
	var scriptDir = Path.GetDirectoryName(Util.CurrentQueryPath);
	var homeDir = Path.Combine(scriptDir, "PoeEye");
	var toolsDir = Path.Combine(scriptDir, "Tools");
	var grPath = Path.Combine(toolsDir, "github-release.exe");
	var username = "iXab3r";
	var reponame = "PoeEyeReleases";
	var releasesFolderName = "Releases";
	var releasesPath = Path.Combine(scriptDir, releasesFolderName);
	var setupFilePath = Path.Combine(releasesPath, "Setup.exe");

	new { scriptDir, grPath,setupFilePath, username, reponame, GithubTokenIsSet = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GITHUB_TOKEN")) }.Dump("GitHubRelease Arguments");
	
	var versionInfo = FileVersionInfo.GetVersionInfo(setupFilePath);
	var version = $"{versionInfo.FileMajorPart}.{versionInfo.FileMinorPart}.{versionInfo.FileBuildPart}";
	var versionTag = $"v{version}";

	version.Dump("PoeEye version");
	
	$"Preparing release draft {versionTag}".Dump();

	Util.Cmd(grPath, $"release --user {username} --repo {reponame} --tag {versionTag}", false);

	$"Uploading binaries".Dump();
	Util.Cmd(grPath, $"upload --user {username} --repo {reponame} --tag {versionTag} --name \"PoeEyeSetup.{version}.exe\" --file \"{setupFilePath}\" --replace", false);

	var packages = Directory.GetFiles(releasesPath, "*.nupkg");
	packages.Dump("Uploading packages");
	
	foreach (var fileName in packages)
	{
		fileName.Dump("Uploading...");
		Util.Cmd(grPath, $"upload --user {username} --repo {reponame} --tag {versionTag} --file \"{fileName}\" --replace", false);
	}
}
