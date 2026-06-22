using System;
using System.Windows.Forms;

namespace LiteAmpPlayer;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        var mainForm = new MainForm();
Application.Run(mainForm);
    }
}


