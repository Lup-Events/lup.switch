using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Lup.Switch.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Twilio;
using Twilio.Rest.Supersim.V1;
using Twilio.TwiML.Voice;

namespace Lup.Switch.Controllers
{
    [Route("api/[controller]")]
    public class SimController : ControllerBase
    {
        // GET api/sim
        [HttpGet]
        public IEnumerable<SimResource> Get()
        {
            return SimResource.Read().ToList();
        }

        // GET api/sim/DMPW97XBJF8J
        [HttpGet("{serial}")]
        public ActionResult<SimResource> Get(String serial)
        {
            if (String.IsNullOrEmpty(serial))
            {
                return ValidationProblem("Missing serial.");
            }
            
            var sims = SimResource.Read().ToList(); // TODO: Consider how to not get a list of all SIMs for every request

            var sim = sims.FirstOrDefault(a => a.UniqueName == serial);
            if (null == sim)
            {
                return Conflict();
            }

            return sim;
        }

        /*
        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }
*/
        
        // PUT api/sim/DMPW97XBJF8J
        [HttpPut("{serial}")]
        public ActionResult<SimResource> Put(String serial, [FromBody]SimModel value)
        {
            if (String.IsNullOrEmpty(serial))
            {
                return ValidationProblem("Missing serial.");
            }
            if (null == value)
            {
                return ValidationProblem("Missing request model.");
            }
            
            var sims = SimResource.Read().ToList();

            var sim = sims.FirstOrDefault(a => a.UniqueName == serial);
            if (null == sim)
            {
                return Conflict();
            }

            if (sim.Status.ToString() == value.Status)
            {
                return Accepted();
            }
            
            return SimResource.Update(
                pathSid: sim.Sid,
                status: value.Status
            );
        }

        /*
        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }*/
    }
}
