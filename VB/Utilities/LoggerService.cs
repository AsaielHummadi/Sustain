using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Sustain.Utilities.Services
{ 
    public interface ILoggerService
    {
        void LogError(Exception exception);
        void LogError(string message, Exception? exception = null);
        void LogInfo(string message);
        void LogWarning(string message);
        void LogDebug(string message);
    }   
    public class LoggerService : ILoggerService
    {
        private readonly ILogger<LoggerService> _logger;

        public LoggerService(ILogger<LoggerService> logger)
        {
            _logger = logger;
        }

        public void LogError(Exception exception)
        {
            var errorDetails = new
            {
                Message = exception.Message,
                File = exception.Source,
                StackTrace = exception.StackTrace,
                InnerException = exception.InnerException?.Message
            };

            _logger.LogError(exception, "Error occurred: {ErrorDetails}",
                JsonSerializer.Serialize(errorDetails));
        }

        public void LogError(string message, Exception? exception = null)
        {
            if (exception != null)
            {
                _logger.LogError(exception, message);
            }
            else
            {
                _logger.LogError(message);
            }
        }

        public void LogInfo(string message)
        {
            _logger.LogInformation(message);
        }

        public void LogWarning(string message)
        {
            _logger.LogWarning(message);
        }

        public void LogDebug(string message)
        {
            _logger.LogDebug(message);
        }
    }
    public static class LoggerExtensions
    {
        public static void LogDetailedError(this ILogger logger, Exception exception, string? additionalMessage = null)
        {
            var errorInfo = new
            {
                Message = exception.Message,
                Type = exception.GetType().Name,
                Source = exception.Source,
                StackTrace = exception.StackTrace,
                InnerException = exception.InnerException?.Message,
                AdditionalInfo = additionalMessage,
                Timestamp = DateTime.UtcNow
            };

            logger.LogError(exception,
                "Detailed Error: {ErrorInfo}",
                JsonSerializer.Serialize(errorInfo, new JsonSerializerOptions { WriteIndented = true }));
        }

        public static void LogBusinessError(this ILogger logger, string businessOperation, string errorMessage, Dictionary<string, object>? additionalData = null)
        {
            var logData = new
            {
                Operation = businessOperation,
                ErrorMessage = errorMessage,
                AdditionalData = additionalData,
                Timestamp = DateTime.UtcNow
            };

            logger.LogWarning("Business Error: {LogData}",
                JsonSerializer.Serialize(logData, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
    public class ScopedLogger : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IDisposable _scope;
        private readonly DateTime _startTime;
        private readonly string _operationName;

        public ScopedLogger(ILogger logger, string operationName, Dictionary<string, object>? additionalContext = null)
        {
            _logger = logger;
            _operationName = operationName;
            _startTime = DateTime.UtcNow;

            var context = new Dictionary<string, object>
            {
                ["OperationName"] = operationName,
                ["StartTime"] = _startTime
            };

            if (additionalContext != null)
            {
                foreach (var kvp in additionalContext)
                {
                    context[kvp.Key] = kvp.Value;
                }
            }

            _scope = _logger.BeginScope(context);
            _logger.LogInformation("Starting operation: {OperationName}", operationName);
        }

        public void LogProgress(string message, Dictionary<string, object>? data = null)
        {
            _logger.LogInformation("Progress - {OperationName}: {Message} {Data}",
                _operationName, message, data != null ? JsonSerializer.Serialize(data) : "");
        }

        public void Dispose()
        {
            var duration = DateTime.UtcNow - _startTime;
            _logger.LogInformation("Completed operation: {OperationName} (Duration: {Duration}ms)",
                _operationName, duration.TotalMilliseconds);
            _scope?.Dispose();
        }
    }
}
