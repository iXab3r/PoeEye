<Query Kind="Program" />

void Main()
{
	var homeDir = Path.GetDirectoryName(Util.CurrentQueryPath);
	var nuspecFileName = @"PoeEye.nuspec";
	var nuspecFilePath = Path.Combine(homeDir, nuspecFileName);

	new[] { nuspecFilePath }.Dump("Reading version from .nuspec file...");
	var nuspecDocument = XElement.Load(nuspecFilePath);
	var ns = nuspecDocument.GetDefaultNamespace();
	
	var version = nuspecDocument.Descendants(ns + "metadata").Single().Descendants(ns + "version").Single().Value;
	
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
