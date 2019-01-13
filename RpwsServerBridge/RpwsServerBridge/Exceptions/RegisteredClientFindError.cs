using System;
using System.Collections.Generic;
using System.Text;

namespace RpwsServerBridge.Exceptions
{
    /// <summary>
    /// Sent when a registered client capable of getting a packet could not be found or reached.
    /// </summary>
    public class RegisteredClientFindError : Exception
    {
    }
}
