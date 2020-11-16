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

namespace AQI.AQILabs.Kernel.Factories
{
    public interface IFutureFactory
    {
        Future CreateFuture(Security security, string generic_months, DateTime first_delivery, DateTime first_notice, DateTime last_delivery, DateTime first_trade, DateTime last_trade, double tick_size, double contract_size, Instrument underlying);
        Future FindFuture(Security security);
        Future FindFuture(int id);
        Future CurrentFuture(Instrument underlyingInstrument, DateTime date);
        Future CurrentFuture(Instrument underlyingInstrument, double contract_size, DateTime date);
        Boolean HasFutures(Instrument underlyingInstrument);
        List<Instrument> Underlyings();
        List<Future> ActiveFutures(Instrument underlyingInstrument, DateTime date);
        List<Future> ActiveFutures(Instrument underlyingInstrument, double contract_size, DateTime date);

        int NextID(int id, DateTime lastTradeDate);
        int PreviousID(int id, DateTime lastTradeDate);

        void SetProperty(Future future, string name, object value);

        void CleanFuturesFromMemory(DateTime date);

        void Remove(Future future);
    }
}
