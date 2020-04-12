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
using System.Web;

using Microsoft.FSharp.Collections;

namespace CoFlows.Server.Utils
{

    public static class FSharpInteropExtensions
    {
        public static FSharpList<TItemType> ToFSharplist<TItemType>(this IEnumerable<TItemType> myList)
        {
            return Microsoft.FSharp.Collections.ListModule.OfSeq<TItemType>(myList);
        }

        public static IEnumerable<TItemType> ToEnumerable<TItemType>(this FSharpList<TItemType> fList)
        {
            return Microsoft.FSharp.Collections.SeqModule.OfList<TItemType>(fList);
        }
    }
    public class Utils
    {
        public static long ToUnixTimestamp(System.DateTime dt)
        {
            DateTime unixRef = new DateTime(1970, 1, 1, 0, 0, 0);
            return (dt.Ticks - unixRef.Ticks) / 10000000;
        }

        public static DateTime FromUnixTimestamp(long timestamp)
        {
            DateTime unixRef = new DateTime(1970, 1, 1, 0, 0, 0);
            return unixRef.AddSeconds(timestamp);
        }

        public static long ToJSTimestamp(System.DateTime dt)
        {
            DateTime unixRef = new DateTime(1970, 1, 1, 0, 0, 0);
            return (long)(dt - unixRef).TotalMilliseconds;
        }
    }
}