/*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using QuantApp.Kernel;

using CoFlows.Server.Utils;


namespace CoFlows.Server.Controllers
{
    [Authorize, Route("[controller]/[action]")]
    public class MController : Controller
    {
        
        /// <summary>
        /// Get data in key/value pairs in the Multiverse (type)
        /// </summary>
        /// <param name="type">Type of multiverse</param>
        /// <returns>Success</returns>
        /// <response code="200">
        /// Result:
        ///
        ///     [{
        ///         'Key': key of object in Multiverse ,
        ///         'Value': Json object,
        ///     },...] 
        /// </response>
        [HttpGet]
        public async Task<IActionResult> Data(string type)
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(userId);

            if (userId != null)
            {
                if(quser == null)
                    QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();
                else
                    QuantApp.Kernel.User.ContextUser = quser.ToUserData();
            }
            else
                QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();

            M m = M.Base(type);
            var res = m.KeyValues();

            QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();

            return Ok(res);
        }

        /// <summary>
        /// Get data in raw format in the Multiverse (type)
        /// </summary>
        /// <param name="type">Type of multiverse</param>
        /// <returns>Success</returns>
        /// <response code="200">
        /// Result:
        ///
        ///     [{
        ///         'ID': Type of multiverse ,
        ///         'EntryID': ID of entry in Multiverse,
        ///         'Entry': Json object,
        ///         'Type': Type of object,
        ///         'Assembly': Assembly where the object's class is defined (only relevant for CoreCLR classes)
        ///     },...] 
        /// </response>
        [HttpGet]
        public async Task<IActionResult> RawData(string type)
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(userId);

            if (userId != null)
            {
                if(quser == null)
                    QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();
                else
                    QuantApp.Kernel.User.ContextUser = quser.ToUserData();
            }
            else
                QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();

            M m = M.Base(type);
            var res = m.RawEntries();

            QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();

            return Ok(res);
        }


        /// <summary>
        /// Save changes to in Multiverse to persistent storage
        /// </summary>
        /// <param name="type">Type of multiverse</param>
        /// <returns>Success</returns>
        /// <response code="200"></response>
        [HttpGet]
        public async Task<IActionResult> Save(string type)
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(userId);

            M m = M.Base(type);
            m.Save();

            return Ok(new { Result = "saved"});
        }
    }
}