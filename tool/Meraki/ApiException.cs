using System;

namespace Lup.TwilioSwitch.Meraki
{
    public class ApiException : Exception
    {
        public ApiException()
        {
        }

        public ApiException(string message)
            : base(message)
        {
        }
        
        public ApiException(string message, Exception exception)
            : base(message, exception)
        {
        }
    }
}