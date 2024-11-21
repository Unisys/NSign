using System.Security.Cryptography.X509Certificates;

namespace NSign.Providers
{
    internal static class Certificates
    {
        public static X509Certificate2 GetCertificate(string name)
        {
#if NET8_0
            return new X509Certificate2(name);
#else
            return X509CertificateLoader.LoadCertificateFromFile(name);
#endif
        }

        public static X509Certificate2 GetCertificateWithPrivateKey(string name, string? password)
        {
#if NET8_0
            return new X509Certificate2(name, password);
#else
            return X509CertificateLoader.LoadPkcs12FromFile(name, password);
#endif
        }
    }
}
