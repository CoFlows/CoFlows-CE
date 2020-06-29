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
    // public class FilesController : Controller
    // {

    //     //[System.Web.Http.HttpPost]
    //     //public string AddHighChart(string filename, string type, int width, string svg)
    //     //{
    //     //    string userId = this.User.QID();
    //     //    if (userId == null)
    //     //        return null;
    //     //    DateTime dt = DateTime.Now;

    //     //    string id = System.Guid.NewGuid().ToString();

    //     //    svg = svg.Replace("_&l;_", "<").Replace("_&r;_", ">");

    //     //    Exporter export = new Exporter(filename, type, width, svg);

    //     //    MemoryStream outStream = new MemoryStream();
    //     //    export.WriteToStream(outStream);
    //     //    //BinaryReader breader = new BinaryReader(outStream);
    //     //    byte[] data = outStream.ToArray();// breader.ReadBytes((int)outStream.Length);

    //     //    FileRepository.AddFile(id, filename, "", type, data, dt, userId, null);

    //     //    return id;
    //     //}
        
    //     [HttpPost]
    //     public string AutoAddFile(HttpPostedFileBase file, string groupID)
    //     {
    //         string userId = this.User.QID();
    //         if (userId == null)
    //             return null;

    //         DateTime dt = DateTime.Now;

    //         string id = System.Guid.NewGuid().ToString();

    //         //svg = svg.Replace("_&l;_", "<").Replace("_&r;_", ">");

    //         //Exporter export = new Exporter(filename, type, width, svg);

    //         MemoryStream outStream = new MemoryStream();
    //         //export.WriteToStream(outStream);
    //         file.InputStream.CopyTo(outStream);
    //         //BinaryReader breader = new BinaryReader(outStream);
    //         byte[] data = outStream.ToArray();// breader.ReadBytes((int)outStream.Length);

    //         FileRepository.AddFile(id, file.FileName, "", file.ContentType, data, dt, userId, groupID);

    //         return @"{state: true, name: '" + file.FileName + "', size: '" + data.Length + "', extra: 'any_data, optional'}";
    //     }

    //     public FileContentResult File(string id)
    //     {
    //         string userId = this.User.QID();
    //         if (userId == null)
    //             return null;
    //         //Sandbox sandbox = AQIEngine.Initialize(tenantName);

    //         FilePermission filep = FileRepository.File(id);
    //         if (filep != null)
    //             return File(filep.Data, filep.Type, filep.Name);

    //         if (id.Contains("."))
    //         {
    //             filep = FileRepository.File(id.Substring(0, id.LastIndexOf(".")));
    //             if (filep != null)
    //                 return File(filep.Data, filep.Type, filep.Name);
    //         }

    //         return null;
    //     }

    //     public ActionResult Files()
    //     {
    //         string userId = this.User.QID();
    //         if (userId == null)
    //             return null;

    //         Dictionary<string, FilePermission> files = FileRepository.FilesByUser(userId);

    //         if (files == null)
    //             return null;

    //         List<object> res = new List<object>();
    //         foreach (string id in files.Keys)
    //         {
    //             string url = Url.Content("~/Files/File/") + id;
    //             res.Add(new { thumb = url, image = url });
    //         }

    //         return new JsonpResult
    //         {
    //             Data = res,
    //             JsonRequestBehavior = JsonRequestBehavior.AllowGet
    //         };
    //     }

    //     public string Remove(string id)
    //     {
    //         string userId = this.User.QID();
    //         if (userId == null)
    //             return null;

    //         FileRepository.Remove(id);

    //         return "ok";
    //     }

    //     public string EditDescription(string id, string description)
    //     {
    //         string userId = this.User.QID();
    //         if (userId == null)
    //             return null;

    //         FileRepository.EditDescription(id, description);

    //         return "ok";
    //     }

    //     [AllowCrossSiteJson]
    //     public ActionResult FilesApp(string groupid)
    //     {
    //         string userId = this.User.QID();
    //         if (userId == null)
    //             return null;

    //         AQI.AQILabs.Kernel.User user = AQI.AQILabs.Kernel.User.FindUser(userId);
    //         AQI.AQILabs.Kernel.Group role = AQI.AQILabs.Kernel.Group.FindGroup(groupid);


            

    //         List<object> jres = new List<object>();

    //         List<AQI.AQILabs.Kernel.IPermissible> files = role.List(user, typeof(FilePermission), false);
    //         foreach (FilePermission file_mem in files)
    //         {
    //             FilePermission file = FileRepository.File(file_mem.ID);
    //             if (file != null)
    //                 jres.Add(new { ID = file.ID, Name = file.Name, Owner = file.Owner.FirstName + " " + file.Owner.LastName, Size = file.Size, Date = (file.Timestamp.ToString("yyyy/MM/dd")), Type = file.Type, Description = file.Description, AccessType = role.Permission(null, file_mem).ToString() });
    //             else
    //                 role.Remove(file_mem);
    //         }

    //         files = role.List(user, typeof(AQICloud.FilePermission), false);
    //         foreach (FilePermission file_mem in files)
    //         {
    //             FilePermission file = FileRepository.File(file_mem.ID);
    //             if (file != null)
    //                 jres.Add(new { ID = file.ID, Name = file.Name, Owner = file.Owner.FirstName + " " + file.Owner.LastName, Size = file.Size, Date = (file.Timestamp.ToString("yyyy/MM/dd")), Type = file.Type, Description = file.Description, AccessType = role.Permission(null, file_mem).ToString() });
    //             else
    //                 role.Remove(file_mem);
    //         }

    //         return new JsonpResult
    //         {
    //             Data = jres,
    //             JsonRequestBehavior = JsonRequestBehavior.AllowGet
    //         };
    //     }
    // }

    // [Route("api/[controller]")]
    [Authorize, Route("[controller]/[action]")]
    // [Authorize, Route("[controller]")]
    // [Route("[controller]")]
    public class FilesController : Controller
    {

        [HttpPost, DisableRequestSizeLimit, Produces("application/json")]
		public ActionResult UploadFile(string id)
		{
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(userId);

            Console.WriteLine(id);
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
                        Console.WriteLine("Saved: " + fileName);
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

            // files = role.List(user, typeof(FilePermission), false);
            // foreach (FilePermission file_mem in files)
            // {
            //     FilePermission file = FileRepository.File(file_mem.ID);
            //     if (file != null)
            //         jres.Add(new { ID = file.ID, Name = file.Name, Owner = file.Owner.FirstName + " " + file.Owner.LastName, Size = file.Size, Date = (file.Timestamp.ToString("yyyy/MM/dd")), Type = file.Type, AccessType = role.Permission(null, file_mem).ToString() });
            //     else
            //         role.Remove(file_mem);
            // }

            // return Ok(new { Result = jres });
            return Ok(jres);
        }

        [HttpGet]
        public FileContentResult File(string id)
        {
            string userId = this.User.QID();
            Console.WriteLine("----------: " + id + " <-> " + userId);
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