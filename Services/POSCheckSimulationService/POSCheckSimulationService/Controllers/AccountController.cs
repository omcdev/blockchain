using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace POSCheckSimulationService.Controllers
{
    [Route("Api/[controller]/[action]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        
       //[HttpPost]
       // public ActionResult<Dictionary<string, string>> CheckAccountString([FromBody] string c)
       // {
       //     Dictionary<string, string> dic = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(c);
       //     var r = new Dictionary<string, string>();
       //     r["Data"] = "true";
       //     r["Code"] = "100000";
       //     r["Message"] = "Message";
       //     return r;
       // }

        [HttpPost]
        public ActionResult<Dictionary<string, string>> CheckAccount([FromBody] Dictionary<string, string> dic)
        {
             
            var r = new Dictionary<string, string>();
            r["Data"] = "true";
            r["Code"] = "100000";
            r["Message"] = "Message";
            return r;
        }
    }
}