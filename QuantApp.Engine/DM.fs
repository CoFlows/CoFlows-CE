(*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 *)

namespace QuantApp.Engine

open System


type public LatLong =
    {
        lat : float
        lng : float
    }



module DM =

    type public ResultChart = { Date : int64 ; Value : float }
    type public ResultMarker = { Name : string; Latitude: float; Longitude: float; Angle: float; Color: string; Text: string }
    
    type public ResultPolygon = { Name : string; Text: string; Coordinates : LatLong list }
    type public ResultLink = { Name: string; Function: string; D_link: string }
    type public ResultM = { M_link: string }
    let Link (name: string, func: string) (p : string[]) =
        let parameters = p |> Array.mapi(fun i p -> "p[" + i.ToString() + "]=" + p) |> Array.fold(fun acc p -> acc + "&" + p) ""
        { Name = name; Function = func; D_link = "name=" + func + parameters }