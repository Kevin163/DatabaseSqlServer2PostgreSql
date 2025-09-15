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
            ReportTextBox.Text = $"日志文件路径: {_logger.LogFilePath}";

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
    }
}