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
    public interface IInstrumentFactory
    {
        Instrument CreateInstrument(string name, InstrumentType instrumentType, string description, Currency currency, FundingType fundingType, Boolean simulated, Boolean cloud);
        Instrument CreateInstrument(string name, InstrumentType instrumentType, string description, Currency currency, FundingType fundingType);

        IEnumerable<Instrument> FindInstruments(User user, string description);
        Instrument FindInstrument(User user, string name);
        Instrument FindSecureInstrument(int id);
        Instrument FindCleanInstrument(User user, int id);
        Instrument FindInstrument(User user, int id);
        Instrument FindInstrumentCSIUA(User user, string CSIUAMarket, int CSIDeliveryCode);
        Instrument FindInstrumentCSI(User user, int CSINumCode, int CSIDeliveryCode, Boolean onlyCache);

        List<Instrument> Instruments(User user);
        List<Instrument> InstrumentsType(User user, InstrumentType type);

        void SetProperty(Instrument instrument, string name, object value);

        void Save(Instrument instrument);
        void Remove(Instrument instrument);

        void RemoveFrom(Instrument instrument, DateTime date);

        void ClearCSICache();

        TimeSeries GetTimeSeries(Instrument instrument, TimeSeriesType tstype, DataProvider provider, Boolean LoadFromDatabase);
        void AddTimeSeriesPoint(Instrument instrument, DateTime date, double value, TimeSeriesType type, DataProvider provider, Boolean onlyMemory);
        void RemoveTimeSeries(Instrument instrument, TimeSeriesType tstype, DataProvider provider);
        void RemoveTimeSeries(Instrument instrument);

        double GetTimeSeriesPoint(Instrument instrument, DateTime date, TimeSeriesType type, DataProvider provider, TimeSeriesRollType timeSeriesRoll, int num);

        void UpdateInstrumentDB(Instrument instrument);

        void CleanMemory(Instrument instrument);

        void CleanTimeSeriesFromMemory(Instrument instrument);
    }
}
