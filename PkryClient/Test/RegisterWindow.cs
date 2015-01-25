using System;
using System.Collections;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows.Forms;

namespace Test
{
    /// <summary>
    /// Klasa odpowiedzialna za okno rejestracji
    /// </summary>
    public partial class RegisterWindow : Form
    {
        /// <summary>
        /// Konstruktor klasy RegisterWindow
        /// </summary>
        public RegisterWindow()
        {
            InitializeComponent();
        }
        /// <summary>
        /// Metoda obsługująca zdarzenie kliknięcia w przycisk RegisterBtn
        /// Metoda jest odpowiedzialna za procedurę rejestracji nowego użytkownika
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RegisterBtn_Click(object sender, EventArgs e)
        {
            string login, pwd, pesel;
            login = LoginTextBox.Text;
            pesel = maskedTextBox1.Text;
            pwd = PwdTextBox.Text;
            if (!BindingModule.CheckPesel(pesel))
            {
                StatusLabel.Text = "Invalid Pesel";
                return;
            }
            if (LoginTextBox.Text == "" || maskedTextBox1.Text == "" ||
                PwdTextBox.Text == "" || ConfPwdTextBox.Text == "")
            {
                //RegisterBtn.Enabled = false;
                StatusLabel.Text = "Provide more information";
                return;
            }
            if (pwd.Equals(ConfPwdTextBox.Text))
            {
                //wyslac ponizsze i poczekac na odpowiedz
                bool response = SendRegisterMsg(login, CryptoModule.HashMessage(pwd), CryptoModule.HashMessage(pesel));

                if (response)
                {
                    StatusLabel.Text = "Registration completed succesfully";
                    LoginTextBox.ReadOnly = true;
                    maskedTextBox1.ReadOnly = true;
                    PwdTextBox.ReadOnly = true;
                    ConfPwdTextBox.ReadOnly = true;
                    RegisterBtn.Enabled = false;
                }
                else
                {
                    StatusLabel.Text = "Registration failed";
                    LoginTextBox.ResetText();
                    maskedTextBox1.ResetText();
                    PwdTextBox.ResetText();
                    ConfPwdTextBox.ResetText();
                }
            }
            else
            {
                StatusLabel.Text = "Passwords don't match";
            }
        }
        /// <summary>
        /// Metoda obsługującą wysłanie zadania rejestracji do serwera
        /// Metoda jest odpowiedzialna za komunikacje z serwerem podczas przeprowadzania procesu rejestracji
        /// </summary>
        /// <param name="login">Login uzytkownika</param>
        /// <param name="password">Hash hasla</param>
        /// <param name="pesel">Hash peselu</param>
        private bool SendRegisterMsg(string login, byte[] password, byte[] pesel)
        {
            TcpClient client = new TcpClient("localhost", 8888);
            SslStream sslStream = new SslStream(
                client.GetStream(),
                false,
                new RemoteCertificateValidationCallback(ValidateServerCertificate),
                null
                );
            try
            {
                sslStream.AuthenticateAsClient("pkryserver.jumpingcrab.com");
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

            byte[] msg = new byte[43 + login.Length];
            byte[] info = Encoding.UTF8.GetBytes("REG");
            info.CopyTo(msg, 0);
            password.CopyTo(msg, 3);
            pesel.CopyTo(msg, 23);
            byte[] log = Encoding.UTF8.GetBytes(login);
            log.CopyTo(msg, 43);

            sslStream.Write(msg);
            sslStream.Flush();

            byte[] buffer = new byte[128];
            int bytes = sslStream.Read(buffer, 0, buffer.Length);
            Decoder decoder = Encoding.UTF8.GetDecoder();
            char[] chars = new char[decoder.GetCharCount(buffer, 0, bytes)];
            decoder.GetChars(buffer, 0, bytes, chars, 0);

            client.Close();

            if (chars[0] == '1')
                return true;
            else
                return false;

        }
        public static bool ValidateServerCertificate(
              object sender,
              X509Certificate certificate,
              X509Chain chain,
              SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            Console.WriteLine("Certificate error: {0}", sslPolicyErrors);

            return false;
        }
        private static Hashtable certificateErrors = new Hashtable();
    }
}
