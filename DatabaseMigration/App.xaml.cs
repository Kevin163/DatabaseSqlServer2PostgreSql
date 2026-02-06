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
        bool isAutoRun = e.Args.Length > 0 && e.Args[0] == "--auto";

        // 创建主窗口
        var mainWindow = new MainWindow
        {
            AutoRun = isAutoRun,
            WindowState = isAutoRun ? WindowState.Minimized : WindowState.Normal
        };

        // 设置为主窗口
        MainWindow = mainWindow;

        // 显示窗口（自动运行模式会在后台运行，迁移完成后自动退出）
        mainWindow.Show();
    }
}

