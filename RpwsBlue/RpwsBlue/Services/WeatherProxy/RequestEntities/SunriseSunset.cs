using System;
using System.Collections.Generic;
using System.Text;

namespace RpwsBlue.Services.WeatherProxy.RequestEntities
{
    class SunriseSunset
    {
        public SunriseSunsetResult results;
    }

    class SunriseSunsetResult
    {
        public string sunrise;
        public string sunset;
    }
}
