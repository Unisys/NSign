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

    public Task<ReadOnlyMemory<byte>> SignAsync(ReadOnlyMemory<byte> input, CancellationToken cancellationToken)
    {
        ISigner signer = GetSignerForUser();
        try
        {
            return signer.SignAsync(input, cancellationToken);
        }
        finally
        {
            if (signer is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    public void UpdateSignatureParams(SignatureParamsComponent signatureParams)
    {
        string username = GetUserName();
        ISigner signer = GetSignerForUser(username);
        try
        {
            signer.UpdateSignatureParams(signatureParams);
            signatureParams.Tag = $"user[{username}]";
        }
        finally
        {
            if (signer is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    private string GetUserName()
    {
        return httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "no-user";
    }

    private ISigner GetSignerForUser(string? username = null)
    {
        if (null == username)
        {
            username = GetUserName();
        }

        return new HmacSha256SignatureProvider(UTF8.GetBytes(username), username);
    }
}
