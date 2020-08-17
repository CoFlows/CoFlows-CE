/*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

using CoFlows.Server.Utils;

using Microsoft.AspNetCore.Authorization;

namespace CoFlows.Server.Controllers
{
    public class GetDataTableModel
    {
        public string dbname { get; set; }
        public string table { get; set; }
        public string target { get; set; }
        public string search { get; set; }
    }

    public class ExecuteDataTable
    {
        public string dbname { get; set; }
        public string table { get; set; }
        public string command { get; set; }
    }
    /// <summary>
    /// Database Controller
    /// </summary>
    [Authorize, Route("[controller]/[action]")]
    public class DBController : Controller
    {
        /// <summary>
        /// Get a datatable from a database
        /// </summary>
        /// <remarks>
        /// Get DataTable adapter for Readonly Web Kernels
        /// </remarks>
        /// <param name="model">Object containing database name, table name, target columns and search criteria</param>
        [HttpPost]
        public async Task<IActionResult> GetDataTable(GetDataTableModel model)
        {
            var user = QuantApp.Kernel.User.FindUser(this.User.QID());

            if (user == null)
                return Unauthorized();

            string target = string.IsNullOrWhiteSpace(model.target) || model.target == "null" ? null : model.target;
            string search = string.IsNullOrWhiteSpace(model.search) || model.search == "null" ? null : model.search;

            var data = QuantApp.Kernel.Database.DB[model.dbname].GetDataTable(model.table, target, search);

            return Ok(data);

        }

        /// <summary>
        /// Get a data table from a database according to a specific command
        /// </summary>
        /// <remarks>
        /// Get DataTable adapter for Readonly Web Kernels as result of a specific command
        /// </remarks>
        /// <param name="model">Object containing database name, table name, sql command</param>        
        [HttpPost]
        public async Task<IActionResult> ExecuteDataTable(ExecuteDataTable model)
        {
            var user = QuantApp.Kernel.User.FindUser(this.User.QID());

            if (user == null)
                return Unauthorized();


            var data = QuantApp.Kernel.Database.DB[model.dbname].ExecuteDataTable(model.table, model.command);

            return Ok(data);
        }
    }



}
