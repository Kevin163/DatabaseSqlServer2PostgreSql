using System;
using System.Threading.Tasks;
using System.Windows;
using DatabaseMigration.Migration;

namespace DatabaseMigration
{
    public partial class MainWindow : Window
    {
        private FileLoggerService _logger;

        /// <summary>
        /// 是否为自动运行模式
        /// </summary>
        public bool AutoRun { get; set; } = false;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (AutoRun)
            {
                // 自动运行模式：延迟一秒后自动开始迁移
                await Task.Delay(1000);
                await RunMigrationAsync();
            }
        }

        private async void MigrateButton_Click(object sender, RoutedEventArgs e)
        {
            await RunMigrationAsync();
        }

        private async Task RunMigrationAsync()
        {
            MigrateButton.IsEnabled = false;
            StatusText.Text = "开始迁移...详情请查看日志文件。";
            ReportTextBox.Text = ""; // Clear previous log file path

            _logger = new FileLoggerService();
            ReportTextBox.Text = _logger.LogFilePath;

            try
            {
                string sourceConnectionString = GetSourceConnectionString();
                string targetConnectionString = GetTargetConnectionString();
                MigrationMode mode = (MigrationMode)MigrationModeComboBox.SelectedIndex;

                var migrationService = new MigrationService(sourceConnectionString, targetConnectionString, _logger, mode);
                await migrationService.MigrateDatabaseAsync();

                StatusText.Text = "迁移完成! 详情请查看日志文件。";

                // 如果是自动运行模式，迁移完成后延迟1秒退出
                if (AutoRun)
                {
                    await Task.Delay(1000);
                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = "迁移失败! 详情请查看日志文件。";
                _logger?.Log($"发生未处理的异常: {ex.ToString()}");

                // 如果是自动运行模式，出错后也要退出
                if (AutoRun)
                {
                    await Task.Delay(1000);
                    Application.Current.Shutdown(1); // 退出码 1 表示失败
                }
            }
            finally
            {
                MigrateButton.IsEnabled = true;
                _logger?.Dispose();
            }
        }

        private string GetSourceConnectionString()
        {
            return $"Server={SourceServer.Text};Database={SourceDb.Text};User Id={SourceUser.Text};Password={SourcePassword.Password};TrustServerCertificate=True;";
        }

        private string GetTargetConnectionString()
        {
            return $"Host={TargetServer.Text};Database={TargetDb.Text};Username={TargetUser.Text};Password={TargetPassword.Password};";
        }

        /// <summary>
        /// 点击 ReportTextBox 时将其内容复制到剪贴板并提示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReportTextBox_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                var text = ReportTextBox.Text;
                if (string.IsNullOrWhiteSpace(text))
                {
                    MessageBox.Show(this, "没有可复制的内容。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                Clipboard.SetText(text);
                MessageBox.Show(this, "已复制到剪贴板。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"复制到剪贴板失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}