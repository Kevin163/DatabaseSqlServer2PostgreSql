using System;
using System.Windows;
using DatabaseMigration.Migration;

namespace DatabaseMigration
{
    public partial class MainWindow : Window
    {
        private FileLoggerService _logger;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void MigrateButton_Click(object sender, RoutedEventArgs e)
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
            }
            catch (Exception ex)
            {
                StatusText.Text = "迁移失败! 详情请查看日志文件。";
                _logger?.Log($"发生未处理的异常: {ex.ToString()}");
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