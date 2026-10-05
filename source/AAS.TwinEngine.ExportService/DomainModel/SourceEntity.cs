using System.Security.Cryptography;
using System.Text;

namespace AAS.TwinEngine.ExportService.DomainModel;

/// <summary>
/// One entity as read from the source system. <see cref="RawJson"/> is the payload that
/// will be forwarded to the target (verbatim).
/// </summary>
public sealed record SourceEntity(string Identifier, string RawJson)
{
	public string ContentHash => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(RawJson)));
}
