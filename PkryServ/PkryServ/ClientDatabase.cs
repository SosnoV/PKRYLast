using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

namespace PkryServ
{
    /// <summary>
    /// Klasa sluzaca do obslugi klientow
    /// </summary>
    public class ClientDatabase
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public ClientDatabase()
        {
            database = new List<Client>();
            try
            {
                ReadFromFile();
            }
            catch
            {

            }
        }
        /// <summary>
        /// Odczytuje klientow z pliku
        /// </summary>
        private void ReadFromFile()
        {
            string[] lines = System.IO.File.ReadAllLines(@"data.txt");
            foreach (string line in lines)
            {
                string[] data = line.Split(' ');
                byte[] password = new byte[20];
                byte[] pesel = new byte[20];
                string login = data[0];

                for(int i = 0; i < 20; ++i)
                {
                    password[i] = Byte.Parse(data[i + 1]);
                }
                for (int i = 0; i < 20; ++i)
                {
                    pesel[i] = Byte.Parse(data[i + 21]);
                }

                Client tmp = new Client(login, password, pesel);
                tmp.msgSignal += new MsgSignal(MsgService);
                database.Add(tmp);
                Console.WriteLine("Client {0} registered" , login);
            }
        }
        /// <summary>
        /// Metoda umozliwiajaca dodanie klienta
        /// </summary>
        /// <param name="login"></param>
        /// <param name="password"></param>
        /// <param name="pesel"></param>
        public void AddClient(string login, byte[] password, byte[] pesel)
        {
            foreach(Client client in database)
            {
                if (login == client.login || client.CheckPesel(pesel))
                    throw new Exception("REG: Login or Pesel in use");
            }
            Client tmp = new Client(login, password, pesel);
            tmp.msgSignal += new MsgSignal(MsgService);
            database.Add(tmp);
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"data.txt", true))
            {
                file.WriteLine(tmp.Write());                
            }
            Console.WriteLine("Client {0} registered, hash lenghth: {1}", login, password.Length);            
        }
        /// <summary>
        /// Metoda umozliwiajaca pobranie certyfikatu
        /// </summary>
        /// <param name="login"></param>
        /// <returns></returns>
        public X509Certificate2 GetCertificate(string login)
        {
            X509Certificate2 certificate = CryptoModule.GenerateCeriticate(login);
            foreach(Client c in database)
            {
                if (c.login == login)
                {
                    c.certificate = certificate;
                    return c.certificate;
                }
            }
            X509Certificate2 wrong = null;
            return wrong;
        }
        /// <summary>
        /// Metoda do obslugi wiadomosci miedzy klientami
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MsgService(object sender, MsgEvent e)
        {
            StringBuilder sb = new StringBuilder();
            Decoder decoder = Encoding.UTF8.GetDecoder();

            char[] chars = new char[decoder.GetCharCount(e.msg, 0, e.msg.Length)];
            decoder.GetChars(e.msg, 0, e.msg.Length, chars, 0);
            sb.Append(chars);

            string message = sb.ToString();

            if (String.Compare("ONL", 0, message, 0, 3) == 0)
            {
                string result = null;
                foreach (Client c in database)
                {
                    if (c.isRunning && !c.isConnected)
                    {
                        result += c.login + " ";
                    }
                }
                byte[] msg;
                if (result == null)
                    msg = Encoding.UTF8.GetBytes("ONLRNikt nie jest online");
                else
                    msg = Encoding.UTF8.GetBytes("ONLR" + result);
                foreach (Client c in database)
                {
                    if (c.login == e.login)
                        c.Send(msg);
                }
                return;
            }
            else if (String.Compare("ISO", 0, message, 0, 3) == 0)
            {
                string login = message.Substring(3);
                bool isOn = false;
                string result;
                foreach (Client c in database)
                {
                    if (c.login == login && c.isRunning && !c.isConnected)
                    {
                        isOn = true;
                        break;
                    }
                }
                if (isOn)
                {
                    result = "ISORTrue";
                    foreach (Client c in database)
                    {
                        if (c.login == e.login)
                        {
                            c.connectedTo = login;
                            break;
                        }
                    }
                }
                else
                    result = "ISORFalse";

                byte[] msg = Encoding.UTF8.GetBytes(result);
                foreach (Client c in database)
                {
                    if (c.login == e.login)
                        c.Send(msg);
                }
                return;
            }
            else if (String.Compare("GCR", 0, message, 0, 3) == 0)
            {
                X509Certificate2 cerAlice = null;
                X509Certificate2 cerBob = null;
                string alice = e.login;
                string bob = null;
                foreach (Client c in database)
                {
                    if (c.login == alice)
                    {
                        bob = c.connectedTo;
                        break;
                    }
                }
                foreach(Client c in database)
                {
                    if(c.login == bob)
                    {
                        c.connectedTo = alice;
                        break;
                    }
                }
                Console.WriteLine("ALICE is {0}", alice);
                Console.WriteLine("BOB is {0}", bob);
                //Pobranie odpowiednich certyfikatow
                foreach (Client c in database)
                {
                    if (c.login == alice)
                        cerAlice = c.certificate;
                    else if (c.login == bob)
                        cerBob = c.certificate;
                }
                //Przygotowanie wiadomości dla Boba
                //sb.Clear();
                string prefix = "GCRB";
                int bytes = Encoding.UTF8.GetByteCount(prefix);
                //sb.Append(prefix).Append(bytes);
                //prefix = sb.ToString();
                //bytes = Encoding.UTF8.GetByteCount(prefix);
                byte[] pre = Encoding.UTF8.GetBytes(prefix);
                byte[] rawData = CryptoModule.PreparePublicCertToSend(cerAlice);
                byte[] bobMsg = new byte[rawData.Length + bytes];
                pre.CopyTo(bobMsg, 0);
                rawData.CopyTo(bobMsg, bytes);

                //Przygotowanie wiadomości dla Alice
                //sb.Clear();
                prefix = "GCRA";
                bytes = Encoding.UTF8.GetByteCount(prefix);
                //sb.Append(prefix).Append(bytes);
                //prefix = sb.ToString();
                //bytes = Encoding.UTF8.GetByteCount(prefix);
                byte[] preA = Encoding.UTF8.GetBytes(prefix);
                byte[] rawDataA = CryptoModule.PreparePublicCertToSend(cerBob);
                byte[] aliceMsg = new byte[rawDataA.Length + bytes];
                preA.CopyTo(aliceMsg, 0);
                rawDataA.CopyTo(aliceMsg, bytes);

                //Wyslanie wiadomosci
                foreach (Client c in database)
                {
                    if (c.login == alice)
                    {
                        c.Send(aliceMsg);
                        c.isConnected = true;
                        break;
                    }
                }
                Console.WriteLine("BOB certificate send to ALICE");
                foreach (Client c in database)
                {
                    if (c.login == bob)
                    {
                        c.Send(bobMsg);
                        c.isConnected = true;
                        break;
                    }
                }
                Console.WriteLine("ALICE certificate send to BOB");
                return;
            }
            else if (String.Compare("DIS", 0, message, 0, 3) == 0)
            {
                Console.WriteLine("Logging {0} out", e.login);
                string con = null;
                bool conect = false;
                foreach(Client c in database)
                {
                    if(c.login == e.login)
                    {
                        if (c.isConnected)
                        {
                            con = c.connectedTo;
                            conect = true;
                        }
                        c.Stop();
                        Console.WriteLine("Client {0} disconnected", c.login);
                        break;
                    }
                }
                if(conect)
                {
                    foreach(Client c in database)
                    {
                        if(c.login == con)
                        {
                            c.Send(Encoding.UTF8.GetBytes("DIC"));
                            c.isConnected = false;
                            c.connectedTo = null;
                            break;
                        }
                    }
                }
            }
            else if (String.Compare("DIC", 0, message, 0, 3) == 0)
            {
                string log = null;
                foreach(Client c in database)
                {
                    if(c.login == e.login)
                    {
                        log = c.connectedTo;
                        c.isConnected = false;
                        c.connectedTo = null;
                        break;
                    }
                }
                foreach (Client c in database)
                {
                    if (c.login == log)
                    {
                        c.Send(e.msg);
                        c.isConnected = false;
                        c.connectedTo = null;
                        break;
                    }
                }
                
            }
            else if (String.Compare("MSG", 0, message, 0, 3) == 0)
            {
                Console.WriteLine("Message from {0}", e.login);
                string log = null;
                foreach (Client c in database)
                {
                    if (c.login == e.login)
                    {
                        log = c.connectedTo;
                        break;
                    }
                }
                foreach (Client c in database)
                {
                    if (c.login == log)
                    {
                        c.Send(e.msg);
                        break;
                    }
                }
                Console.WriteLine("Message sent to {0}", log);
            }
        }
        /// <summary>
        /// MEtoda startujaca klienta
        /// </summary>
        public void RunClient()
        {
            foreach(Client client in database)
            {
                if (client.isOnline && !client.isRunning)
                    client.Run();
            }
        }
        /// <summary>
        /// Metoda sluzaca do logowanie klienta
        /// </summary>
        /// <param name="login"></param>
        /// <param name="password"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public bool LogClient(string login, byte[] password, int n)
        {
            foreach (Client client in database)
            {
                if (login == client.login)
                {
                    string pass = null;
                    foreach(byte b in password)
                    {
                        pass += (b.ToString() + " ");
                    }
                    --n;

                    byte[] hash = CryptoModule.HashNTimes(client.passwordHash, n);
                    string h = null;
                    foreach (byte b in hash)
                    {
                        h += (b.ToString() + " ");
                    }

                    if (h.Equals(pass))
                    {
                        Console.WriteLine("LOG: Logged {0}", login);
                        client.isOnline = true;
                        return true;
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// Metoda sluzaca do pobrania wartosci n
        /// </summary>
        /// <param name="login"></param>
        /// <returns></returns>
        public int ClientN(string login)
        {
            foreach (Client client in database)
            {
                if (login == client.login)
                    return client.GetN();
            }
            return 0;
        }
        /// <summary>
        /// Metoda dodajaca wlasciwy strumien dla klienta
        /// </summary>
        /// <param name="login"></param>
        /// <param name="endPoint"></param>
        public void AddEndPoint(string login, SslStream endPoint)
        {
            foreach (Client client in database)
            {
                if (login == client.login)
                {
                    client.sslStream = endPoint;
                    client.isOnline = true;
                    break;
                }
            }
        }
        List<Client> database;
    }
}
