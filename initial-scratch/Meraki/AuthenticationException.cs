using System;

namespace Lup.TwilioSwitch.Meraki
{
    public class AuthenticationException : Exception
    {
        public AuthenticationException()
        {
        }

        public AuthenticationException(string message)
            : base(message)
        {
        }
        
        public AuthenticationException(string message, Exception exception)
            : base(message, exception)
        {
        }
    }
}