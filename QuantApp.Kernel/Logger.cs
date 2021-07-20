/*
 * The MIT License (MIT)
 * Copyright (c) 2007-2019, Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;

using NLog;
using NLog.Targets;

namespace QuantApp.Kernel 
{ 
    [Target("QuantApp.Logger")] 
    public sealed class Logger: TargetWithLayout 
    { 
        public class Types
        {
            public bool Debug { get; set; }
            public bool Error { get; set; }
            public bool Fatal { get; set; }
            public bool Trace { get; set; }
            public bool Warn { get; set; }
            public bool Info { get; set; }
        }
        public class Config
        {
            public IEnumerable<string> IgnoreConsole { get; set; }
            public Types Types { get; set; }
        }
        private object _MessageLock= new object();

        private static Config _config = new Config { IgnoreConsole = new List<string>(), Types = new Types{ Debug = true, Error = true, Fatal = true, Trace = true, Warn = true, Info = true }};
        public static void SetConfig(Config config)
        {
            _config = config;
        }

        private static string _id = null;

        public static void SetID(string id)
        {
            _id = id;
        }

        public delegate void AddEvent(string ID, LogEventInfo logEvent);
        public static AddEvent AddEventFunction = null;

        protected override void Write(LogEventInfo logEvent) 
        {             
            lock (_MessageLock)
            {
                try
                {
                    if(AddEventFunction != null)
                        AddEventFunction(_id, logEvent);
                }
                catch{}

                if(_config != null)
                    foreach(var ig in _config.IgnoreConsole)
                        if(logEvent.CallerClassName.StartsWith(ig))
                            return;
                
                ConsoleColor originalColor = Console.ForegroundColor;

                var print = false;

                if(logEvent.Level == LogLevel.Debug && (_config != null && _config.Types.Debug))
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    print = true;
                }
                
                else if(logEvent.Level == LogLevel.Error && (_config != null && _config.Types.Error))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    print = true;
                }

                else if(logEvent.Level == LogLevel.Fatal && (_config != null && _config.Types.Fatal))
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    print = true;
                }

                else if(logEvent.Level == LogLevel.Trace && (_config != null && _config.Types.Trace))
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    print = true;
                }

                else if(logEvent.Level == LogLevel.Warn && (_config != null && _config.Types.Warn))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    print = true;
                }
                
                else if(logEvent.Level == LogLevel.Info && (_config != null && _config.Types.Info))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    print = true;
                }

                if(print)
                {
                    Console.Write($"{logEvent.TimeStamp} |{logEvent.Level,-5}|");
                
                    Console.ForegroundColor = ConsoleColor.DarkBlue;
                    Console.Write($" {logEvent.CallerClassName}.{logEvent.CallerMemberName}");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine($" - {logEvent.Message}");

                    Console.ForegroundColor = originalColor;
                    Console.ResetColor();
                }

            }
 
        } 
 
    } 
}