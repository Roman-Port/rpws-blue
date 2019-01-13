using LibRpws;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace RpwsBlue.Services
{
    public static class TestingService
    {
        public static void OnRequest(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee)
        {
            Thread.Sleep(5000);
            throw new NotImplementedException();
        }
    }
}
