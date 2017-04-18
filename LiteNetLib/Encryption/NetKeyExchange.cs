#if !WINRT || UNITY_EDITOR
using System.Security.Cryptography;

namespace LiteNetLib.Encryption
{
    public class NetKeyExchange
    {
        private readonly RSACryptoServiceProvider _rsa;
        private const int KeySize = 2048;

        public NetKeyExchange()
        {
            _rsa = new RSACryptoServiceProvider(KeySize);
        }

        public byte[] ExportPublicKey()
        {
            return _rsa.ExportCspBlob(false);
        }

        public void ImportPublicKey(byte[] data)
        {
            _rsa.ImportCspBlob(data);
        }

        public byte[] EncryptKey(byte[] key)
        {
            return _rsa.Encrypt(key, false);
        }

        public byte[] DecryptKey(byte[] data)
        {
            return _rsa.Decrypt(data, false);
        }
    }
}
#else
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Security.Cryptography.Core;

namespace LiteNetLib.Encryption
{
    public class NetKeyExchange
    {
        private readonly AsymmetricKeyAlgorithmProvider _rsa;
        private CryptographicKey _key;
        private const int KeySize = 2048;

        public NetKeyExchange()
        {
            _rsa = AsymmetricKeyAlgorithmProvider.OpenAlgorithm(AsymmetricAlgorithmNames.RsaPkcs1);
        }

        public byte[] ExportPublicKey()
        {
            _key = _rsa.CreateKeyPair(KeySize);
            return _key.ExportPublicKey(CryptographicPublicKeyBlobType.Capi1PublicKey).ToArray();
        }

        public void ImportPublicKey(byte[] data)
        {
            _key = _rsa.ImportPublicKey(data.AsBuffer(), CryptographicPublicKeyBlobType.Capi1PublicKey);
        }

        public byte[] EncryptKey(byte[] key)
        {
            return CryptographicEngine.Encrypt(_key, key.AsBuffer(), null).ToArray();
        }

        public byte[] DecryptKey(byte[] data)
        {
            return CryptographicEngine.Decrypt(_key, data.AsBuffer(), null).ToArray();
        }
    }
}
#endif


