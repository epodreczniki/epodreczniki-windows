using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;

namespace epodreczniki.Common
{
    public class DataDecoder
    {
        private static string _keyIV = "d36cbed36c6b1d80dc9b400cb5cfaf23";
        private static string _keyMaterial = "7a9784670f8268012b2f124435fe51a98ef1636a253037b35487aca1ea0180cd57f6f7e59288757ed0e64bc041a0889dda2d7dc124ed06364e175a749e61cc33";

        private static CryptographicKey GenerateSymmetricKey()
        {
            CryptographicKey key;            
            //SymmetricKeyAlgorithmProvider Algorithm = SymmetricKeyAlgorithmProvider.OpenAlgorithm("AES_CBC");
            SymmetricKeyAlgorithmProvider Algorithm = SymmetricKeyAlgorithmProvider.OpenAlgorithm("AES_CBC_PKCS7");            

            // generowanie klucza symetrycznego
            //UInt32 keySize = 512;
            //IBuffer keymaterial = CryptographicBuffer.GenerateRandom((keySize + 7) / 8);
            //_keyMaterial = CryptographicBuffer.EncodeToHexString(keymaterial);

            IBuffer keymaterial = CryptographicBuffer.DecodeFromHexString(DataDecoder._keyMaterial);

            try
            {
                key = Algorithm.CreateSymmetricKey(keymaterial);                                
            }
            catch (ArgumentException)
            {
                //"An invalid key size was specified for the given algorithm.";
                return null;
            }
            return key;
        }        

        public static string CreateSalt(int size = 8)
        {
            try
            {
                Guid guid = Guid.NewGuid();
                byte[] bytes = guid.ToByteArray();
                if (size > bytes.Length)
                    size = bytes.Length;
            
                return Convert.ToBase64String(bytes.Take(size).ToArray());
            }
            catch (Exception)
            {
                return String.Empty;
            }
        }

        public static string HashPassword(string password, string salt)
        {
            try
            {
                var alg = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5);
                IBuffer buffer = CryptographicBuffer.ConvertStringToBinary(password + salt, BinaryStringEncoding.Utf8);
                var hashed = alg.HashData(buffer);
                return CryptographicBuffer.EncodeToBase64String(hashed);
            }
            catch (Exception)
            {
                return String.Empty;
            }
        }

        public static bool VerifyPassword(string passwordHashed, string passwordToVerify, string salt)
        {
            try
            {
                var alg = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5);
                IBuffer buffer = CryptographicBuffer.ConvertStringToBinary(passwordToVerify + salt, BinaryStringEncoding.Utf8);
                IBuffer bufferToVerify = alg.HashData(buffer);

                IBuffer bufferHashed = CryptographicBuffer.DecodeFromBase64String(passwordHashed);

                return CryptographicBuffer.Compare(bufferHashed, bufferToVerify);
                
            }
            catch (Exception)
            {
                return false;
            }            
        }

        public static IBuffer EncryptData(string data)
        {
            try
            {
                IBuffer iv = null;
                CryptographicKey key = DataDecoder.GenerateSymmetricKey();

                IBuffer buffer = CryptographicBuffer.ConvertStringToBinary(data, BinaryStringEncoding.Utf8);

                iv = CryptographicBuffer.DecodeFromHexString(_keyIV);

                return CryptographicEngine.Encrypt(key, buffer, iv);             
            }
            catch(Exception)
            {
                return null;
            }
        }

        public static string DecryptData(IBuffer data)
        {
            IBuffer decrypted;
            try
            {                
                CryptographicKey key = DataDecoder.GenerateSymmetricKey();
                IBuffer iv = CryptographicBuffer.DecodeFromHexString(DataDecoder._keyIV);

                decrypted = CryptographicEngine.Decrypt(key, data, iv);
            }
            catch (Exception)
            {
                return String.Empty;
            }

            return CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, decrypted);
        }                        
    }
}
