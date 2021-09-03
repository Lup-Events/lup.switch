using System;
using System.Collections.Generic;
using System.Linq;
using Lup.Switch.Models;
using Microsoft.AspNetCore.Mvc;
using Twilio.Rest.Supersim.V1;
using Lup.Switch.Handlers;
using Twilio.Exceptions;
using Twilio.Rest.Verify.V2.Service;

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

            var sim = SimHandler.GetByName(serial);
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
            
            var sim = SimHandler.GetByName(serial);
            if (null == sim)
            {
                return Conflict();
            }

            if (sim.Status.ToString() == value.Status)
            {
                return Accepted();
            }

            try
            {
                // A "ready" SIM can't transition directly to "inactive", it must be "active" first
                if (sim.Status == SimResource.StatusEnum.Ready 
                    && value.Status.ToUpperInvariant() == "INACTIVE")
                {
                    sim = SimHandler.UpdateStatus(sim.Sid, SimResource.StatusUpdateEnum.Active);
                }
                
                // Update status
                sim = SimHandler.UpdateStatus(sim.Sid, value.Status);
            }catch(ApiException ex){
                return Problem(ex.Message);
            }
            return sim;
        }

        /*
        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }*/
    }
}
