﻿using System;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows.Forms;

namespace Test
{
    /// <summary>
    /// Klasa odpowiedzialna za główne okno aplikacji
    /// </summary>
    public partial class MainWindow : Form
    {
        private StringBuilder sb;
        public CommunicationModule cm;
        private bool isChatOpen = false;
        private string loginToConnect; 
        private bool areCharsOver;

        /// <summary>
        /// Konstrukto klasy MainWindow
        /// </summary>
        public MainWindow()
        {
            //this.lw = lw;
            cm = new CommunicationModule();
            cm.msgSignal += new MsgSignal(MsgService);
            sb = new StringBuilder();
            InitializeComponent();
            EnableDisableControls(false);
            loginToConnect = null;
            this.MsgTextBox.Enabled = false;
            this.SendBtn.Enabled = false;
            this.DisconnectBtn.Enabled = false;
        }
        /// <summary>
        /// Metoda odpowiedzialna za obsluge przychodzacych wiadomosci
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MsgService(object sender, MsgEvent e)
        {
            sb.Clear();
            Decoder decoder = Encoding.UTF8.GetDecoder();

            char[] chars = new char[decoder.GetCharCount(e.msg, 0, e.msg.Length)];
            decoder.GetChars(e.msg, 0, e.msg.Length, chars, 0);
            sb.Append(chars);

            string message = sb.ToString();
            sb.Clear();

            if (String.Compare("ONLR", 0, message, 0, 4) == 0)
            {
                string onlinelist = message.Substring(4);
                onlinelist = onlinelist.Replace(' ', '\n');
                WriteInLog("Online:\n" + onlinelist);
                return;
            }
            else if (String.Compare("ISOR", 0, message, 0, 4) == 0)
            {
                bool isOnline = false;
                string msg = null;
                if (String.Compare("True", 0, message, 4, 4) == 0)
                    isOnline = true;

                if (!isOnline)
                {
                    sb.Append("Connection attempt with: ").Append(loginToConnect).
                        Append(" failed, User is offline");
                    msg = sb.ToString();
                    WriteInLog(msg);
                    loginToConnect = null;
                    sb.Clear();
                    return;
                }
                else
                {
                    sb.Append("Connection attempt with: ").Append(loginToConnect).
                       Append(" succeed");
                    msg = sb.ToString();
                    WriteInLog(msg);
                    sb.Append("Waiting for certificate from ").Append(loginToConnect);
                    msg = sb.ToString();
                    WriteInLog(msg);
                    sb.Clear();

                    byte[] msgOut = BindingModule.enc.GetBytes("GCR");
                    cm.Send(msgOut);
                    return;
                }
            }
            else if(String.Compare("GCRA", 0, message, 0, 4) == 0)
            {
                byte[] data = e.msg;
                byte[] certRawData = new byte[data.Length - 4];
                Array.Copy(data, 4, certRawData, 0, certRawData.Length);

                X509Certificate2 cert = CryptoModule.CreatePublicCertFromRawData(certRawData);
                loginToConnect = CryptoModule.ImportKey(cert, false, false);

                string msg;
                sb.Clear();
                sb.Append("Alice received certicate from ").Append(loginToConnect).
                        Append(". Starting chat now");
                msg = sb.ToString();
                WriteInLog(msg);

                //Rozpoczecie dzialania chatu
                isChatOpen = true;
                EnableDisableControls(false);
                this.MsgTextBox.Enabled = true;
                this.SendBtn.Enabled = true;
                this.DisconnectBtn.Enabled = true;

                return;
            }
            else if(String.Compare("GCRB", 0, message, 0, 4) == 0)
            {
                byte[] data = e.msg;
                byte[] certRawData = new byte[data.Length - 4];
                Array.Copy(data, 4, certRawData, 0, certRawData.Length);

                X509Certificate2 cert = CryptoModule.CreatePublicCertFromRawData(certRawData);
                loginToConnect = CryptoModule.ImportKey(cert, false, false);

                string msg;
                sb.Clear();
                sb.Append("Bob received certicate from ").Append(loginToConnect).
                        Append(".Connecting. Starting chat now");
                msg = sb.ToString();
                WriteInLog(msg);

                //Rozpoczecie dzialania chatu
                isChatOpen = true;
                EnableDisableControls(false);
                this.MsgTextBox.Enabled = true;
                this.SendBtn.Enabled = true;
                this.DisconnectBtn.Enabled = true;
                
                return;
            }
            else if (String.Compare("DIC", 0, message, 0, 3) == 0)
            {
                //Disconnect
                if (isChatOpen)
                {
                    SendBtn.Enabled = false;
                    MsgTextBox.Enabled = false;
                    isChatOpen = false;
                    EnableDisableControls(true);
                    this.MsgTextBox.Enabled = false;
                    this.SendBtn.Enabled = false;
                    this.DisconnectBtn.Enabled = false;
                    CryptoModule.RemoveUserKey(loginToConnect);
                    loginToConnect = null;
                    WriteInLog("Disconnected. Chat terminated");
                }
            }
            else if (String.Compare("MSG", 0, message, 0, 3) == 0)
            {
                WriteInLog("MSG recived");
                byte[] encrypted = new byte[128];
                byte[] signed = new byte[128];

                Array.Copy(e.msg, 3, encrypted, 0, 128);
                Array.Copy(e.msg, 131, signed, 0, 128);

                byte[] decrypted = CryptoModule.DecryptMsg(encrypted);
                
                if(CryptoModule.Verify(decrypted, signed, loginToConnect))
                {
                    string msg = Encoding.UTF8.GetString(decrypted);
                    WriteInLogAsOtherUser(msg);
                }
                else
                {
                    //Disconnect
                    SendBtn.Enabled = false;
                    MsgTextBox.Enabled = false;
                    isChatOpen = false;
                    EnableDisableControls(true);
                    this.MsgTextBox.Enabled = false;
                    this.SendBtn.Enabled = false;
                    this.DisconnectBtn.Enabled = false;
                    CryptoModule.RemoveUserKey(loginToConnect);
                    cm.Send(Encoding.UTF8.GetBytes("DIC"));
                    loginToConnect = null;
                    WriteInLog("Disconnected. Chat terminated");
                }
            }
        }
        /// <summary>
        /// Metoda odpowiedzialna za procedurę połączenia z innym użytkownikiem
        /// </summary>
        private void ConnectMethod()
        {
            if (isChatOpen == true)
            {
                WriteInLog("Can't open another session");
                return;
            }
            if (ConInputTextBox.Text.Equals("")) //Pusty TextBox
                return;
            loginToConnect = ConInputTextBox.Text;
            if (loginToConnect.Equals(BindingModule.myLogin))
            {
                WriteInLog("Can't talk with yourself");
                ConInputTextBox.ResetText();
                return;
            }
            sb.Clear();
            sb.Append("Trying to connect with: ").Append(loginToConnect);
            string msg = sb.ToString();
            WriteInLog(msg);
            ConInputTextBox.Clear();

            cm.UserOnline(loginToConnect);
            return;
            /*
            if (!isOnline)
            {
                sb.Append("Connection attempt with: ").Append(login).
                    Append(" failed, User is offline");
                msg = sb.ToString();
                WriteInLog(msg);
                return;
            }
            else
            {
                sb.Append("Connection attempt with: ").Append(login).
                   Append(" succeed");
                msg = sb.ToString();
                WriteInLog(msg);
                sb.Append("Waiting for certificate for ").Append(login);
                msg = sb.ToString();
                WriteInLog(msg);

                //Odebranie certyfikatu i utworzenie certyfikatu:
                //Odkomentowac:
                //byte[] certRawData;
                //X509Certificate2 cert = CryptoModule.CreatePublicCertFromRawData(certRawData);
                //CryptoModule.ImportKey(cert, false, false);

                sb.Append("Received certicate for ").Append(login).
                    Append(". Starting chat now");
                msg = sb.ToString();
                WriteInLog(msg);
                //Wyswietlenie okna czatu
                ChatWindow chat = new ChatWindow(login, this);
                chat.Show();
                //BindingModule.AddChat(login, this);
            }*/
        }
        /// <summary>
        /// Metoda obsługująca zdarzenie kliknięcia w przycisk ConnectBtn
        /// W szczególności wywoływana jest metoda ConnectMethod()
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConnectBtn_Click(object sender, EventArgs e)
        {
            ConnectMethod();
        }
        /// <summary>
        /// Metoda wyświetlająca komunikat w Log
        /// </summary>
        /// <param name="msg">
        /// Komunikat do wyświetlenia
        /// </param>
        public void WriteInLog(string msg)
        {
            sb.Clear();
            sb.Append(DateTime.Now.ToString()).Append(" ").Append(msg);
            Log.AppendText(sb.ToString());
            sb.Clear();
            Log.AppendText("\n");
        }
        /// <summary>
        /// Metoda włączająca/wyłączajaca kontrolki (przyciski, textbox)
        /// </summary>
        /// <param name="enabled">
        /// True - włączone
        /// False - wyłączone
        /// </param>
        internal void EnableDisableControls(bool enabled)
        {
            this.ConnectBtn.Enabled = enabled;
            this.ConInputTextBox.ReadOnly = (!enabled);
            this.OnlineBtn.Enabled = enabled;
        }
        /// <summary>
        /// Metoda obsługująca zdarzenie ładowania komponentu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Load(object sender, EventArgs e)
        {
            ConnectBtnToolTip.SetToolTip(this.ConnectBtn, "Press to connect");
            OnlineBtnToolTip.SetToolTip(this.OnlineBtn, "Check who's online");
            WriteInLog("Program running and ready");
        }
        /// <summary>
        /// Metoda obslugujaca klikniecie w przycisk sluzacy do pobranie uzytkownikow online
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)//onlinebutton
        {
                cm.GetOnlineUsers();
        }
        /// <summary>
        /// Metoda obsługująca zdarzenie wciśnięcia klawisza w ConInputTextBox
        /// W szczególności wciśnięcie klawisza Enter powoduje wywołanie metody ConnectMethod()
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConInputTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                ConnectMethod();
            }
        }
        /// <summary>
        /// Metoda wyłączająca przycisk LoginBtn
        /// </summary>
        internal void DisableLogBtn()
        {
            this.LogBtn.Enabled = false;
        }
        /// <summary>
        /// Metoda obsługująca zdarzenie kliknięcia w przycisk LogBtn
        /// W szczególności wywołuje ona LoginWindow co pozwala na przeprowadzenie procedury logowania
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LogBtn_Click(object sender, EventArgs e)
        {
            //EnableDisableControls(false);
            new LoginWindow(this).Show();
        }
        /// <summary>
        /// Metoda obsługująca zdarzenie TextChanged
        /// Dotyczy okna wyslania wiadomosci
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MsgTextBox_TextChanged(object sender, EventArgs e)
        {
            if (MsgTextBox.Text.Length > 80 || MsgTextBox.Text.Length == 0)
            {
                areCharsOver = true;
                SendBtn.Enabled = false;
                SendBtn.BackColor = System.Drawing.Color.Red;
            }
            else
            {
                areCharsOver = false;
                SendBtn.Enabled = true;
                SendBtn.BackColor = System.Drawing.Color.Green;
            }

            // sb.Append(MsgTextBox.Text.Length).Append("/160");
            //CharCount.Text = sb.ToString();
            sb.Clear();
        }
        /// <summary>
        /// Metoda obsługująca zdarzenie Click wygenerowane przez przycisk SendBtn
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SendBtn_Click(object sender, EventArgs e)
        {
            TextToSendMethod();
        }
        /// <summary>
        /// Metoda wysyłająca wiadomość użytkownika
        /// </summary>
        private void TextToSendMethod()
        {
            if (!areCharsOver)
            {
                string msg = MsgTextBox.Text;

                byte[] encryptedMsg = CryptoModule.EncryptMsg(Encoding.UTF8.GetBytes(msg), loginToConnect);
                byte[] signedMsg = CryptoModule.Sign(msg);
                byte[] toSend = new byte[259];
                byte[] prefix = Encoding.UTF8.GetBytes("MSG");

                Array.Copy(prefix, 0, toSend, 0, 3);
                Array.Copy(encryptedMsg, 0, toSend, 3, 128);
                Array.Copy(signedMsg, 0, toSend, 131, 128);
                
                cm.Send(toSend);

                WriteInLogAsThisUser(msg);
                MsgTextBox.ResetText();
            }
        }
        /// <summary>
        /// Metoda wyświetlająca wiadomość napisaną przez użytkownika
        /// </summary>
        /// <param name="msg"></param>
        public void WriteInLogAsThisUser(string msg)
        {
            sb.Clear();
            sb.Append(BindingModule.myLogin).Append("\n").
               Append(msg);
            WriteInLog(sb.ToString());
        }
        /// <summary>
        /// Metoda wyświetlająca wiadomość od innego użytkownika
        /// </summary>
        /// <param name="msg"></param>
        public void WriteInLogAsOtherUser(string msg)
        {
            sb.Clear();
            sb.Append(loginToConnect).Append("\n").
               Append(msg);
            WriteInLog(sb.ToString());
        }
        /// <summary>
        /// Metoda słuzaca do rozlaczenia aktualnej rozmowy
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DisconnectBtn_Click(object sender, EventArgs e)
        {
            if (isChatOpen)
            {
                //Disconnect
                SendBtn.Enabled = false;
                MsgTextBox.Enabled = false;
                isChatOpen = false;
                EnableDisableControls(true);
                this.MsgTextBox.Enabled = false;
                this.SendBtn.Enabled = false;
                this.DisconnectBtn.Enabled = false;
                CryptoModule.RemoveUserKey(loginToConnect);
                cm.Send(Encoding.UTF8.GetBytes("DIC"));
                loginToConnect = null;
                WriteInLog("Disconnected. Chat terminated");
            }
        }
        /// <summary>
        /// Metoda sluzaca do zamkniecia chatu i wylogowania uzytkownika
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                cm.Send(Encoding.UTF8.GetBytes("DIS"));
            }
            catch
            {

            }
            if (cm.isRunning)
            {
                cm.StopListening();
            }
            try
            {
                cm.Stop();
            }
            catch
            {

            }
        }

    }
}