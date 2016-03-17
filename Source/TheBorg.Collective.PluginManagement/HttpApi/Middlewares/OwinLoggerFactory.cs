// The MIT License(MIT)
// 
// Copyright(c) 2016 Rasmus Mikkelsen
// https://github.com/rasmus/TheBorg
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

using System;
using System.Diagnostics;
using Microsoft.Owin.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using ILogger = Serilog.ILogger;

namespace TheBorg.Collective.PluginManagement.HttpApi.Middlewares
{
    public class OwinLoggerFactory : ILoggerFactory
    {
        private readonly Func<ILogger> _loggerFactory;
        private readonly Func<TraceEventType, LogEventLevel> _mapLogLevel;

        public OwinLoggerFactory(ILogger logger = null, Func<TraceEventType, LogEventLevel> getLogEventLevel = null)
        {
            _loggerFactory = logger == null
                ? (Func<ILogger>)(() => Log.Logger)
                : (() => logger);
            _mapLogLevel = getLogEventLevel ?? ToLogEventLevel;
        }

        public Microsoft.Owin.Logging.ILogger Create(string name)
        {
            return new OwinLogger(_loggerFactory().ForContext(Constants.SourceContextPropertyName, name), _mapLogLevel);
        }

        private static LogEventLevel ToLogEventLevel(TraceEventType traceEventType)
        {
            switch (traceEventType)
            {
                case TraceEventType.Critical:
                    return LogEventLevel.Fatal;
                case TraceEventType.Error:
                    return LogEventLevel.Error;
                case TraceEventType.Warning:
                    return LogEventLevel.Warning;
                case TraceEventType.Information:
                    return LogEventLevel.Information;
                case TraceEventType.Verbose:
                    return LogEventLevel.Verbose;
                case TraceEventType.Start:
                    return LogEventLevel.Debug;
                case TraceEventType.Stop:
                    return LogEventLevel.Debug;
                case TraceEventType.Suspend:
                    return LogEventLevel.Debug;
                case TraceEventType.Resume:
                    return LogEventLevel.Debug;
                case TraceEventType.Transfer:
                    return LogEventLevel.Debug;
                default:
                    throw new ArgumentOutOfRangeException(nameof(traceEventType));
            }
        }

        private class OwinLogger : Microsoft.Owin.Logging.ILogger
        {
            private readonly ILogger _logger;
            private readonly Func<TraceEventType, LogEventLevel> _getLogEventLevel;

            internal OwinLogger(ILogger logger, Func<TraceEventType, LogEventLevel> getLogEventLevel)
            {
                _logger = logger;
                _getLogEventLevel = getLogEventLevel;
            }

            public bool WriteCore(TraceEventType eventType, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
            {
                var level = _getLogEventLevel(eventType);
                if (state == null)
                {
                    return _logger.IsEnabled(level);
                }

                if (!_logger.IsEnabled(level))
                {
                    return false;
                }

                _logger.Write(level, exception, formatter(state, exception));

                return true;
            }
        }
    }
}
