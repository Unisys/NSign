using System.Security.Cryptography.X509Certificates;

namespace NSign.Providers
{
    internal static class Certificates
    {
        public static X509Certificate2 GetCertificate(string name)
        {
            return new X509Certificate2(name);
        }

        public static X509Certificate2 GetCertificateWithPrivateKey(string name, string password)
        {
            return new X509Certificate2(name, password);
        }
    }
}
