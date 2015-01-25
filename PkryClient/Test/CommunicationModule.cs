using System;
using System.Collections;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Windows.Forms;

namespace Test
{
    /// <summary>
    /// Delegat do obslugi eventu
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void MsgSignal(object sender, MsgEvent e);
    /// <summary>
    /// Event sluzacy do asychronicznego przesylania wiadomosci w programie
    /// </summary>
    public class MsgEvent
    {
        /// <summary>
        /// Konstruktor eventu z wiadomoscia
        /// </summary>
        /// <param name="data"></param>
        public MsgEvent(byte[] data)
        {
            msg = data;
        }
        /// <summary>
        /// Pole przechowywujace wiadomosc
        /// </summary>
        public byte[] msg { get; private set; }
    }
    /// <summary>
    /// Klsa obslugujaca komunikacje
    /// </summary>
    public class CommunicationModule
    {
        /// <summary>
        /// Konstruktor domyslny
        /// </summary>
        public CommunicationModule()
        {
            client = null;
            sslStream = null;
            isRunning = false;
        }
        /// <summary>
        /// Metoda sluzaca do uruchomienia komunikacji z sererem
        /// W wypadku niepowodzenia komunikacja jest zamykana i zwracany jest wynik false
        /// </summary>
        /// <param name="machineName"></param>
        /// <param name="serverName"></param>
        /// <returns>True jesli udalo sie polaczyc z serwerem
        /// False jesli nie udalo sie polaczyc z serwerem</returns>
        public bool Run(string machineName, string serverName)
        {
            client = new TcpClient(machineName , 8888);
            //client = new TcpClient(new IPEndPoint(IPAddress.Parse("89.77.76.109"), 8888));
            sslStream = new SslStream(
            client.GetStream(),
            false,
            new RemoteCertificateValidationCallback(ValidateServerCertificate),
            null
            );

            try
            {
                sslStream.AuthenticateAsClient(serverName);
            }
            catch (AuthenticationException e)
            {
                Console.WriteLine("Exception: {0}", e.Message);
                if (e.InnerException != null)
                {
                    Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                }
                Console.WriteLine("Authentication failed - closing the connection.");
                client.Close();
                return false;
            }
            isRunning = true;
            return true;
        }
        /// <summary>
        /// Metoda sluzaca do zastopowania klienta TCP
        /// </summary>
        public void Stop()
        {
            client.Close();
        }
        /// <summary>
        /// Metoda majaca na celu wyslanie loginu do sererwera i oczekiwanie na odpowiedz przy logowaniu
        /// </summary>
        /// <param name="login"></param>
        /// <returns>Zwraca parametr n oznaczjace liczbe wymaganych hashy hasla</returns>
        public int SendLogin(string login)
        {
            byte[] msg = BindingModule.enc.GetBytes("LOG" + login);
            sslStream.Write(msg);
            sslStream.Flush();

            byte[] buffer = new byte[128];
            int bytes = sslStream.Read(buffer, 0, buffer.Length);
            char[] chars = new char[BindingModule.enc.GetCharCount(buffer, 0, bytes)];
            BindingModule.enc.GetChars(buffer, 0, bytes, chars, 0);

            StringBuilder messageData = new StringBuilder();
            messageData.Append(chars);
            string result = messageData.ToString();

            int n = Int32.Parse(result);
            if (n == 0)
                client.Close();
            return n;
        }
        /// <summary>
        /// Metoda sluzaca do potwierdzenia zgodnosci wprowadzonego hasla z tym zapisanym na serwerze
        /// </summary>
        /// <param name="pwd"></param>
        /// <returns></returns>
        public bool SendPwd(byte[] pwd)
        {
            sslStream.Write(pwd);
            sslStream.Flush();

            byte[] buffer = new byte[128];
            int bytes = sslStream.Read(buffer, 0, buffer.Length);
            char[] chars = new char[BindingModule.enc.GetCharCount(buffer, 0, bytes)];
            BindingModule.enc.GetChars(buffer, 0, bytes, chars, 0);

            StringBuilder messageData = new StringBuilder();
            messageData.Append(chars);
            int result = Int32.Parse(messageData.ToString());

            if (result == 1)
                return true;
            return false;
        }
        /// <summary>
        /// Metoda pozwalajaca uzyskac certyfikat z serwera
        /// </summary>
        /// <returns></returns>
        public byte[] GetCertificate()
        {
            byte[] msg = BindingModule.enc.GetBytes("CER" + BindingModule.myLogin);
            sslStream.Write(msg);
            sslStream.Flush();

            byte[] buffer = new byte[1670];
            int bytes = sslStream.Read(buffer, 0, buffer.Length);

            return buffer;
        }
        /// <summary>
        /// Metoda pozwalajaca na rozpoczecie asynchronicznej komunikacji z serwerem
        /// </summary>
        public void Run()
        {
            listener = new Thread(Listen);
            listener.IsBackground = true;
            listener.Start();
            isRunning = true;
        }
        /// <summary>
        /// Metoda obslugujaca asychroniczne nasluchiwanie z serwera
        /// </summary>
        private void Listen()
        {
            byte[] buffor = new byte[10240];
            byte[] data = null;
            while (true)
            {
                try
                {
                    int bytes = sslStream.Read(buffor, 0, buffor.Length);
                    data = new byte[bytes];
                    Array.Copy(buffor, data, bytes);
                    MsgCame(new MsgEvent(data));
                }
                catch(Exception e)
                {
                    MessageBox.Show(e.ToString());
                }
            }
        }
        /// <summary>
        /// Metoda pozwalajaca zakonczyc nasluchiwanie
        /// </summary>
        public void StopListening()
        {
            listener.Abort();
            isRunning = false;
        }
        /// <summary>
        /// Metoda obslugujaca rzucanie eventu z wiadomoscia
        /// </summary>
        /// <param name="e"></param>
        private void MsgCame(MsgEvent e)
        {
            if (msgSignal != null)
                msgSignal(this, e);
        }
        /// <summary>
        /// Metoda sluzaca do sprawdzenie dostepnosci danego uzytkownika
        /// </summary>
        /// <param name="userLogin">Nazwa uzytkownika</param>
        public void UserOnline(string userLogin)
        {
            byte[] msg = BindingModule.enc.GetBytes("ISO" + userLogin);
            Send(msg);
        }
        /// <summary>
        /// Metoda sluzaca do pobrania listy aktywnych uzytkownikow z serwera
        /// </summary>
        public void GetOnlineUsers()
        {
            byte[] msg = BindingModule.enc.GetBytes("ONL");
            Send(msg);
            
        }
        /// <summary>
        /// Meoda sluzaca do wysylania danych
        /// </summary>
        /// <param name="data"></param>
        public void Send(byte[] data)
        {
            sslStream.Write(data);
            sslStream.Flush();
        }
        /// <summary>
        /// Delegat słuzacy do potwierdzenia autentycznosci serwera
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="sslPolicyErrors"></param>
        /// <returns></returns>
        public static bool ValidateServerCertificate(
                 object sender,
                 X509Certificate certificate,
                 X509Chain chain,
                 SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            Console.WriteLine("Certificate error: {0}", sslPolicyErrors);

            // Do not allow this client to communicate with unauthenticated servers. 
            return false;
        }

        TcpClient client;
        SslStream sslStream;
        /// <summary>
        /// Pole sluzace do wyslania wydarzenia
        /// </summary>
        public event MsgSignal msgSignal;
        static Hashtable certificateErrors = new Hashtable();
        Thread listener;
        /// <summary>
        /// Pole pozwala sprawdzic czy ropoczelo sie asychroniczne nasluchiwanie
        /// </summary>
        public bool isRunning { get; private set; }
    }
}
