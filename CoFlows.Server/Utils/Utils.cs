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
using System.Text;

using System.IO;
using System.IO.Compression;


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
    public class Compression
    {
        public static string Encode(string text)
        {
            if(string.IsNullOrEmpty(text))
                return "";
            return Encode(Encoding.UTF8.GetBytes(text));
        }

        public static string Encode(byte[] buffer)
        {
            byte[] compressedBytes;
        
            using (var uncompressedStream = new MemoryStream(buffer))
            {
                using (var compressedStream = new MemoryStream())
                { 
                    // setting the leaveOpen parameter to true to ensure that compressedStream will not be closed when compressorStream is disposed
                    // this allows compressorStream to close and flush its buffers to compressedStream and guarantees that compressedStream.ToArray() can be called afterward
                    // although MSDN documentation states that ToArray() can be called on a closed MemoryStream, I don't want to rely on that very odd behavior should it ever change
                    using (var compressorStream = new DeflateStream(compressedStream, CompressionLevel.Optimal, true))
                    {
                        uncompressedStream.CopyTo(compressorStream);
                    }
    
                    // call compressedStream.ToArray() after the enclosing DeflateStream has closed and flushed its buffer to compressedStream
                    compressedBytes = compressedStream.ToArray();
                }
            }
    
            return Convert.ToBase64String(compressedBytes);


            // var memoryStream = new MemoryStream();
            // using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
            // {
            //     gZipStream.Write(buffer, 0, buffer.Length);
            // }

            // memoryStream.Position = 0;

            // var compressedData = new byte[memoryStream.Length];
            // memoryStream.Read(compressedData, 0, compressedData.Length);

            // var gZipBuffer = new byte[compressedData.Length + 4];
            // Buffer.BlockCopy(compressedData, 0, gZipBuffer, 4, compressedData.Length);
            // Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gZipBuffer, 0, 4);
            // return Convert.ToBase64String(gZipBuffer);
        }
        public static string Decode(string compressedText)
        {
            if(string.IsNullOrEmpty(compressedText))
                return "";
            return Encoding.UTF8.GetString(DecodeBytes(compressedText));
        }

        public static byte[] DecodeBytes(string compressedText)
        {
            if(string.IsNullOrEmpty(compressedText))
                return Array.Empty<byte>();

            try
            {

                byte[] decompressedBytes;
            
                var compressedStream = new MemoryStream(Convert.FromBase64String(compressedText));
        
                using (var decompressorStream = new DeflateStream(compressedStream, CompressionMode.Decompress))
                {
                    using (var decompressedStream = new MemoryStream())
                    {
                        decompressorStream.CopyTo(decompressedStream);
        
                        decompressedBytes = decompressedStream.ToArray();
                    }
                }
        
                    return decompressedBytes;
            }
            catch(Exception e)
            {
            // return Encoding.UTF8.GetString(decompressedBytes);

                byte[] gZipBuffer = Convert.FromBase64String(compressedText);
                using (var memoryStream = new MemoryStream())
                {
                    int dataLength = BitConverter.ToInt32(gZipBuffer, 0);
                    memoryStream.Write(gZipBuffer, 4, gZipBuffer.Length - 4);

                    var buffer = new byte[dataLength];

                    memoryStream.Position = 0;
                    using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                    {
                        gZipStream.Read(buffer, 0, buffer.Length);
                    }

                    return buffer;
                }
            }
        }
    }
}