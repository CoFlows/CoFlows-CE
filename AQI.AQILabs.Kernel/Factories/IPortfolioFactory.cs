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
    public interface IPortfolioFactory
    {
        Portfolio CreatePortfolio(Instrument instrument, Instrument deposit, Instrument borrow, Portfolio parent);
        Portfolio FindPortfolio(Instrument instrument);
        Portfolio FindPortfolio(Instrument instrument, Boolean loadPositionsInMemory);

        List<Portfolio> FindPortfolio(string custodian, string accountid);

        Strategy FindStrategy(int id);
        Portfolio FindParentPortfolio(int id);

        void UpdatePortfolioDB(Portfolio instrument);

        void SaveNewPositions(Portfolio portfolio);

        void UpdateOrder(Order order);

        bool ProcessedCorporateAction(Portfolio portfolio, CorporateAction action);

        void ProcessCorporateAction(Portfolio portfolio, CorporateAction action);

        DateTime LastOrderTimestamp(Portfolio portfolio, DateTime date);
        DateTime LastPositionTimestamp(Portfolio portfolio, DateTime date);

        DateTime FirstPositionTimestamp(Portfolio portfolio, DateTime date);


        void Remove(Portfolio portfolio);

        void RemoveFrom(Portfolio portfolio, DateTime date);

        void RemovePositionsFrom(Portfolio portfolio, DateTime date);

        void RemoveOrdersFrom(Portfolio portfolio, DateTime date);

        void RemoveReserves(Portfolio portfolio);

        void LoadReserves(Portfolio portfolio);

        void AddReserve(Portfolio portfolio, Currency ccy, Instrument longInstrument, Instrument shortInstrument);

        List<Position> LoadPositions(Portfolio portfolio, DateTime date);
        List<Order> LoadOrders(Portfolio portfolio, DateTime date);

        void AddNewOrder(Order o);

        void AddNewPositionMessage(Portfolio.PositionMessage p);

        void SetProperty(Portfolio portfolio, string name, object value);
    }
}
