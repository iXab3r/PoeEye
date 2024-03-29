﻿global using System;
global using System.Collections.Generic;
global using System.Collections.ObjectModel;
global using System.Linq;
global using BenchmarkDotNet.Attributes;
global using BenchmarkDotNet.Running;
global using DynamicData;
global using DynamicData.Aggregation;
global using DynamicData.Binding;
global using NUnit.Framework;
global using PoeShared.Scaffolding;
global using PropertyBinder;
global using ReactiveUI;
global using Shouldly;
global using System.Reactive.Linq;
global using BenchmarkDotNet.Configs;
global using BenchmarkDotNet.Jobs;
global using BenchmarkDotNet.Toolchains.CsProj;
global using BenchmarkDotNet.Toolchains.DotNetCli;

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.

[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM

[assembly: AssemblyVersion("0.0.0.0")]
[assembly: AssemblyFileVersion("0.0.0.0")]

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly:
    InternalsVisibleTo(
        "DynamicProxyGenAssembly2, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c547cac37abd99c8db225ef2f6c8a3602f3b3606cc9891605d02baa56104f4cfc0734aa39b93bf7852f7d9266654753cc297e7d2edfe0bac1cdcf9f717241550e0a7b191195b7667bb4f64bcb8e2121380fd1d9d46ad2d92d2d15605093924cceaf74c4861eff62abf69b9291ed0a340e113be11e6a7d3113e92484cf7045cc7")]

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None, //where theme specific resource dictionaries are located
    //(used if a resource is not found in the page, 
    // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located
    //(used if a resource is not found in the page, 
    // app, or any theme specific resource dictionaries)
)]
