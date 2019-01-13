using System;
using System.Collections.Generic;
using System.Text;

namespace RpwsServerBridge.Entities
{
    public enum RequestStatusCode
    {
        Ok = 200,
        NotFound = 400,
        ServerError = 500
    }
}
