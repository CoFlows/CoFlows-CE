/*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http.Headers;

using CoFlows.Server.Utils;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using QuantApp.Kernel;

namespace CoFlows.Server.Controllers
{
    [Authorize, Route("[controller]/[action]")]
    public class FilesController : Controller
    {
        /// <summary>
        /// Upload multiple files to a group (group id)
        /// </summary>
        /// <returns>Success</returns>
        /// <response code="200"></response>
        [HttpPost, DisableRequestSizeLimit, Produces("application/json")]
		public ActionResult UploadFile(string groupid)
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
                            FileRepository.AddFile(fid, fileName, data, file.ContentType, dt, userId, groupid);
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
  
        /// <summary>
        /// Get list of files in group
        /// </summary>
        /// <param name="groupid">Group ID</param>
        /// <returns>Success</returns>
        /// <response code="200">
        /// Result:
        ///
        ///     [{
        ///         "ID": "File ID",
        ///         "Name": "File name",
        ///         "Owner": "File owner / user that uploaded it",
        ///         "Size": "File size",
        ///         "Date": "Upload date",
        ///         "Type": "Content Type",
        ///         "Permission": "Permission required to access this file in a group"
        ///     }, 
        ///     ...]
        ///
        /// </response>
        [HttpGet]
        public async Task<IActionResult> Files(string groupid)
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(userId);
            QuantApp.Kernel.Group role = QuantApp.Kernel.Group.FindGroup(groupid);

            if(role == null)
                return BadRequest(new { Data = "Group not found "});

            List<object> jres = new List<object>();

            List<IPermissible> files = role.List(user, typeof(FilePermission), false);
            foreach (FilePermission file_mem in files)
            {
                FilePermission file = FileRepository.File(file_mem.ID);
                if (file != null)
                    jres.Add(new { 
                        ID = file.ID, 
                        Name = file.Name, 
                        Owner = file.Owner.FirstName + " " + file.Owner.LastName, 
                        Size = file.Size, 
                        Date = (file.Timestamp.ToString("yyyy/MM/dd")), 
                        Type = file.Type, 
                        Permission = (int)role.Permission(null, file_mem) 
                        });
                else
                    role.Remove(file_mem);
            }
            return Ok(jres);
        }

        /// <summary>
        /// Get file
        /// </summary>
        /// <param name="fid">File ID</param>
        /// <returns>Success</returns>
        /// <response code="200">FileContent</response>
        [HttpGet]
        public FileContentResult File(string fid)
        {
            string userId = this.User.QID();
            if (userId == null)
                return null;
            
            FilePermission filep = FileRepository.File(fid);
            if (filep != null)
                return File(filep.Data, filep.Type, filep.Name);

            if (fid.Contains("."))
            {
                filep = FileRepository.File(fid.Substring(0, fid.LastIndexOf(".")));
                if (filep != null)
                    return File(filep.Data, filep.Type, filep.Name);
            }

            return null;
        }

        /// <summary>
        /// Remove a file
        /// </summary>
        /// <param name="fid">File ID</param>
        /// <returns>Success</returns>
        /// <response code="200"></response>
        [HttpGet]
        public async Task<ActionResult> Remove(string fid)
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            FileRepository.Remove(fid);

            return Ok(new { Data = "ok"} );
        }
    }
}