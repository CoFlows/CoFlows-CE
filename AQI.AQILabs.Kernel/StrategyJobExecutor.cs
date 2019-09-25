/*
 * The MIT License (MIT)
 * Copyright (c) 2007-2019, Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Quartz;
using Quartz.Impl;

namespace AQI.AQILabs.Kernel
{
    /// <summary>
    /// Class managing the logic of the Quartz based scheduler for the execution of strategy logic.
    /// </summary>
    public class StrategyJobExecutor
    {

        private Strategy _strategy = null;

        /// <summary>
        /// Constructor
        /// </summary>
        public StrategyJobExecutor(Strategy strategy)
        {
            _strategy = strategy;

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
            string jobID = "Strategy Job " + _strategy.ID + " " + schedule;

            Console.WriteLine("STARTING: " + jobID);

            IJobDetail job = JobBuilder.Create<StrategyJob>()
                .WithIdentity(jobID, "Strategy Group " + _strategy.ID)
                .Build();

            if (schedule.StartsWith("M:"))
            {
                job.JobDataMap["ExecutionType"] = "Mark";
                schedule = schedule.Replace("M:", "");
            }
            else if (schedule.StartsWith("E:"))
            {
                job.JobDataMap["ExecutionType"] = "Execute";
                schedule = schedule.Replace("E:", "");
            }

            else
                job.JobDataMap["ExecutionType"] = "All";

            ICronTrigger trigger = (ICronTrigger)TriggerBuilder.Create()
                .WithIdentity(jobID, "Strategy Group " + _strategy.ID)
                .StartNow()
                .WithCronSchedule(schedule)
                .ForJob(job.Key)
                .Build();

            job.JobDataMap["Strategy"] = _strategy;

            _sched.ScheduleJob(job, trigger);
            //_sched.AddJob(job,true);
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
                            Console.WriteLine("STOPING: " + jobKey);

                            if (group == ("Strategy Group " + _strategy.ID))
                                _sched.DeleteJob(jobKey);
                        }
                    }
                }

                //var x = _sched.GetJobKeys<IJobDetail>();
                //_sched.PauseJob(_sched.GetJobKeys(IJ))
                //foreach (var x in _sched.PauseJob()
                //    Console.WriteLine("STOPPING: " + x);
                //_sched.Shutdown(true);
            }
        }
    }

    /// <summary>
    /// Class representing the strategy execution job
    /// </summary>
    public class StrategyJob : IJob
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


            JobDataMap dataMap = context.JobDetail.JobDataMap;


            Strategy strategy = (Strategy)dataMap["Strategy"];
            if (!strategy.Executing)
            {
                lock (strategy.ExecutionLock)
                {



                    string type = (string)dataMap["ExecutionType"];

                    // This job simply prints out its job name and the
                    // date and time that it is running
                    JobKey jobKey = context.JobDetail.Key;
                    DateTime date = DateTime.Now;
                    //date = Round(date, new TimeSpan(0, 1, 0));
                    date = Round(date, new TimeSpan(0, 0, 1));

                    //SystemLog.Write(date, null, SystemLog.Type.Production, string.Format("StrategyJob says: {0} executing: {1}", jobKey, strategy));

                    if (strategy.JobCalculation != null)
                        strategy.JobCalculation(date);
                    else
                    {
                        strategy.Portfolio.CanSave = false;
                        Console.WriteLine("RUNNING: " + this.GetHashCode() + " " + strategy + " " + strategy.SimulationObject + " " + DateTime.Now.ToString("hh:mm:ss.fff") + " " + date.ToString("hh:mm:ss.fff"));

                        if (type != "Execute")
                        {
                            strategy.Tree.PreNAVCalculation(date);
                            strategy.Tree.ManageCorporateActions(date);                            
                            strategy.Tree.NAVCalculation(date);
                            strategy.Tree.MarginFutures(date);
                        }

                        if ((context.NextFireTimeUtc != null && context.NextFireTimeUtc.Value.Date == date.Date && type != "Mark") || type == "Execute" || type == "All")
                        {
                            strategy.Tree.ExecuteLogic(date);
                            strategy.Tree.PostExecuteLogic(date);
                            strategy.Portfolio.SubmitOrders(date);
                        }

                        strategy.Portfolio.CanSave = true;

                        if (!strategy.SimulationObject)
                        {
                            //strategy.Tree.SaveNewPositions();
                            strategy.Tree.Save();
                        }

                        Console.WriteLine(string.Format("StrategyJob says: {0} executed: {1}", jobKey, strategy) + " " + this.GetHashCode() + " " + DateTime.Now.ToString("hh:mm:ss.fff"));
                    }

                    strategy.Executing = false;
                }
            }

            return Task.CompletedTask;
        }
    }
}
