using Serilog;
using Serilog.Core;
using System.IO;
using System.Text;
using System.Linq;

namespace DatabaseMigration.Migration
{
    public class FileLoggerService : IDisposable
    {
        private readonly Logger _logger;
        private readonly string _logFilePath;
        private bool _disposed = false;

        public string LogFilePath => _logFilePath;

        public FileLoggerService()
        {
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(logDir);
            _logFilePath = Path.Combine(logDir, $"migration_{DateTime.Now:yyyyMMdd_HHmmss}.log");

            // 删除旧日志文件，只保留最近的3个
            try
            {
                var files = Directory.GetFiles(logDir, "migration_*.log");
                var filesToDelete = files
                    .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                    .Skip(3);

                foreach (var f in filesToDelete)
                {
                    try
                    {
                        if (File.Exists(f))
                        {
                            File.Delete(f);
                        }
                    }
                    catch
                    {
                        // 忽略单个文件删除错误
                    }
                }
            }
            catch
            {
                // 忽略枚举或排序等错误，保证日志系统不会中断
            }

            // 使用 Async 包装 File sink，保证写入是异步的且高效
            _logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Async(a => a.File(
                    _logFilePath,
                    encoding: Encoding.UTF8,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] {Message:lj}{NewLine}{Exception}"
                ))
                .CreateLogger() as Logger;
        }
        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="message"></param>
        public void Log(string message)
        {
            if (_disposed) return;
            _logger?.Information(message);
        }
        /// <summary>
        /// 记录错误日志
        /// </summary>
        /// <param name="message"></param>
        public void LogError(string message)
        {
            if (_disposed) return;
            _logger?.Error(message);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                // 释放 Serilog logger，确保缓冲日志被刷新
                _logger?.Dispose();
            }
            catch
            {
                // 忽略释放中的异常
            }
        }
    }
}
