namespace PoeShared.Modularity;

/// <summary>
/// Specifies the type of exception that occurred during the processing of Poe configuration.
/// </summary>
public enum PoeConfigExceptionType
{
    /// <summary>
    /// Indicates an unknown exception type. Used as a default value when the specific exception type is not determined.
    /// </summary>
    Unknown,

    /// <summary>
    /// Indicates that the metadata replacement process has failed. This occurs when the replacement service returns null for the provided metadata. 
    /// This could be due to issues in the replacement service or invalid metadata input.
    /// </summary>
    MetadataReplacementFailed,

    /// <summary>
    /// Indicates that conversion of the metadata failed. This occurs when the metadata has a different version than expected (lower than the current version) 
    /// and the conversion subsystem is unable to convert it to the required format. This should be reported to the development team for investigation.
    /// </summary>
    ConversionFailed,

    /// <summary>
    /// Indicates that the provided metadata version is greater than the version supported by the current version of the application. 
    /// This usually means the user is attempting to import metadata from a newer version of the application into an older version, 
    /// which is not supported due to potential incompatibilities.
    /// </summary>
    VersionIsGreaterThanSupported,
}
