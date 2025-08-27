using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OQSDrug
{
    internal static class Program
    {
        private static Mutex app_mutex;

        [STAThread]
        static void Main()
        {
            // ミューテックス作成
            app_mutex = new Mutex(false, "Global\\OQSDRUG_001");

            // ミューテックスの所有権を要求する
            if (!app_mutex.WaitOne(0, false))
            {
                MessageBox.Show("このアプリケーションは複数起動できません。");
                return;
            }

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
            }
            finally
            {
                // ミューテックスを解放
                app_mutex.ReleaseMutex();
            }
        }
    }
}
