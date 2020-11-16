/*
 * The MIT License (MIT)
 * Copyright (c) 2007-2019, Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
namespace AQI.AQILabs.Kernel
{
    public enum InterestRateTenorType
    {
        Daily = 1, Weekly = 2, Monthly = 3, Yearly = 4
    };

    public class InterestRate : Instrument
    {
        new public static AQI.AQILabs.Kernel.Factories.IInterestRateFactory Factory = null;

        private int _maturity = 0;
        private InterestRateTenorType _maturityType = 0;

        public InterestRate(Instrument instrument, int maturity, InterestRateTenorType maturityType)
            : base(instrument)
        {
            this._maturity = maturity;
            this._maturityType = maturityType;
        }

        public int Maturity
        {
            get
            {
                return this._maturity;
            }
            set
            {
                this._maturity = value;
                Factory.SetProperty(this, "Maturity", value);
            }
        }

        public InterestRateTenorType MaturityType
        {
            get
            {
                return this._maturityType;
            }
            set
            {
                this._maturityType = value;
                Factory.SetProperty(this, "MaturityType", (int)value);
            }
        }

        public double YearstoMaturity
        {
            get
            {
                double t = 1.0;
                if (MaturityType == InterestRateTenorType.Daily)
                {
                    t = 1.0 / 365.0;
                }
                else if (MaturityType == InterestRateTenorType.Weekly)
                {
                    t = 1.0 / 52.0;
                }
                else if (MaturityType == InterestRateTenorType.Monthly)
                {
                    t = 1.0 / 12.0;
                }
                return t * (double)Maturity;
            }
        }

        new public void Remove()
        {
            Factory.Remove(this);
            base.Remove();
        }

        public static InterestRate CreateInterestRate(Instrument instrument, int maturity, InterestRateTenorType maturityType)
        {
            return Factory.CreateInterestRate(instrument, maturity, maturityType);
        }
        public static InterestRate FindInterestRate(Instrument instrument)
        {
            if (instrument is InterestRate)
                return instrument as InterestRate;

            return Factory.FindInterestRate(instrument);
        }
    }
    public class Deposit : InterestRate
    {
        new public static AQI.AQILabs.Kernel.Factories.IDepositFactory Factory = null;

        private DayCountConvention _dayCountConvention;

        public Deposit(InterestRate rate, DayCountConvention dayCountConvention)
            : base(rate, rate.Maturity, rate.MaturityType)
        {
            this._dayCountConvention = dayCountConvention;
        }

        public DayCountConvention DayCountConvention
        {
            get
            {
                return this._dayCountConvention;
            }
            set
            {
                this._dayCountConvention = value;
                Factory.SetProperty(this, "DayCountConvention", (int)value);
            }
        }

        new public void Remove()
        {
            Factory.Remove(this);
            base.Remove();
        }

        public static Deposit CreateDeposit(InterestRate instrument, DayCountConvention dayCount)
        {
            return Factory.CreateDeposit(instrument, dayCount);
        }
        public static Deposit FindDeposit(InterestRate rate)
        {
            return Factory.FindDeposit(rate);
        }
    }
    public class InterestRateSwap : InterestRate
    {
        new public static AQI.AQILabs.Kernel.Factories.IInterestRateSwapFactory Factory = null;

        private int _floatFrequency;
        private InterestRateTenorType _floatFrequencyType;
        private DayCountConvention _floatDayCountConvention;
        private int _fixedFrequency;
        private InterestRateTenorType _fixedFrequencyType;
        private DayCountConvention _fixedDayCountConvention;
        private int _effective;

        public InterestRateSwap(InterestRate rate,
            int floatFrequency,
            InterestRateTenorType floatFrequencyType,
            DayCountConvention floatDayCountConvention,
            int fixedFrequency,
            InterestRateTenorType fixedFrequencyType,
            DayCountConvention fixedDayCountConvention,
            int effective)
            : base(rate, rate.Maturity, rate.MaturityType)
        {
            this._floatFrequency = floatFrequency;
            this._floatFrequencyType = floatFrequencyType;
            this._floatDayCountConvention = floatDayCountConvention;
            this._fixedFrequency = fixedFrequency;
            this._fixedFrequencyType = fixedFrequencyType;
            this._fixedDayCountConvention = fixedDayCountConvention;
            this._effective = effective;

        }

        public int FloatFrequency
        {
            get
            {
                return this._floatFrequency;
            }
            set
            {
                this._floatFrequency = value;
                Factory.SetProperty(this, "FloatFrequency", (int)value);
            }
        }

        public InterestRateTenorType FloatFrequencyType
        {
            get
            {
                return _floatFrequencyType;
            }
            set
            {
                this._floatFrequencyType = value;
                Factory.SetProperty(this, "FloatFrequencyType", (int)value);
            }
        }

        public DayCountConvention FloatDayCountConvention
        {
            get
            {
                return this._floatDayCountConvention;
            }
            set
            {
                this._floatDayCountConvention = value;
                Factory.SetProperty(this, "FloatDayCountConvention", (int)value);
            }
        }

        public int FixedFrequency
        {
            get
            {
                return this._fixedFrequency;
            }
            set
            {
                this._fixedFrequency = value;
                Factory.SetProperty(this, "FixedFrequency", value);
            }
        }

        public InterestRateTenorType FixedFrequencyType
        {
            get
            {
                return this._fixedFrequencyType;
            }
            set
            {
                this._fixedFrequencyType = value;
                Factory.SetProperty(this, "FixedFrequencyType", (int)value);
            }
        }

        public DayCountConvention FixedDayCountConvention
        {
            get
            {
                return this._fixedDayCountConvention;
            }
            set
            {
                this._fixedDayCountConvention = value;
                Factory.SetProperty(this, "FixedDayCountConvention", (int)value);
            }
        }

        public int Effective
        {
            get
            {
                return _effective;
            }
            set
            {
                this._effective = value;
                Factory.SetProperty(this, "Effective", value);
            }
        }

        new public void Remove()
        {
            Factory.Remove(this);
            base.Remove();
        }

        public static InterestRateSwap CreateInterestRateSwap(InterestRate instrument, int floatFrequency, InterestRateTenorType floatFrequencyType, DayCountConvention floatDayCount, int fixedFrequency, InterestRateTenorType fixedFrequencyType, DayCountConvention fixedDayCount, int effective)
        {
            return Factory.CreateInterestRateSwap(instrument, floatFrequency, floatFrequencyType, floatDayCount, fixedFrequency, fixedFrequencyType, fixedDayCount, effective);
        }
        public static InterestRateSwap FindInterestRateSwap(InterestRate instrument)
        {
            return Factory.FindInterestRateSwap(instrument);
        }
    }
}