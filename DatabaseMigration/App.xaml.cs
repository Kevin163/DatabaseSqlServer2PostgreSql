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
        // 检查命令行参数
        if (e.Args.Length > 0 && e.Args[0] == "--auto")
        {
            // 自动运行模式：不调用 base.OnStartup，避免创建两个窗口
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            var mainWindow = new MainWindow();
            mainWindow.AutoRun = true;
            mainWindow.Show();
        }
        else
        {
            // 正常模式：调用基类方法，会自动启动 StartupUri 指定的窗口
            base.OnStartup(e);
        }
    }
}

