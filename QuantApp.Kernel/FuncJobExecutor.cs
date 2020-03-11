/*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
 
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Quartz;
using Quartz.Impl;

namespace QuantApp.Kernel
{
    /// <summary>
    /// Class managing the logic of the Quartz based scheduler for the execution of strategy logic.
    /// </summary>
    [DisallowConcurrentExecution]
    public class FuncJobExecutor
    {

        private Func<DateTime, string, int> _func = null;
        private string _name = null;

        /// <summary>
        /// Constructor
        /// </summary>
        public FuncJobExecutor(string name, Func<DateTime, string, int> func)
        {
            _name = name;
            _func = func;

            ISchedulerFactory sf = new StdSchedulerFactory();
            _sched = (sf.GetScheduler()).Result;
            _sched.Start();
        }

        IScheduler _sched = null;

        /// <summary>
        /// Function: start job with a given schedule
        /// </summary>
        /// <param name="schedule">Quartz formatted schedule</param>
        public void StartJob(string schedule)
        {
            string jobID = "Job: " + _name + " " + schedule;

            Console.WriteLine("Starting " + jobID);

            IJobDetail job = JobBuilder.Create<FuncJob>()
                .WithIdentity(jobID, "Func Group " + _name)
                .Build();

            if (schedule.Contains("|"))
            {
                var pair = schedule.Split('|');

                job.JobDataMap["ExecutionType"] = pair[0];
                schedule = pair[1];
            }
            else
                job.JobDataMap["ExecutionType"] = "All";

            ICronTrigger trigger = (ICronTrigger)TriggerBuilder.Create()
                .WithIdentity(jobID, "Func Group " + _name)
                .StartNow()
                .WithCronSchedule(schedule)
                .ForJob(job.Key)
                .Build();

            job.JobDataMap["Func"] = _func;

            _sched.ScheduleJob(job, trigger);

            _sched.RescheduleJob(trigger.Key, trigger);
            _sched.ResumeJob(job.Key);
        }

        /// <summary>
        /// Function: stop job
        /// </summary>
        public void StopJob()
        {
            if (_sched != null)
            {
                IReadOnlyCollection<string> jobGroups = (_sched.GetJobGroupNames()).Result;
                IReadOnlyCollection<string> triggerGroups = (_sched.GetTriggerGroupNames()).Result;

                foreach (string group in jobGroups)
                {
                    var groupMatcher = Quartz.Impl.Matchers.GroupMatcher<JobKey>.GroupContains(group);
                    var jobKeys = (_sched.GetJobKeys(groupMatcher)).Result;
                    foreach (var jobKey in jobKeys)
                    {
                        var detail = _sched.GetJobDetail(jobKey);
                        var triggers = (_sched.GetTriggersOfJob(jobKey)).Result;
                        foreach (ITrigger trigger in triggers)
                        {
                            if (group == ("Func Group " + _name))
                            {
                                _sched.DeleteJob(jobKey);
                                Console.WriteLine("STOPING: " + jobKey);
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Class representing the strategy execution job
    /// </summary>
    public class FuncJob : IJob
    {
        private static DateTime Round(DateTime dateTime, TimeSpan interval)
        {
            var halfIntervelTicks = (interval.Ticks + 1) >> 1;

            return dateTime.AddTicks(halfIntervelTicks - ((dateTime.Ticks + halfIntervelTicks) % interval.Ticks));
        }


        /// <summary>
        /// Function: implemention of Quartz.Net job in the following steps.
        /// 1) PreNAV Calculation
        /// 2) Manage Corporate Actions
        /// 3) Margin Futures
        /// 4) Hedge FX
        /// 5) NAV Calculations
        /// 6) Execute Logic
        /// 7) Post Logic Execution
        /// 8) Submit Orders
        /// 9) Save data and new positions
        /// </summary>
        public virtual Task Execute(IJobExecutionContext context)
        {   
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-GB");

            try
            {
                JobDataMap dataMap = context.JobDetail.JobDataMap;

                Func<DateTime, string, int> func = (Func<DateTime, string, int>)dataMap["Func"];

                string type = (string)dataMap["ExecutionType"];

                // This job simply prints out its job name and the
                // date and time that it is running
                JobKey jobKey = context.JobDetail.Key;
                DateTime date = DateTime.Now;
                //date = Round(date, new TimeSpan(0, 1, 0));
                date = Round(date, new TimeSpan(0, 0, 1));

                func(date, type);
                // Console.WriteLine(string.Format("FuncJob says: {0} executed", jobKey) + " " + this.GetHashCode() + " " + DateTime.Now.ToString("hh:mm:ss.fff"));
            }
            catch(Exception e)
            {
                Console.WriteLine("FuncJob says: " + this.GetHashCode() + " " + DateTime.Now.ToString("hh:mm:ss.fff"));
                Console.WriteLine(e);
            }
            
            return Task.CompletedTask;
        }
    }
}
