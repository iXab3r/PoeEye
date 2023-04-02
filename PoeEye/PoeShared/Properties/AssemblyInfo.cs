global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Drawing;
global using System.Reactive.Disposables;
global using System.Reactive.Linq;
global using System.Threading.Tasks;
global using PoeShared.Scaffolding; 
global using PoeShared.Logging;
global using System.Collections;
global using System.Diagnostics;
global using System.Globalization;
global using System.Linq.Expressions;
global using System.Collections.Concurrent;
global using System.Threading;

using System.Reflection;
using System.Runtime.CompilerServices;
using PoeShared.Modularity;

[assembly: AssemblyVersion("0.0.0.0")]
[assembly: AssemblyFileVersion("0.0.0.0")]
[assembly: AssemblyMetadata("SquirrelAwareVersion", "0")]
[assembly: AssemblyHasPoeConfigConverters]
[assembly: AssemblyHasPoeMetadataReplacements]

[assembly: InternalsVisibleTo("PoeShared.Benchmarks")]
[assembly: InternalsVisibleTo("PoeShared.Tests")]
[assembly: InternalsVisibleTo("EyeAuras.Tests")]
[assembly: InternalsVisibleTo("EyeAuras.Benchmarks")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]