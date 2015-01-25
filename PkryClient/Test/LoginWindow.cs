using System;
using System.Windows.Forms;
using System.Security.Cryptography.X509Certificates;

namespace Test
{
    /// <summary>
    /// Klasa definiująca okno logowania
    /// </summary>
    public partial class LoginWindow : Form
    {

        private MainWindow mw;

        /// <summary>
        /// Konstruktor klasy LoginWindow
        /// </summary>
        public LoginWindow(MainWindow mw)
        {
            //odwołanie w ChatWindow: mw.lw.cm
            this.mw = mw;
            InitializeComponent();
        }

        /// <summary>
        /// Metoda służąca do przeprowadzenia procedury logowania
        /// </summary>
        private void LoginMethod()
        {
            if (PwdTextBox.Text != "" && LoginTextBox.Text != "")
            {
                //Zebranie danych z pol okna logowania
                string login = LoginTextBox.Text;
                string pwd = PwdTextBox.Text;
                //Zaszyfrowanie hasla i usuniecie z pamieci
                byte[] pwdArray = BindingModule.enc.GetBytes(pwd);
                StatusLabel.Text = "Login in progress";
                pwd = "";
                PwdTextBox.ResetText();
                //Uruchomienie modulu komunkacji
                if (!mw.cm.Run("localhost", "pkryserver.jumpingcrab.com"))
                {
                    StatusLabel.Text = "Not able to tart comunnication module";
                    return;
                }
                //Wyslanie loginu i oczekiwanie na odp
                int n = mw.cm.SendLogin(login);
                //Reakcja na bledny login
                if (n == 0)
                {
                    StatusLabel.Text = "Signing in failed, wrong login";
                    mw.cm.Stop();
                }
                //Poprawny login hashowanie i wyslanie hasla
                else 
                {
                    pwdArray = CryptoModule.HashNTimes(pwdArray, --n);
                    //send pwdArray
                    //Poczekaj na odpowiedz

                   // bool response = mw.cm.SendPwd(pwdArray);
                    if (mw.cm.SendPwd(pwdArray))
                    {
                        BindingModule.setLogin(login);
                        //Udane logowanie, czekam na certyfikat
                        StatusLabel.Text = "Waiting for certificate";
                        //Otrzymany certyfikat
                        byte[] certificateRawData = mw.cm.GetCertificate();
                        X509Certificate2 certificate = CryptoModule.CreatePrivateCertFromRawData(certificateRawData);
                        CryptoModule.ImportKey(certificate, true, false);
                        CryptoModule.ImportKey(certificate, true, true);

                        mw.EnableDisableControls(true);
                        mw.DisableLogBtn();
                        mw.WriteInLog("Logged in!");
                        mw.cm.Run();
                        this.Close();
                    }
                    else
                    {
                        StatusLabel.Text = "Signing in failed, wrong password";
                    }
                }
            }
            else
            {
                StatusLabel.Text = "Need more data to proceed";
            }

        }

        private void RegisterLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            new RegisterWindow().Show();
        }

        private void LoginTextBox_Enter(object sender, EventArgs e)
        {
            LoginTextBox.Text = "";
        }

        private void PwdTextBox_Enter(object sender, EventArgs e)
        {
            PwdTextBox.Text = "";
        }

        private void LoginBtn_Click(object sender, EventArgs e)
        {
            LoginMethod();
        }

        private void LoginWindow_Load(object sender, EventArgs e)
        {
            this.RegistrationToolTip.SetToolTip(this.RegisterLabel, "Click to register");
            this.LoginBtnToolTip.SetToolTip(this.LoginBtn, "Click to log in");
        }

        private void PwdTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                LoginMethod();
            }
        }

    }
}
