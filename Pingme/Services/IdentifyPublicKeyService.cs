using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Pingme.Services
{
    internal class IdentifyPublicKeyService
    {
        private readonly AsymmetricKeyParameter _privateKey;
        private readonly HashSet<string> _internalUsers;

        public IdentifyPublicKeyService(string domainPrivateKeyPemPath, IEnumerable<string> internalUsers)
        {
            if (!File.Exists(domainPrivateKeyPemPath))
                throw new FileNotFoundException("Domain private key PEM file not found.", domainPrivateKeyPemPath);

            using (var reader = File.OpenText(domainPrivateKeyPemPath))
            {
                var pemReader = new PemReader(reader);
                var keyObject = pemReader.ReadObject();

                if (keyObject is AsymmetricCipherKeyPair keyPair)
                    _privateKey = keyPair.Private;
                else if (keyObject is AsymmetricKeyParameter keyParam && keyParam.IsPrivate)
                    _privateKey = keyParam;
                else
                    throw new InvalidOperationException("Invalid private key format in PEM file.");
            }

            _internalUsers = new HashSet<string>(internalUsers);
        }

        /// <summary>
        /// Ký public key của user nội bộ
        /// </summary>
        /// <param name="userId">ID user nội bộ</param>
        /// <param name="publicKeyString">Public key dạng string (XML hoặc PEM)</param>
        /// <returns>Tuple: Success, Signature (base64), Error message</returns>
        public (bool Success, string SignatureBase64, string Error) SignPublicKey(string userId, string publicKeyString)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(publicKeyString))
                return (false, null, "UserId or public key is empty.");

            if (!_internalUsers.Contains(userId))
                return (false, null, $"User '{userId}' is not a valid internal user.");

            try
            {
                //byte[] dataBytes = Encoding.UTF8.GetBytes(publicKeyString);
                string normalized = NormalizeXml(publicKeyString);
                byte[] dataBytes = Encoding.UTF8.GetBytes(normalized);

                var signer = SignerUtilities.GetSigner("SHA-256withECDSA");
                signer.Init(true, _privateKey);
                signer.BlockUpdate(dataBytes, 0, dataBytes.Length);
                byte[] signature = signer.GenerateSignature();

                string base64Signature = Convert.ToBase64String(signature);
                return (true, base64Signature, null);
            }
            catch (Exception ex)
            {
                return (false, null, $"Error signing public key: {ex.Message}");
            }
        }
        private string NormalizeXml(string xml)
        {
            return xml.Replace("\r", "").Replace("\n", "").Replace("  ", "").Trim();
        }

    }
}
