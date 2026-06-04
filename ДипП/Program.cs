using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ДипП
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Включаем консоль для отладки
#if DEBUG
            AllocConsole();
            Console.WriteLine("=== ЗАПУСК ПРИЛОЖЕНИЯ ===");
            Console.WriteLine($"Текущая директория: {AppDomain.CurrentDomain.BaseDirectory}");
            Console.WriteLine($"Время: {DateTime.Now:HH:mm:ss}");
            Console.WriteLine();
#endif

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Запускаем тестовую форму
            Application.Run(new TestForm());
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();
    }
}
