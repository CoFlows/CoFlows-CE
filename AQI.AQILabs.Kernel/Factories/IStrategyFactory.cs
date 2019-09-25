/*
 * The MIT License (MIT)
 * Copyright (c) 2007-2019, Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Data;
using AQI.AQILabs.Kernel.Numerics.Util;


using QuantApp.Kernel;

namespace AQI.AQILabs.Kernel.Factories
{
    public interface IStrategyFactory
    {
        Strategy FindStrategy(Instrument instrument);

        Portfolio FindPortfolio(int id);

        void Startup(int id, string className);

        void UpdateStrategyDB(Strategy strategy);

        TimeSeries GetMemorySeries(Strategy strategy, int memorytype, int memoryclass);
        Dictionary<int[], TimeSeries> GetMemorySeries(Strategy strategy);

        IEnumerable<int[]> GetMemorySeriesIds(Strategy strategy);
        void AddMemorySeries(Strategy strategy, int memorytype, int memoryclass, TimeSeries timeseries);
        void AddMemoryPoint(Strategy strategy, DateTime date, double value, int memorytype, int memoryclass, Boolean onlyMemory);
        double GetMemorySeriesPoint(Strategy strategy, DateTime date, int memorytype, int memoryclass, TimeSeriesRollType timeSeriesRoll);

        void SetProperty(Strategy strategy, string name, object value);

        void Save(Strategy strategy);
        void Remove(Strategy strategy);

        void RemoveFrom(Strategy strategy, DateTime date);

        void ClearMemory(Strategy strategy, DateTime date);
        void ClearAUMMemory(Strategy strategy, DateTime date);

        List<Strategy> ActiveMasters(User user, DateTime date);
    }
}
