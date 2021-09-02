using System;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using Twilio.Rest.Supersim.V1;

namespace Lup.Switch.Handlers
{
    public static class SimHandler
    {
        private const Int32 CacheAge = 5; // min
        private static readonly MemoryCache Cache = new MemoryCache(new MemoryCacheOptions());

        public static SimResource GetByName(string name)
        {
            if (null == name)
            {
                throw new ArgumentNullException(nameof(name));
            }

            // Check if in cache
            if (!Cache.TryGetValue(name, out var sim))
            {
                // Get full list of SIMs
                var sims = SimResource.Read().ToList();

                // Iterate list...
                foreach (var s in sims)
                {
                    // Populate cache
                    SetCache(s);

                    // If this is the value we were looking for anyway, note i t
                    if (s.UniqueName == name)
                    {
                        sim = s;
                    }
                }
            }

            return (SimResource)sim;
        }

        public static SimResource UpdateStatus(String sid, SimResource.StatusUpdateEnum status)
        {
            if (null == sid)
            {
                throw new ArgumentNullException(nameof(sid));
            }
            
            // Update
            var sim = SimResource.Update(
                pathSid: sid,
                status: status
            );
            
            // Set cache
            SetCache(sim);

            return sim;
        }

        private static void SetCache(SimResource sim)
        {
            // We can't cache SIMs with a unique name
            if (sim.UniqueName == null)
            {
                return;
            }
            
            // Update cache
            Cache.Set(sim.UniqueName, sim, TimeSpan.FromMinutes(CacheAge));
        }
    }
}