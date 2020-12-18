using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsFormsApp1;

namespace TestIzm
{
    static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main()
        {
            CultureInfo.CurrentCulture = new CultureInfo("ru-RU");
            
            var t = System.Threading.Thread.CurrentThread;
            t.CurrentUICulture = new CultureInfo("ru-RU");
            t.CurrentCulture = new CultureInfo("ru-RU");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

           // CultureInfo.CurrentCulture = new CultureInfo("en", true);

            Application.Run(new TechnologyControl());
        }
    }
}
