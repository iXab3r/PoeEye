<Query Kind="Program" />

void Main()
{
	var homeDir = Path.GetDirectoryName(Util.CurrentQueryPath);
	var nuspecFileName = @"PoeEye.nuspec";
	var binariesDir = @"bin\";
	var nuspecFilePath = Path.Combine(homeDir, nuspecFileName);

	var extensionsToExclude = new[] {
		".xml",
		".nupkg"
	};
	
	var binariesPath = Path.Combine(homeDir, binariesDir);

	new[] { binariesPath, nuspecFilePath }.Dump("Args");

	var files = Directory.GetFiles(binariesPath, "*.*", SearchOption.AllDirectories);
	files = files.Select(x => new{
			Relative = MakeRelativePath(binariesPath, x),
			Absolute = x
		})
		.Select(x => x.Relative)
		.ToArray();
	files = files.Where(x => !extensionsToExclude.Contains(Path.GetExtension(x), StringComparer.OrdinalIgnoreCase)).ToArray();
	files.Dump("Nuspec content");

	nuspecFilePath.Dump("Opening nuspec file...");
	var nuspecDocument = XElement.Load(nuspecFilePath);
	var ns = nuspecDocument.GetDefaultNamespace();

	var filesNode = nuspecDocument.Descendants(ns + "files").Single();
	filesNode.Dump("[BEFORE] nuspec files list");
	filesNode.RemoveAll();
	foreach (var file in files)
	{
		var newElement = new XElement(ns+"file");
		newElement.SetAttributeValue("src", Path.Combine(binariesDir, file));
		newElement.SetAttributeValue("target", Path.Combine(@"lib\.net45", file));
		newElement.Attributes("xmlns").Remove();

		filesNode.Add(newElement);
	}
	filesNode.Dump("[AFTER] nuspec files list");
	
	File.Copy(nuspecFilePath, nuspecFilePath+".bak", true);
 	nuspecDocument.Save(nuspecFilePath);
}

// Define other methods and classes here
public static String MakeRelativePath(String fromPath, String toPath)
{
	if (String.IsNullOrEmpty(fromPath)) throw new ArgumentNullException("fromPath");
	if (String.IsNullOrEmpty(toPath)) throw new ArgumentNullException("toPath");

	Uri fromUri = new Uri(fromPath);
	Uri toUri = new Uri(toPath);

	if (fromUri.Scheme != toUri.Scheme) { return toPath; } // path can't be made relative.

	Uri relativeUri = fromUri.MakeRelativeUri(toUri);
	String relativePath = Uri.UnescapeDataString(relativeUri.ToString());

	if (toUri.Scheme.Equals("file", StringComparison.InvariantCultureIgnoreCase))
	{
		relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
	}

	return relativePath;
}