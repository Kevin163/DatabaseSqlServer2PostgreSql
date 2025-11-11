using Microsoft.Data.SqlClient;
using Npgsql;

namespace DatabaseMigration.Migration
{
    public class MigrationService
    {
        private readonly string _sourceConnectionString;
        private readonly string _targetConnectionString;
        private readonly FileLoggerService _logger;
        private readonly MigrationMode _migrationMode;

        public MigrationService(string sourceConnectionString, string targetConnectionString, FileLoggerService logger, MigrationMode migrationMode)
        {
            _sourceConnectionString = sourceConnectionString;
            _targetConnectionString = targetConnectionString;
            _logger = logger;
            _migrationMode = migrationMode;
        }

        public async Task MigrateDatabaseAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    _logger.Log($"数据库迁移开始。模式: {_migrationMode}");
                    using (SqlConnection sourceConnection = new SqlConnection(_sourceConnectionString))
                    {
                        sourceConnection.Open();
                        _logger.Log("源数据库连接成功。");

                        using (NpgsqlConnection targetConnection = new NpgsqlConnection(_targetConnectionString))
                        {
                            targetConnection.Open();
                            _logger.Log("目标数据库连接成功。");

                            //由于表和视图的已经测试通过，目前测试期间，为了节省时间，暂时注释掉表和视图的迁移
                            //new TableMigrator(_logger, _migrationMode).Migrate(sourceConnection, targetConnection);
                            //new ViewMigrator(_logger).Migrate(sourceConnection, targetConnection);
                            new StoredProcedureMigrator(_logger).Migrate(sourceConnection, targetConnection);
                            // 其它迁移器同理
                        }
                    }
                    _logger.Log("数据库迁移完成。");
                }
                catch (Exception ex)
                {
                    _logger.Log($"迁移过程中发生严重错误: {ex}");
                }
            });
        }
    }

    public enum MigrationMode
    {
        Development,
        Production
    }
}
