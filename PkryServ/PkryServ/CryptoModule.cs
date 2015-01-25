using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using System.IO;

namespace PkryServ
{
    partial class CryptoModule
    {
        private static int keyLength = 1024;
        
        public CryptoModule() 
        {
            sha1Provider = new SHA1CryptoServiceProvider();
            myPrivateKey = new RSACryptoServiceProvider();
            myPublicKey = new RSACryptoServiceProvider();
            otherPublicKeys = new Dictionary<string, RSACryptoServiceProvider>();
        }      
        public static void DisplayCertificate(X509Certificate2 certificate) 
        {
            Console.WriteLine("Name: {0}", certificate.FriendlyName);
            Console.WriteLine("Issuer: {0}", certificate.IssuerName.Name);
            Console.WriteLine("Subject: {0}", certificate.SubjectName.Name);
            Console.WriteLine("Version: {0}", certificate.Version);
            Console.WriteLine("Valid from: {0}", certificate.NotBefore);
            Console.WriteLine("Valid until: {0}", certificate.NotAfter);
            Console.WriteLine("Serial number: {0}", certificate.SerialNumber);
            Console.WriteLine("Signature Algorithm: {0}", certificate.SignatureAlgorithm.FriendlyName);
            Console.WriteLine("Thumbprint: {0}", certificate.Thumbprint);
            Console.WriteLine("Private key: {0}", certificate.PrivateKey);
            Console.WriteLine("Public key: {0}", certificate.PublicKey.Key);
            Console.WriteLine();
            
        }      
        public static bool ImportCert(X509Certificate2 cert,
            StoreName name = StoreName.My,
            StoreLocation location = StoreLocation.CurrentUser)
        {   
            bool isCompleted = false;
            X509Store store = new X509Store(name, location);
            try
            {
                store.Open(OpenFlags.MaxAllowed);
                store.Add(cert);
                isCompleted = true;
            }
            catch (Exception e) { Console.WriteLine(e.Message); }
            finally { store.Close(); }
            return isCompleted;
        }

        public bool DeleteCerts(string subjectName, 
            StoreName name = StoreName.My,
            StoreLocation location = StoreLocation.CurrentUser) 
        {
            bool isCompleted = false;
            X509Store store = new X509Store(name, location);
            try
            {
                store.Open(OpenFlags.ReadWrite);
                X509Certificate2Collection certsToRemove = store.Certificates.Find(X509FindType.FindBySubjectName, subjectName, false);
                if (certsToRemove != null && certsToRemove.Count > 0)
                {
                    store.RemoveRange(certsToRemove);
                    isCompleted = true;
                }
            }
            catch (Exception e) { Console.WriteLine(e.Message); }
            finally { store.Close(); }

            return isCompleted;
        }
        public static X509Certificate2 GenerateCeriticate(string subjectName,
            string Pwd = "bar",
            string FilePath = "cert.pfx",
            string Alias = "CryptoModule") 
        {
            var kpGen = new RsaKeyPairGenerator();
            kpGen.Init(new KeyGenerationParameters(new SecureRandom(new CryptoApiRandomGenerator() ), keyLength));
            var kp = kpGen.GenerateKeyPair();

            
            var certGenerator = new X509V3CertificateGenerator();
            var certName = new X509Name("CN=" + subjectName);
            var serialNo = BigInteger.ProbablePrime(120, new Random());

            certGenerator.SetSerialNumber(serialNo);
            certGenerator.SetSubjectDN(certName);
            certGenerator.SetIssuerDN(certName);
            certGenerator.SetNotAfter(DateTime.Now.AddDays(1));
            certGenerator.SetNotBefore(DateTime.Now);
            certGenerator.SetSignatureAlgorithm("SHA1withRSA");
            certGenerator.SetPublicKey(kp.Public);

            var newCertificate = certGenerator.Generate(kp.Private);
            
            var store = new Pkcs12Store();
            var certEntry = new X509CertificateEntry(newCertificate);
            store.SetCertificateEntry(Alias, certEntry);
            store.SetKeyEntry(Alias, new AsymmetricKeyEntry(kp.Private), new[] { certEntry });
            
            using (var certFile = File.Create(FilePath))
            {
                store.Save(certFile, Pwd.ToCharArray(), new SecureRandom (new CryptoApiRandomGenerator()));
            }

            var X509net = new System.Security.Cryptography.X509Certificates.X509Certificate2(FilePath,
                Pwd,
                X509KeyStorageFlags.Exportable);
            
            ImportCert(X509net);
            return X509net;
        }
        public static byte[] PreparePublicCertToSend(X509Certificate2 cert)
        {
            return cert.Export(X509ContentType.Cert);
        }

        public X509Certificate2 CreatePublicCertFromRawData(byte[] rawData)
        {
            X509Certificate2 cert = new X509Certificate2(rawData);
            return cert;
        }
        public static byte[] PreparePrivateCertToSend(X509Certificate2 cert) 
        {
            return cert.Export(X509ContentType.Pfx, "pwd");
        }

        public X509Certificate2 CreatePrivateCertFromRawData(byte[] rawData) 
        {
            X509Certificate2 cert = new X509Certificate2(rawData, "pwd", X509KeyStorageFlags.Exportable);
            return cert;
        }
        public void EnumCert(StoreName name = StoreName.My, StoreLocation location = StoreLocation.CurrentUser) 
        {
            X509Store store = new X509Store(name, location);
            try 
	        {	        
		        store.Open(OpenFlags.MaxAllowed);
                foreach (var cert in store.Certificates)
                {
                    DisplayCertificate(cert);
                    if (cert.HasPrivateKey)
                        Console.WriteLine("Above has private key\n");
                }
	        }
	        catch (Exception e)
	        {
		        Console.WriteLine(e.Message);
	        }
            finally
            {   
                store.Close();
            }
        }

        public static X509Certificate2 FindCertificate(CertProperty cp,
            string property,
            StoreName name = StoreName.My, 
            StoreLocation location =  StoreLocation.CurrentUser)
        {
            X509Store store = new X509Store(name, location);
            X509Certificate2Collection certs = null;
            X509Certificate2 foundCert = null;
            try
            {
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);
                switch (cp)
                {
                    case CertProperty.Thumbprint:
                        certs = store.Certificates.Find(X509FindType.FindByThumbprint, property, false);
                        break;
                    case CertProperty.Subject:
                        certs = store.Certificates.Find(X509FindType.FindBySubjectName, property, false);
                        break;
                    case CertProperty.SerialNumber:
                        certs = store.Certificates.Find(X509FindType.FindBySerialNumber, property, false);
                        break;
                    default:
                        break;
                }

            }
            catch (Exception e) { Console.WriteLine(e.Message); }
            finally { store.Close(); }
            if (certs != null)
            {
                foundCert = new X509Certificate2(certs[0]);
            }
            return foundCert;
        }

        public string GetCertThumbprint(string subject,
           StoreName name = StoreName.My,
           StoreLocation location = StoreLocation.CurrentUser) 
        {
            return FindCertificate(CertProperty.Subject, subject).Thumbprint;
        }       
        public enum CertProperty
        {
            Thumbprint,
            Subject,
            SerialNumber
        }
    }
}
