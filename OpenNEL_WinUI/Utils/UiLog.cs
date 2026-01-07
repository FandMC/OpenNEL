/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace OpenNEL_WinUI.Utils
{
    public static class UiLog
    {
        public static event Action<string> Logged;
        static readonly object _lock = new object();
        static readonly System.Collections.Generic.List<string> _buffer = new System.Collections.Generic.List<string>();
        class Sink : ILogEventSink
        {
            readonly MessageTemplateTextFormatter _formatter = new MessageTemplateTextFormatter("{Timestamp:HH:mm:ss} [{Level}] {Message:lj}{NewLine}{Exception}");
            public void Emit(LogEvent logEvent)
            {
                using var sw = new System.IO.StringWriter();
                _formatter.Format(logEvent, sw);
                var s = sw.ToString();
                try
                {
                    lock (_lock)
                    {
                        _buffer.Add(s);
                        if (_buffer.Count > 2000) _buffer.RemoveAt(0);
                    }
                }
                catch { }
                try { Logged?.Invoke(s); } catch { }
            }
        }
        public static ILogEventSink CreateSink() => new Sink();
        public static System.Collections.Generic.IReadOnlyList<string> GetSnapshot()
        {
            lock (_lock)
            {
                return _buffer.ToArray();
            }
        }
    }
}
