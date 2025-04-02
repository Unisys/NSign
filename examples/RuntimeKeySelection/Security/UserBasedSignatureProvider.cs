using NSign;
using NSign.Providers;
using NSign.Signatures;
using System.Text;

namespace RuntimeKeySelection.Security;

internal sealed class UserBasedSignatureProvider : ISigner
{
    private static readonly Encoding UTF8 = new UTF8Encoding();
    private readonly IHttpContextAccessor httpContextAccessor;

    public UserBasedSignatureProvider(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public Task<ReadOnlyMemory<byte>> SignAsync(string? keyId, ReadOnlyMemory<byte> input, CancellationToken cancellationToken)
    {
        ISigner signer = GetSignerKeyId(keyId);
        try
        {
            return signer.SignAsync(keyId, input, cancellationToken);
        }
        finally
        {
            if (signer is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    public async Task UpdateSignatureParamsAsync(SignatureParamsComponent signatureParams, MessageContext messageContext, CancellationToken cancellationToken)
    {
        string username = GetUserName();
        string keyId = await GetUserKeyIdAsync();

        signatureParams.KeyId = keyId;
        signatureParams.Tag = $"user[{username}]";
    }

    private string GetUserName()
    {
        return httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "no-user";
    }

    private Task<string> GetUserKeyIdAsync()
    {
        return Task.FromResult(httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "no-user");
    }

    private ISigner GetSignerKeyId(string? keyId)
    {
        ArgumentNullException.ThrowIfNull(keyId);

        return new HmacSha256SignatureProvider(UTF8.GetBytes(keyId), keyId);
    }
}
