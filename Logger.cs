using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProject1
{

    /// <summary>
    /// 静态日志工具类，基于Serilog实现
    /// </summary>
    public static class Logger
    {
        // 静态日志实例
        private static readonly ILogger _logger;

        // 静态构造函数，初始化日志配置
        static Logger()
        {
            // 日志输出路径
            string logDirectory = @"E:\aa\TestProject1\ProjectFiles\NetSolution\obj";
            string logFilePath = $@"{logDirectory}\app-log-.txt";

            // 初始化日志配置
            _logger = new LoggerConfiguration()
                .MinimumLevel.Debug() // 最小日志级别
                .WriteTo.File(
                    path: logFilePath,
                    rollingInterval: RollingInterval.Day, // 按天滚动
                    rollOnFileSizeLimit: true,
                    fileSizeLimitBytes: 10 * 1024 * 1024, // 10MB
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                    retainedFileCountLimit: 30 // 保留30天日志
                )
                .CreateLogger();
        }

        /// <summary>
        /// 记录Debug级别日志
        /// </summary>
        public static void Debug(string message)
        {
            _logger.Debug(message);
        }

        /// <summary>
        /// 记录Info级别日志
        /// </summary>
        public static void Info(string message)
        {
            _logger.Information(message);
        }

        /// <summary>
        /// 记录Warning级别日志
        /// </summary>
        public static void Warn(string message)
        {
            _logger.Warning(message);
        }

        /// <summary>
        /// 记录Error级别日志
        /// </summary>
        public static void Error(string message)
        {
            _logger.Error(message);
        }

        /// <summary>
        /// 记录带异常的Error级别日志
        /// </summary>
        public static void Error(Exception ex, string message)
        {
            _logger.Error(ex, message);
        }

        /// <summary>
        /// 记录Fatal级别日志
        /// </summary>
        public static void Fatal(string message)
        {
            _logger.Fatal(message);
        }

        /// <summary>
        /// 记录带异常的Fatal级别日志
        /// </summary>
        public static void Fatal(Exception ex, string message)
        {
            _logger.Fatal(ex, message);
        }

        /// <summary>
        /// 手动刷新并关闭日志（建议在程序退出时调用）
        /// </summary>
        public static void CloseAndFlush()
        {
            Log.CloseAndFlush();
        }
    }
}
