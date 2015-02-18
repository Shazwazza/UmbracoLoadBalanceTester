using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using LBTester.Models;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Net.Http.Client;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace LBTester.Controllers.Controllers
{
    [Route("api/[controller]")]
    public class StatusController : Controller
    {
        // GET api/status/test
        [HttpGet("{search}")]
        public IEnumerable<StatusModel> Get(string search)
        {
            return new[]
            {
                new StatusModel {Status = "error" },
                new StatusModel {Status = "ok" },
                new StatusModel {Status = "error" }
            };            
        }

        // POST api/values
        [HttpPost("{serverIndex}")]
        public async Task<int> Publish(int serverIndex)
        {
            var urls = new[] {
                "http://localhost:7300/umbraco/api/LBTestingKit/CreateContent",
                "http://lbtest1.dev/umbraco/api/LBTestingKit/CreateContent",
                "http://lbtest2.dev/umbraco/api/LBTestingKit/CreateContent"
            };

            using (var client = new HttpClient())
            {
                using (var responseMsg = await client.GetAsync(urls[serverIndex]))
                {
                    if (!responseMsg.IsSuccessStatusCode)
                    {
                        throw new Exception("Could not publish content on remote server");
                    }

                    var content = await responseMsg.Content.ReadAsStringAsync();

                    return int.Parse(content);
                }
            }
        }

    }
}
