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
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c547cac37abd99c8db225ef2f6c8a3602f3b3606cc9891605d02baa56104f4cfc0734aa39b93bf7852f7d9266654753cc297e7d2edfe0bac1cdcf9f717241550e0a7b191195b7667bb4f64bcb8e2121380fd1d9d46ad2d92d2d15605093924cceaf74c4861eff62abf69b9291ed0a340e113be11e6a7d3113e92484cf7045cc7")]

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

[assembly: XmlnsDefinition("http://coderush.net/poeeye/", "PoeShared.Scaffolding")]
[assembly: XmlnsDefinition("http://coderush.net/poeeye/", "PoeShared.Scaffolding.WPF")]
