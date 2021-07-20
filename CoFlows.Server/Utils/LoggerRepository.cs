/*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
 
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Data;

using QuantApp.Kernel;

using NLog;

namespace CoFlows.Server.Utils
{

    public class LoggerRepository
    {
        public static string RuntimeID = System.Guid.NewGuid().ToString();
        public class LogEntry
        {
            public string RuntimeID { get; set; }
            public string ID { get; set; }
            public DateTime Timestamp { get; set; }
            public string Level { get; set; }
            public string ClassName { get; set; }
            public string MemberName { get; set; }
            public int LineNumber { get; set; }
            public int SequenceID { get; set; }
            public string Message { get; set; }
        }

        private static T GetValue<T>(DataRow row, string columnname)
        {
            object res = null;
            if (row.RowState == DataRowState.Detached)
                return (T)res;
            if (typeof(T) == typeof(string))
                res = "";
            else if (typeof(T) == typeof(int))
                res = 0;
            else if (typeof(T) == typeof(double))
                res = 0.0;
            else if (typeof(T) == typeof(DateTime))
                res = DateTime.MinValue;
            else if (typeof(T) == typeof(bool))
                res = false;
            object obj = row[columnname];
            if (obj is DBNull)
                return (T)res;

            if (typeof(T) == typeof(int))
                return (T)(object)Convert.ToInt32(obj);
            return (T)obj;
        }

        public static void AddEvent(string ID, LogEventInfo logEvent)
        {
            try
            {
                string cm;

                if(Database.DB["CloudApp"] is QuantApp.Kernel.Adapters.SQL.PostgresDataSetAdapter)
                    cm = $"INSERT INTO Logger (RuntimeID, ID, Timestamp, Level, ClassName, MemberName, LineNumber, SequencyID, Message) values (@RuntimeID, @ID, @Timestamp, @Level, @ClassName, @MemberName, @LineNumber, @SequencyID, @Message) ON CONFLICT ON CONSTRAINT Logger_pkey DO NOTHING;";

                else if(Database.DB["CloudApp"] is QuantApp.Kernel.Adapters.SQL.SQLiteDataSetAdapter)
                    cm = $"INSERT INTO Logger (RuntimeID, ID, Timestamp, Level, ClassName, MemberName, LineNumber, SequencyID, Message) values (@RuntimeID, @ID, @Timestamp, @Level, @ClassName, @MemberName, @LineNumber, @SequencyID, @Message) ON CONFLICT DO NOTHING;";

                else
                    cm = $"INSERT IGNORE INTO Logger (RuntimeID, ID, Timestamp, Level, ClassName, MemberName, LineNumber, SequencyID, Message) values (@RuntimeID, @ID, @Timestamp, @Level, @ClassName, @MemberName, @LineNumber, @SequencyID, @Message);";
            
                Database.DB["CloudApp"].ExecuteCommand(
                    cm, 
                    new Tuple<string,object>[] { 
                        new Tuple<string,object>("RuntimeID", RuntimeID),
                        new Tuple<string,object>("ID", ID),
                        new Tuple<string,object>("Timestamp", logEvent.TimeStamp.ToString("yyyy-MM-dd HH:mm:ss.fff")),
                        new Tuple<string,object>("Level", logEvent.Level),
                        new Tuple<string,object>("ClassName", logEvent.CallerClassName),
                        new Tuple<string,object>("MemberName", logEvent.CallerMemberName),
                        new Tuple<string,object>("LineNumber", logEvent.CallerLineNumber),
                        new Tuple<string,object>("SequencyID", logEvent.SequenceID),
                        new Tuple<string,object>("Message", logEvent.Message)
                    });
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static void AddEvent(LogEntry logEvent)
        {
            try
            {
                string cm;

                if(Database.DB["CloudApp"] is QuantApp.Kernel.Adapters.SQL.PostgresDataSetAdapter)
                    cm = $"INSERT INTO Logger (RuntimeID, ID, Timestamp, Level, ClassName, MemberName, LineNumber, SequencyID, Message) values (@RuntimeID, @ID, @Timestamp, @Level, @ClassName, @MemberName, @LineNumber, @SequencyID, @Message) ON CONFLICT ON CONSTRAINT Logger_pkey DO NOTHING;";
                
                else if(Database.DB["CloudApp"] is QuantApp.Kernel.Adapters.SQL.SQLiteDataSetAdapter)
                    cm = $"INSERT INTO Logger (RuntimeID, ID, Timestamp, Level, ClassName, MemberName, LineNumber, SequencyID, Message) values (@RuntimeID, @ID, @Timestamp, @Level, @ClassName, @MemberName, @LineNumber, @SequencyID, @Message) ON CONFLICT DO NOTHING;";

                else
                    cm = $"INSERT IGNORE INTO Logger (RuntimeID, ID, Timestamp, Level, ClassName, MemberName, LineNumber, SequencyID, Message) values (@RuntimeID, @ID, @Timestamp, @Level, @ClassName, @MemberName, @LineNumber, @SequencyID, @Message);";
            

                Database.DB["CloudApp"].ExecuteCommand(
                    cm, 
                    new Tuple<string,object>[] { 
                        new Tuple<string,object>("RuntimeID", logEvent.RuntimeID),
                        new Tuple<string,object>("ID", logEvent.ID),
                        new Tuple<string,object>("Timestamp", logEvent.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff")),
                        new Tuple<string,object>("Level", logEvent.Level),
                        new Tuple<string,object>("ClassName", logEvent.ClassName),
                        new Tuple<string,object>("MemberName", logEvent.MemberName),
                        new Tuple<string,object>("LineNumber", logEvent.LineNumber),
                        new Tuple<string,object>("SequencyID", logEvent.SequenceID),
                        new Tuple<string,object>("Message", logEvent.Message)
                    });
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static List<LogEntry> ClusterMonthlyCosts(string id)
        {
            string str =  
            @"
            SELECT 
                ID, Timestamp, Level, ClassName, MemberName, LineNumber, SequencyID, Message
            FROM Logger
            WHERE ID = '" + id + @"'
            ";

            DataTable _dataTable = Database.DB["CloudApp"].ExecuteDataTable("table", str);

            DataRowCollection rows = _dataTable.Rows;

            List<LogEntry> result = new List<LogEntry>();

            if (rows.Count != 0)
            {
                foreach (DataRow row in rows)
                    result.Add(new LogEntry(){ 

                        
                        ID = GetValue<string>(row, "ID"),
                        Timestamp = GetValue<DateTime>(row, "Timestamp"),
                        Level = GetValue<string>(row, "Level"),
                        ClassName = GetValue<string>(row, "ClassName"),
                        MemberName = GetValue<string>(row, "MemberName"),
                        LineNumber = GetValue<int>(row, "LineNumber"),
                        SequenceID = GetValue<int>(row, "SequenceID"),
                        Message = GetValue<string>(row, "Message"),
                        });
            }

            return result;
        }
    }
}