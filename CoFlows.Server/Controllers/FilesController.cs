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
using System.Threading.Tasks;


using CoFlows.Server.Models;
using CoFlows.Server.Utils;

using System.Net;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Formatting;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

using Newtonsoft.Json;
using QuantApp.Kernel;
using QuantApp.Engine;
using Python.Runtime;

using Newtonsoft.Json;

namespace CoFlows.Server.Controllers
{
    [Authorize, Route("[controller]/[action]")]
    public class FilesController : Controller
    {

        [HttpPost, DisableRequestSizeLimit, Produces("application/json")]
		public ActionResult UploadFile(string id)
		{
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(userId);
			try
			{
                foreach(var file in Request.Form.Files)
                {
                    if (file.Length > 0)
                    {
                        string fileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
                        {
                            
                            MemoryStream outStream = new MemoryStream();
                            file.CopyTo(outStream);

                            byte[] data = outStream.ToArray();
                            string fid = System.Guid.NewGuid().ToString();
                            DateTime dt = DateTime.Now;
                            FileRepository.AddFile(fid, fileName, data, file.ContentType, dt, userId, id);
                        }
                    }
                }
				return Json("Upload Successful.");
			}
			catch (System.Exception ex)
			{
				return Json("Upload Failed: " + ex.Message);
			}
		}
  
        [HttpGet]
        public async Task<IActionResult> Files(string id)
        {
            string userId = this.User.QID();
            if (userId == null)
                return null;

            QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(userId);
            QuantApp.Kernel.Group role = QuantApp.Kernel.Group.FindGroup(id);

            List<object> jres = new List<object>();

            List<IPermissible> files = role.List(user, typeof(FilePermission), false);
            foreach (FilePermission file_mem in files)
            {
                FilePermission file = FileRepository.File(file_mem.ID);
                if (file != null)
                    jres.Add(new { ID = file.ID, Name = file.Name, Owner = file.Owner.FirstName + " " + file.Owner.LastName, Size = file.Size, Date = (file.Timestamp.ToString("yyyy/MM/dd")), Type = file.Type, Permission = (int)role.Permission(null, file_mem) });
                else
                    role.Remove(file_mem);
            }
            return Ok(jres);
        }

        [HttpGet]
        public FileContentResult File(string id)
        {
            string userId = this.User.QID();
            if (userId == null)
                return null;
            
            FilePermission filep = FileRepository.File(id);
            if (filep != null)
                return File(filep.Data, filep.Type, filep.Name);

            if (id.Contains("."))
            {
                filep = FileRepository.File(id.Substring(0, id.LastIndexOf(".")));
                if (filep != null)
                    return File(filep.Data, filep.Type, filep.Name);
            }

            return null;
        }

        [HttpGet]
        public string Remove(string id)
        {
            string userId = this.User.QID();
            if (userId == null)
                return null;

            FileRepository.Remove(id);

            return "ok";
        }
    }
}