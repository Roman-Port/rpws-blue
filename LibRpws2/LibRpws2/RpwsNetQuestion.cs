using System;
using System.Collections.Generic;
using System.Text;

namespace LibRpws2
{
    //Add 20.
    public enum RpwsNetQuestion
    {
        Shell = 1,
        ValidateAccessToken = 2,

        GetAppById = 3,
        GetAppByUUID = 4,
        GetAppsByIdIndexed = 5,
        GetAppsByUUIDIndexed = 6
    }
}
