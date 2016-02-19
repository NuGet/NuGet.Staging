// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Mvc;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860
namespace Stage.Manager.Controllers
{
    [Route("api/[controller]")]
    public class StageController : Controller
    {
        // GET: api/stage
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/stage/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/stage
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/stage/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/stage/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
