using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Test
{
    /// <summary>
    /// Klasa odpowiedzialna za całą funkcjonalność związaną z kryptografią
    /// </summary>
    class CryptoModule
    {
        private static Encoding enc = Encoding.UTF8;
        private static SHA1CryptoServiceProvider sha1Provider = new SHA1CryptoServiceProvider();
        private static string hashFunctionVersion = "SHA1";

        private static RSACryptoServiceProvider myPrivateKey = null;
        private static RSACryptoServiceProvider myPublicKey = null;
        private static Dictionary<string, RSACryptoServiceProvider> otherPublicKeys = new Dictionary<string, RSACryptoServiceProvider>();



        public static byte[] HashMessage(string message)
        {
            //SHA1CryptoServiceProvider sha1Provider = new SHA1CryptoServiceProvider(); //mocniejsze sha do uzycia?
            Byte[] dataToHash = enc.GetBytes(message);
            return sha1Provider.ComputeHash(dataToHash);
        }
        /// <summary>
        /// Metoda licząca hash z bajtów n razy
        /// </summary>
        /// <param name="data"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public static byte[] HashNTimes(byte[] data, int n)
        {
            byte[] result = data;
            for (int i = 0; i < n; i++)
            {
                result = Hash(result);
            }
            return result;
        }
        /// <summary>
        /// Metoda licząca hash z podanych bajtów
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] Hash(byte[] data)
        {
            return sha1Provider.ComputeHash(data);
        }
        /// <summary>
        /// Metoda weryfikująca podpisaną wiadomość
        /// </summary>
        /// <param name="decryptedMsg"></param>
        /// <param name="signedMsg"></param>
        /// <param name="senderName"></param>
        /// <returns></returns>
        public static bool Verify(byte[] decryptedMsg, byte[] signedMsg, string senderName)
        {
            RSACryptoServiceProvider csp;
            otherPublicKeys.TryGetValue(senderName, out csp);
            return csp.VerifyData(decryptedMsg, hashFunctionVersion, signedMsg);
        }
        /// <summary>
        /// Metoda podpisująca wiadomość
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static byte[] Sign(string msg)
        {
            return myPrivateKey.SignData(enc.GetBytes(msg), hashFunctionVersion);
        }
        /// <summary>
        /// Metoda odszyfrowująca wiadomość
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static byte[] DecryptMsg(byte[] msg)
        {
            return myPrivateKey.Decrypt(msg, true);
        }
        
        /// <summary>
        /// Metoda szyfrująca wiadomość
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="receiverName"></param>
        /// <returns></returns>
        public static byte[] EncryptMsg(byte[] msg, string receiverName)
        {
            RSACryptoServiceProvider csp;
            if (!otherPublicKeys.TryGetValue(receiverName, out csp))
                return null;
            return csp.Encrypt(msg, true);
        }

        /// <summary>
        /// Metoda importująca klucze (prywatne lub publiczne) z certyfikatu
        /// </summary>
        /// <param name="cert"></param>
        /// <param name="isPrivate">
        /// true - import klucza prywatnego
        /// </param>
        /// <param name="isMine">
        /// true - import publicznego klucza zalogowanego użytkownika
        /// false - import publicznego klucza innego użytkownika
        /// </param>
        public static string ImportKey(X509Certificate2 cert, bool isPrivate, bool isMine)
        {
            if (isPrivate)
            {
                myPrivateKey = (RSACryptoServiceProvider)cert.PrivateKey;
                return null;
            }
            else if (isMine)
            {
                myPublicKey = (RSACryptoServiceProvider)cert.PublicKey.Key;
                return null;
            }
            else
            {
                string[] array = cert.Subject.Split('=');
                otherPublicKeys.Add(array[1], (RSACryptoServiceProvider)cert.PublicKey.Key);
                return array[1];
            }

        }
        /// <summary>
        /// Metoda usuwająca z pamięci klucz użytkownika
        /// </summary>
        /// <param name="userName"></param>
        public static void RemoveUserKey(string userName)
        {
            otherPublicKeys.Remove(userName);
        }
        /// <summary>
        /// Metoda tworząca certyfikat z kluczem publicznym z przesłanych bajtów
        /// </summary>
        /// <param name="rawData"></param>
        /// <returns></returns>
        public static X509Certificate2 CreatePublicCertFromRawData(byte[] rawData)
        {
            X509Certificate2 cert = new X509Certificate2(rawData);
            return cert;
        }
        /// <summary>
        /// Metoda tworząca certyfikat z parą kluczy z przesłanych bajtów
        /// </summary>
        /// <param name="rawData"></param>
        /// <returns></returns>
        public static X509Certificate2 CreatePrivateCertFromRawData(byte[] rawData)
        {
            X509Certificate2 cert = new X509Certificate2(rawData, "pwd", X509KeyStorageFlags.Exportable);
            return cert;
        }
    }
}
