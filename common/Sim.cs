using System;
using System.Collections;

namespace Lup.Switch
{
    public class Sim
    {
        public const String SuperSimPrefix = "8988307";
        public static Sim GetByIccid(String iccid)
        {
            throw new NotImplementedException();
        }
        
        public static IList<Sim> GetAll()
        {
            throw new NotImplementedException();
        }
        

        public static Sim GetByLabel(String label)
        {
            
        }
        
        public static void DeactivateAll()
        {
            // Deactivate all SuperSIMs
            throw new NotImplementedException();
        }
        
        public SimStatusType Status { get; private set; }

        private Sim() { }

        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public void Activate() {
            // Enable SuperSIM
            throw new NotImplementedException();
        }


        public void Deactivate()
        {
            // Deactivate SuperSIM
            throw new NotImplementedException();
        }

        
    }
}