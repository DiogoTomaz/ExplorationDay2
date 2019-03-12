using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace FawltyTowers.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BasilController : Controller
    {
        static int getCalls = 0;
        static int getSybillCalls = 0;

        [HttpGet]
        public async Task<ActionResult<string>> Get()
        {
            getCalls++;
            await Task.Delay(500);

            // third time is the charm 
            if(getCalls % 3 == 0)
            {
                return "Basil Fawlty";
            }

            return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
        }

        [Route("whois")]
        [HttpGet]
        public async Task<ActionResult<string>> WhoIs()
        {            
            if(!Request.Headers.Keys.Contains("Authorization"))
            {
                return new StatusCodeResult((int)HttpStatusCode.Unauthorized);
            }

            await Task.Delay(500);
            return "The Frantic Boss";
        }

        [Route("callmanuel")]
        [HttpGet]
        public ActionResult<string> BetterCallManuel()
        {
            return new StatusCodeResult((int)HttpStatusCode.Forbidden);
        }

        [Route("sybill")]
        [HttpGet]
        public async Task<ActionResult<string>> GetSybill()
        {
            getSybillCalls++;
            
            // third time is the charm 
            if (getSybillCalls % 3 != 0)
            {
                await Task.Delay(10000);
            }
            else
            {
                await Task.Delay(500);
            }

            return "Sybill Fawlty";
        }
    }
}
