using System;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Threading;

namespace PkryServ
{
    public delegate void MsgSignal(object sender, MsgEvent e);
    public class MsgEvent
    {
        /// <summary>
        /// Wydarzenie z odebrana wiadomoscia
        /// </summary>
        /// <param name="data"></param>
        /// <param name="log"></param>
        public MsgEvent(byte[] data, string log)
        {
            msg = data;
            login = log;
        }
        public byte[] msg { get; private set; }
        public string login { get; private set; }
    }
    /// <summary>
    /// Klasa opisujaca klienta
    /// </summary>
    class Client
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="login"></param>
        /// <param name="password"></param>
        /// <param name="pesel"></param>
        public Client(string login, byte[] password, byte[] pesel )
        {
            this.login = login;
            passwordHash = password;
            peselHash = pesel;
            n = 1;
            generator = new Random();
            isOnline = false;
            isRunning = false;
            isConnected = false;
            certificate = CryptoModule.GenerateCeriticate(login);
        }
        /// <summary>
        /// Metoda umozliwiajaca porownanie peseli
        /// </summary>
        /// <param name="pesel"></param>
        /// <returns></returns>
        public bool CheckPesel(byte[] pesel)
        {
            string p1 = null;
            string p2 = null;
            StringBuilder sb = new StringBuilder();
            foreach (byte b in pesel)
            {
                sb.Append(b.ToString()).Append(" ");
            }
            p1 = sb.ToString();
            sb.Clear();
            foreach (byte b in peselHash)
            {
                sb.Append(b.ToString()).Append(" ");
            }
            p2 = sb.ToString();

            if (p1.Equals(p2))
                return true;
            return false;
        }
        /// <summary>
        /// Metoda zwracajaca n
        /// </summary>
        /// <returns></returns>
        public int GetN()
        {
            if( n == 1)
            {
                n = generator.Next(5, 15);
                return n;
            }

            return n--;
        }
        /// <summary>
        /// Metoda pokazujaca klienta
        /// </summary>
        public void Show()
        {
            Console.WriteLine("Client: {0}", login);
            Console.Write("Paswword: ");
            foreach(byte b in passwordHash)
            {
                Console.Write(b.ToString() + " ");
            }
            Console.WriteLine();
            Console.Write("Pesel: ");
            foreach (byte b in peselHash)
            {
                Console.Write(b.ToString() + " ");
            }
            Console.WriteLine();
        }
        /// <summary>
        /// Metoda sluzaca do zapisu do pliku
        /// </summary>
        /// <returns></returns>
        public string Write()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(login).Append(" ");
            foreach(byte b in passwordHash)
            {
                sb.Append(b.ToString()).Append(" ");
            }
            foreach (byte b in peselHash)
            {
                sb.Append(b.ToString()).Append(" ");
            }
            return sb.ToString();
        }
        /// <summary>
        /// Metoda startujaca klienta
        /// </summary>
        public void Run()
        {
            listener = new Thread(Listen);
            listener.IsBackground = true;
            listener.Start();
            isRunning = true;
        }
        /// <summary>
        /// Metoda umozliwiajaca nasluchiwanie w kanale danego klienta
        /// </summary>
        private void Listen()
        {
            byte[] buffor = new byte[8192];
            byte[] data = null;
            while(true)
            {
                try
                {
                    int bytes = sslStream.Read(buffor, 0, buffor.Length);
                    data = new byte[bytes];
                    Array.Copy(buffor, data, bytes);
                    MsgCame(new MsgEvent(data, login));
                }
                catch(Exception e)
                {
                    //Console.WriteLine(e.ToString());
                    Console.WriteLine("Client {0} is not responding. Disconnecting user...", login);
                    Disconnect();
                    break;
                }
            }
        }
        /// <summary>
        /// Metoda umozliwiajaca rozlaczenie
        /// </summary>
        public void Disconnect()
        {
            isOnline = false;
            isRunning = false;
            isConnected = false;
            sslStream.Close();
            sslStream = null;
            Console.WriteLine("Client {0} disconnected", login);
        }
        /// <summary>
        /// Obsluga wydarzenia przyjscia wiadomosci
        /// </summary>
        /// <param name="e"></param>
        private void MsgCame(MsgEvent e)
        {
            if (msgSignal != null)
                msgSignal(this, e);
        }
        /// <summary>
        /// Metoda umozliwiajaca wyslanie wiadomosci
        /// </summary>
        /// <param name="data"></param>
        public void Send(byte[] data)
        {
            sslStream.Write(data);
            sslStream.Flush();
        }
        /// <summary>
        /// Metoda stopujaca dzialanie klienta
        /// </summary>
        public void Stop()
        {
            listener.Abort();
            isOnline = false;
            isRunning = false;
            isConnected = false;
            sslStream.Close();
            sslStream = null;
        }
 
        Random generator;
        int n;
        Thread listener;
        public bool isOnline { get; set; }
        public bool isRunning { get; set; }
        public bool isConnected { get; set; }
        public string login {get; private set;}
        public string connectedTo { get;  set; }
        public byte[] passwordHash { get; set;}
        public byte[] peselHash {get; set;}
        public X509Certificate2 certificate { get; set; }
        public SslStream sslStream { get; set; }
        public event MsgSignal msgSignal;
    }
}
