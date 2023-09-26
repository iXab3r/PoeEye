using System.ComponentModel;

// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices;

/// <summary>
///   Fixes bug in msbuild that happens in projects for Linux that use records
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
internal class IsExternalInit{}