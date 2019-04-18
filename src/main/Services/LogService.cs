﻿using System;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace PKISharp.WACS.Services
{
    public class LogService : ILogService
    {
        private Logger _screenLogger;
        private Logger _eventLogger;
        private LoggingLevelSwitch _levelSwitch;
        public bool Dirty { get; set; }

        [Flags]
        public enum LogType
        {
            None = 0,
            Screen = 1,
            Event = 2,
            Both = Screen | Event
        }

        public LogService()
        {
#if DEBUG
            var initialLevel = LogEventLevel.Debug;
#else
            var initialLevel = LogEventLevel.Information;
#endif
            _levelSwitch = new LoggingLevelSwitch(initialMinimumLevel: initialLevel);
            try
            {
                _screenLogger = new LoggerConfiguration()
                    .MinimumLevel.ControlledBy(_levelSwitch)
                    .Enrich.FromLogContext()
                    .Filter.ByIncludingOnly(x => { Dirty = true; return true; })
                    .WriteTo.Console(outputTemplate: " [{Level:u4}] {Message:l}{NewLine}{Exception}", theme: SystemConsoleTheme.Literate)
                    .ReadFrom.AppSettings()
                    .CreateLogger();

                _eventLogger = new LoggerConfiguration()
                    .MinimumLevel.ControlledBy(_levelSwitch)
                    .Enrich.FromLogContext()
                    .WriteTo.EventLog("win-acme", manageEventSource: true)
                    .ReadFrom.AppSettings("event")
                    .CreateLogger();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($" Error while creating logger: {ex.Message} - {ex.StackTrace}");
                Console.ResetColor();
                Console.WriteLine();
                Environment.Exit(ex.HResult);
            }
            Log.Debug("The global logger has been configured");
        }

        public void SetVerbose()
        {
            _levelSwitch.MinimumLevel = LogEventLevel.Verbose;
            Verbose("Verbose mode logging enabled");
        }

        public void Verbose(string message, params object[] items)
        {
            Verbose(LogType.Screen, message, items);
        }

        public void Debug(string message, params object[] items)
        {
            Debug(LogType.Screen, message, items);
        }

        public void Warning(string message, params object[] items)
        {
            Warning(LogType.Screen | LogType.Event, message, items);
        }

        public void Error(string message, params object[] items)
        {
            Error(LogType.Screen | LogType.Event, message, items);
        }

        public void Error(Exception ex, string message, params object[] items)
        {
            Error(LogType.Screen | LogType.Event, ex, message, items);
        }

        public void Information(string message, params object[] items)
        {
            Information(false, true, message, items);
        }

        public void Information(bool asEvent, string message, params object[] items)
        {
            Information(asEvent, true, message, items);
        }

        public void Information(bool asEvent, bool asScreen, string message, params object[] items)
        {
            if (asEvent || asScreen)
            {
                var type = LogType.None;
                if (asEvent)
                {
                    type |= LogType.Event;
                }
                if (asScreen)
                {
                    type |= LogType.Screen;
                }
                Information(type, message, items);
            }
        }

        private void Verbose(LogType type, string message, params object[] items)
        {
            Write(type, LogEventLevel.Verbose, message, items);
        }

        private void Debug(LogType type, string message, params object[] items)
        {
            Write(type, LogEventLevel.Debug, message, items);
        }

        private void Information(LogType type, string message, params object[] items)
        {
            Write(type, LogEventLevel.Information, message, items);
        }

        private void Warning(LogType type, string message, params object[] items)
        {
            Write(type, LogEventLevel.Warning, message, items);
        }

        private void Error(LogType type, string message, params object[] items)
        {
            Write(type, LogEventLevel.Error, message, items);
        }

        private void Error(LogType type, Exception ex, string message, params object[] items)
        {
            Write(type, LogEventLevel.Error, ex, message, items);
        }

        private void Write(LogType type, LogEventLevel level, string message, params object[] items)
        {
            Write(type, level, null, message, items);
        }

        private void Write(LogType type, LogEventLevel level, Exception ex, string message, params object[] items)
        {
            if (type.HasFlag(LogType.Screen))
            {
                _screenLogger.Write(level, ex, message, items);
            }
            if (type.HasFlag(LogType.Event))
            {
                _eventLogger.Write(level, ex, message, items);
            }
        }
    }
}
