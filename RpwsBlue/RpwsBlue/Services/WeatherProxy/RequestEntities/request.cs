using System;
using System.Collections.Generic;
using System.Text;

namespace RpwsBlue.Services.WeatherProxy.RequestEntities
{
    public class Currently
    {
        public float time { get; set; }
        public string summary { get; set; }
        public string icon { get; set; }
        public float nearestStormDistance { get; set; }
        public float nearestStormBearing { get; set; }
        public float precipIntensity { get; set; }
        public float precipProbability { get; set; }
        public double temperature { get; set; }
        public double apparentTemperature { get; set; }
        public double dewPoint { get; set; }
        public double humidity { get; set; }
        public double pressure { get; set; }
        public double windSpeed { get; set; }
        public double windGust { get; set; }
        public float windBearing { get; set; }
        public double cloudCover { get; set; }
        public float uvIndex { get; set; }
        public float visibility { get; set; }
        public double ozone { get; set; }
    }

    public class DataDaily
    {
        public float time { get; set; }
        public string summary { get; set; }
        public string icon { get; set; }
        public float sunriseTime { get; set; }
        public float sunsetTime { get; set; }
        public double moonPhase { get; set; }
        public double precipIntensity { get; set; }
        public double precipIntensityMax { get; set; }
        public float precipIntensityMaxTime { get; set; }
        public double precipProbability { get; set; }
        public string precipType { get; set; }
        public double temperatureHigh { get; set; }
        public float temperatureHighTime { get; set; }
        public double temperatureLow { get; set; }
        public float temperatureLowTime { get; set; }
        public double apparentTemperatureHigh { get; set; }
        public float apparentTemperatureHighTime { get; set; }
        public double apparentTemperatureLow { get; set; }
        public float apparentTemperatureLowTime { get; set; }
        public double dewPoint { get; set; }
        public double humidity { get; set; }
        public double pressure { get; set; }
        public double windSpeed { get; set; }
        public double windGust { get; set; }
        public float windGustTime { get; set; }
        public float windBearing { get; set; }
        public double cloudCover { get; set; }
        public float uvIndex { get; set; }
        public float uvIndexTime { get; set; }
        public float visibility { get; set; }
        public double ozone { get; set; }
        public double temperatureMin { get; set; }
        public float temperatureMinTime { get; set; }
        public double temperatureMax { get; set; }
        public float temperatureMaxTime { get; set; }
        public double apparentTemperatureMin { get; set; }
        public float apparentTemperatureMinTime { get; set; }
        public double apparentTemperatureMax { get; set; }
        public float apparentTemperatureMaxTime { get; set; }
    }

    public class Daily
    {
        public string summary { get; set; }
        public string icon { get; set; }
        public List<DataDaily> data { get; set; }
    }

    public class WeatherRequest
    {
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string timezone { get; set; }
        public Currently currently { get; set; }
        public Daily daily { get; set; }
        public float offset { get; set; }
    }
}
