using System;
using System.IO;
using System.Windows;
using AshkanLedgerPro.Security;

namespace AshkanLedgerPro;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // The login dialog is the first Window. Never let its close trigger
        // the default OnMainWindowClose shutdown before the workspace opens.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        base.OnStartup(e);
        try
        {
            var login = new LoginWindow();
            bool authenticated = login.ShowDialog() == true && login.User is not null;
            if (!authenticated)
            {
                Shutdown();
                return;
            }

            var workspace = new MainWindow(login.User!);
            MainWindow = workspace;
            workspace.Show();
            ShutdownMode = ShutdownMode.OnMainWindowClose;
        }
        catch (Exception ex)
        {
            ShowStartupError(ex);
            Shutdown(1);
        }
    }

    private static void ShowStartupError(Exception ex)
    {
        try
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AshkanLedgerPro", "Logs");
            Directory.CreateDirectory(directory);
            File.AppendAllText(Path.Combine(directory, "startup-errors.log"),
                $"[{DateTimeOffset.Now:O}] {ex}{Environment.NewLine}{Environment.NewLine}");
        }
        catch { /* Logging failure must not conceal the original error. */ }
        MessageBox.Show(ex.ToString(), "Ashkan Ledger Pro - Startup Error",
            MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
