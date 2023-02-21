using System.Security.Cryptography;

namespace Limp.Client.Cryptography
{
    public class RSAClient
    {
        public RSA Rsa { get; set; } = RSA.Create();
        public RSAParameters PublicParameters { get; set; }
        private RSAParameters GetPublicParameters => Rsa.ExportParameters(false);
    }
}
