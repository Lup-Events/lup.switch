using System;
using System.Collections;
using Twilio.Rest.Supersim.V1;

namespace Lup.Switch
{
    public class Device
    {
        
        public static Device GetBySerial(String serial)
        {
            throw new NotImplementedException();
        }
        
        public static IList<Device> GetAll()
        {
            throw new NotImplementedException();
        }
        
        public String Serial { get; private set; }
        public String Imei { get; private set; }
        public String Iccid { get; private set; }
        
        private Device(){ }

        public Sim GetSim()
        {
            // TODO: Iccid blank
            return Sim.GetByIccid(Iccid);
        }

        public void InstallSim(Sim service)
        {
            // Reject if not setup
            // Label SuperSIM for device
            // Add APN via Meraki
            throw new NotImplementedException();
        }


        public void UninstallCellular()
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Check for issues that may prevent cellular internet working.
        /// </summary>
        /// <returns>List of issues found. Empty means no issues detected.</returns>
        /// <exception cref="NotImplementedException"></exception>
        public CellularIssueType[] DiagnoseCellularIssues()
        {
            throw new NotImplementedException();
        }

    }
}