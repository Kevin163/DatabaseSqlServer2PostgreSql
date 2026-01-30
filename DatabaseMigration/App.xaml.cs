using System.Configuration;
using System.Data;
using System.Windows;

namespace DatabaseMigration;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 检查命令行参数
        if (e.Args.Length > 0 && e.Args[0] == "--auto")
        {
            // 自动运行模式
            var mainWindow = new MainWindow();
            mainWindow.AutoRun = true;
            mainWindow.Show();
        }
    }
}

