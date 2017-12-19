<Query Kind="Program" />

void Main()
{
	var homeDir = Path.GetDirectoryName(Util.CurrentQueryPath);
	var binariesDir = @"bin\";
	var exeFilePath = Path.Combine(homeDir, binariesDir, "PoeEye.exe");

	new[] { exeFilePath }.Dump("Reading version from .exe file...");

	var versionInfo = FileVersionInfo.GetVersionInfo(exeFilePath);
	var version = $"{versionInfo.FileMajorPart}.{versionInfo.FileMinorPart}.{versionInfo.FileBuildPart}.{versionInfo.FilePrivatePart}";
	
	version.Dump("Version");
	
	var nupkgFileName = $@"PoeEye.{version}.nupkg";
	var nupkgFilePath = Path.Combine(homeDir, nupkgFileName);
	var releasesFolderName = "Releases";
	var squirrelPath = Path.Combine(homeDir, $@"packages\squirrel.windows.1.0.2\tools\Squirrel.exe");

	new { version, homeDir, nupkgFilePath, squirrelPath }.Dump("Running Releasify...");

	Util.Cmd(squirrelPath, $"--releasify={nupkgFilePath}", false);

	var sourceReleasesFolderPath = Path.Combine(Path.GetDirectoryName(squirrelPath), releasesFolderName);
	var targetReleasesFolderPath = Path.Combine(homeDir, releasesFolderName);
	
	if (Directory.Exists(targetReleasesFolderPath)){
		targetReleasesFolderPath.Dump("Target directory exists, removing it");
		Directory.Delete(targetReleasesFolderPath);
	}

	new { sourceReleasesFolderPath, targetReleasesFolderPath }.Dump("Moving 'Releases' folder...");
	Directory.Move(sourceReleasesFolderPath, targetReleasesFolderPath);
}

// Define other methods and classes here