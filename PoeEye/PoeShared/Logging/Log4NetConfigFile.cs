using System.Xml;
using log4net.Core;

namespace PoeShared.Logging;

public sealed class Log4NetConfigFile
{
    private static readonly IFluentLog Log = typeof(Log4NetConfigFile).PrepareLogger();

    private readonly LevelMap levelMap = new();
    private string loadedXml;
    
    public Log4NetConfigFile()
    {
        AddBuiltinLevels(levelMap);
    }
    
    public Level Level { get; set; }
    
    public void SaveTo(FileInfo file)
    {
        Log.Info($"Saving configuration to {file}");
        var document = new XmlDocument();
        document.LoadXml(loadedXml);
        var navigator = document.CreateNavigator() ?? throw new XmlException($"Failed to create navigator for file {file}");
        var node = navigator.SelectSingleNode("//log4net//root//level");
        node.SetAttribute("value", string.Empty, Level?.ToString());

        Directory.CreateDirectory(file.DirectoryName);
        document.Save(file.FullName);
    }

    public void Load(FileInfo fileInfo)
    {
        Log.Info($"Loading configuration from file {fileInfo}");
        var content = File.ReadAllText(fileInfo.FullName);
        LoadXml(content);    
    }

    public void LoadXml(string xml)
    {
        Log.Info($"Loading configuration: {xml}");
        loadedXml = xml;
        
        var document = new XmlDocument();
        document.LoadXml(loadedXml);
        var navigator = document.CreateNavigator() ?? throw new XmlException($"Failed to create navigator");
        var node = navigator.SelectSingleNode("//log4net//root//level");
        var levelValue = node?.GetAttribute("value", string.Empty);
        Log.Debug($"Log level value: {levelValue}");
        Level = string.IsNullOrEmpty(levelValue) ? null : levelMap[levelValue];
    }
    
    private static void AddBuiltinLevels(LevelMap levelMap)
    {
        levelMap.Add(Level.Off);
        levelMap.Add(Level.Emergency);
        levelMap.Add(Level.Fatal);
        levelMap.Add(Level.Alert);
        levelMap.Add(Level.Critical);
        levelMap.Add(Level.Severe);
        levelMap.Add(Level.Error);
        levelMap.Add(Level.Warn);
        levelMap.Add(Level.Notice);
        levelMap.Add(Level.Info);
        levelMap.Add(Level.Debug);
        levelMap.Add(Level.Fine);
        levelMap.Add(Level.Trace);
        levelMap.Add(Level.Finer);
        levelMap.Add(Level.Verbose);
        levelMap.Add(Level.Finest);
        levelMap.Add(Level.All);
    }
}