﻿using System.Text;

namespace Test
{
    /// <summary>
    /// Klasa pomocnicza przenosząca dane między obiektami różnych klas
    /// </summary>
    class BindingModule
    {
        public static Encoding enc = Encoding.UTF8;
        /// <summary>
        /// Login użytkownika
        /// </summary>
        public static string myLogin { get; private set; }
        /// <summary>
        /// Metoda sprawdzająca poprawność peselu
        /// </summary>
        /// <param name="pesel"></param>
        /// <returns></returns>
        public static bool CheckPesel(string pesel)
        {
            if (pesel.Length != 10)
                return false;
            bool result = false;
            int[] parameters = { 1, 3, 7, 9, 1, 3, 7, 9, 1, 3 };
            char[] array = pesel.ToCharArray();
            int sum = 0;
            int controlNumber = -1;
            for (int i = 0; i < 10; i++)
            {
                sum += parameters[i] * int.Parse(array[i].ToString());
                if (i == 9)
                    controlNumber = int.Parse(array[i].ToString());
            }
            sum = sum % 10;
            if (sum != 0)
                sum = 10 - sum;
            if (sum == controlNumber)
                result = true;
            return result;
        }
        /// <summary>
        /// Metoda ustawiająca login
        /// </summary>
        /// <param name="login"></param>
        public static void setLogin(string login)
        {
            myLogin = login;
        }
    }
}