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
    public interface IInterestRateFactory
    {
        InterestRate CreateInterestRate(Instrument instrument, int maturity, InterestRateTenorType maturityType);
        InterestRate FindInterestRate(Instrument instrument);

        void SetProperty(InterestRate rate, string name, object value);

        void Remove(InterestRate rate);
    }

    public interface IDepositFactory
    {
        Deposit CreateDeposit(InterestRate instrument, DayCountConvention dayCount);
        Deposit FindDeposit(InterestRate rate);

        void SetProperty(Deposit rate, string name, object value);

        void Remove(Deposit rate);
    }

    public interface IInterestRateSwapFactory
    {
        InterestRateSwap CreateInterestRateSwap(InterestRate instrument, int floatFrequency, InterestRateTenorType floatFrequencyType, DayCountConvention floatDayCount, int fixedFrequency, InterestRateTenorType fixedFrequencyType, DayCountConvention fixedDayCount, int effective);
        InterestRateSwap FindInterestRateSwap(InterestRate instrument);

        void SetProperty(InterestRateSwap swap, string name, object value);

        void Remove(InterestRateSwap swap);
    }
}
