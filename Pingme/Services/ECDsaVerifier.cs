using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using System;
using System.Text;
using System.Linq;
using System.IO;

namespace Pingme.Services
{
    public class ECDsaVerifier
    {
        public bool Verify(string data, string signatureBase64, string domainPublicKeyPem)
        {
            try
            {
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                byte[] signature = Convert.FromBase64String(signatureBase64);

                // Load public key từ PEM
                AsymmetricKeyParameter pubKey;
                using (var reader = new StringReader(domainPublicKeyPem))
                {
                    var pemReader = new PemReader(reader);
                    pubKey = (AsymmetricKeyParameter)pemReader.ReadObject();
                }

                var verifier = SignerUtilities.GetSigner("SHA-256withECDSA");
                verifier.Init(false, pubKey); // false = verify
                verifier.BlockUpdate(dataBytes, 0, dataBytes.Length);
                return verifier.VerifySignature(signature);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ECDsaVerifier] Lỗi xác minh chữ ký: {ex.Message}");
                return false;
            }
        }
    }
}
