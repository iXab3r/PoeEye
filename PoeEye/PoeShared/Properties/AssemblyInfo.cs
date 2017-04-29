using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Markup;
using RestEase;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.

[assembly: AssemblyTitle("PoeShared")]
[assembly: AssemblyDescription("PoeEye Shared classes")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("PoeShared")]
[assembly: AssemblyCopyright("Copyright © Xab3r 2015")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.

[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM

[assembly: Guid("12df53dd-7144-4d2d-b7a1-b4bc5bbdfb69")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]

[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: InternalsVisibleTo("PoeEye.Tests")]
[assembly: InternalsVisibleTo(RestClient.FactoryAssemblyName)]

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None, //where theme specific resource dictionaries are located
                                     //(used if a resource is not found in the page, 
                                     // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located
                                              //(used if a resource is not found in the page, 
                                              // app, or any theme specific resource dictionaries)
    )]

[assembly: XmlnsPrefix("http://coderush.net/poeeye/", "eye")]
[assembly: XmlnsDefinition("http://coderush.net/poeeye/", "PoeShared")]

[assembly: XmlnsDefinition("http://coderush.net/poeeye/", "PoeShared.Converters")]

[assembly: XmlnsDefinition("http://coderush.net/poeeye/", "PoeShared.Resources")]
[assembly: XmlnsDefinition("http://coderush.net/poeeye/", "PoeShared.Resources.Notifications")]

[assembly: XmlnsDefinition("http://coderush.net/poeeye/", "PoeShared.UI")]
[assembly: XmlnsDefinition("http://coderush.net/poeeye/", "PoeShared.UI.Controls")]
[assembly: XmlnsDefinition("http://coderush.net/poeeye/", "PoeShared.UI.Models")]
[assembly: XmlnsDefinition("http://coderush.net/poeeye/", "PoeShared.UI.ViewModels")]
[assembly: XmlnsDefinition("http://coderush.net/poeeye/", "PoeShared.UI.Views")]

[assembly: XmlnsDefinition("http://coderush.net/poeeye/", "PoeShared.Themes")]

[assembly: XmlnsDefinition("http://coderush.net/poeeye/", "PoeShared.Scaffolding")]
[assembly: XmlnsDefinition("http://coderush.net/poeeye/", "PoeShared.Scaffolding.WPF")]
[assembly: XmlnsDefinition("http://coderush.net/poeeye/", "PoeShared.Scaffolding.Converters")]
