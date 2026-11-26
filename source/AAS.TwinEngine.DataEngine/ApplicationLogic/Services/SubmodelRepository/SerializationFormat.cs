namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;

/// <summary>
/// Serialization formats supported by the IDTA Serialization API.
/// </summary>
public enum SerializationFormat
{
    /// <summary>
    /// AASX package serialization (<c>application/asset-administration-shell-package+xml</c>).
    /// </summary>
    AasxPackage,

    /// <summary>
    /// Plain JSON serialization of the Asset Administration Shell environment (<c>application/json</c>).
    /// </summary>
    Json,

    /// <summary>
    /// Plain XML serialization of the Asset Administration Shell environment (<c>application/xml</c>).
    /// </summary>
    Xml
}
