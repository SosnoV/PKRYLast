using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace PkryServ
{
    partial class CryptoModule
    {
        //private UnicodeEncoding enc;
        private Encoding enc = Encoding.UTF8;
        private static SHA1CryptoServiceProvider sha1Provider = new SHA1CryptoServiceProvider();
        private String hashFunctionVersion = "SHA1";

        private RSACryptoServiceProvider myPrivateKey;
        private RSACryptoServiceProvider myPublicKey;
        private Dictionary<string, RSACryptoServiceProvider> otherPublicKeys;

        public byte[] HashMessage(string message)
        {
            Byte[] dataToHash = enc.GetBytes(message);
            return sha1Provider.ComputeHash(dataToHash);
        }

        public static byte[] Hash(byte[] data)
        {
            return sha1Provider.ComputeHash(data);
        }
        public static byte[] HashNTimes(byte[] data, int n)
        {
            byte[] result = data;
            for (int i = 0; i < n; i++)
            {
                result = Hash(result);
            }
            return result;
        }

        public bool Verify(byte[] decryptedMsg, byte[] signedMsg, string senderName)
        {
            RSACryptoServiceProvider csp;
            otherPublicKeys.TryGetValue(senderName, out csp);
            return csp.VerifyData(decryptedMsg, hashFunctionVersion, signedMsg);  
        }

        public byte[] Sign(string msg) 
        {
            return myPrivateKey.SignData(enc.GetBytes(msg), hashFunctionVersion);
        }

        public byte[] DecryptMsg(byte[] msg) 
        {
            return myPrivateKey.Decrypt(msg, true);
        }

        public byte[] EncryptMsg(byte[] msg, string receiverName) 
        {
            RSACryptoServiceProvider csp;
            otherPublicKeys.TryGetValue(receiverName, out csp);
            return csp.Encrypt(msg, true);
        }
        public void ImportKey(X509Certificate2 cert, bool isPrivate, bool isMine) 
        {
            if (isPrivate)
            {
                myPrivateKey = (RSACryptoServiceProvider)cert.PrivateKey;
            }
            else if((!isPrivate) & (!isMine))
            {
                string[] array = cert.Subject.Split('=');
                otherPublicKeys.Add(array[1], (RSACryptoServiceProvider)cert.PublicKey.Key);
            }
            else if ((!isPrivate) & (isMine))
            {
                myPublicKey = (RSACryptoServiceProvider)cert.PublicKey.Key;
            }
            else 
            {
                Console.WriteLine("Zle parametry");
            }
        }

        //Import key from certificate from store
        public void ImportKey(string subject, bool isPrivate, bool isMine,
            StoreName name = StoreName.My, StoreLocation location = StoreLocation.CurrentUser) 
        {
            if ( (!isPrivate) & (!isMine))
            {
                RSACryptoServiceProvider x = new RSACryptoServiceProvider();
                x = (RSACryptoServiceProvider)FindCertificate(CertProperty.Subject, subject, name, location).PublicKey.Key;
                otherPublicKeys.Add(subject, x);
            }
            else if ( (!isPrivate) & (isMine))
            {
                myPublicKey = (RSACryptoServiceProvider)FindCertificate(CertProperty.Subject, subject, name, location).PublicKey.Key;
            }
            else if(isPrivate)
            {
                myPrivateKey = (RSACryptoServiceProvider)FindCertificate(CertProperty.Subject, subject, name, location).PrivateKey;
            }
            else
            {
                Console.WriteLine("ImportKey zle parametry logiczne");
            }
        }
    }
}
