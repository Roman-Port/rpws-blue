using LibRpws;
using LibRpws2.ApiEntities;
using Newtonsoft.Json;
using RpwsBlue.Services.WeatherProxy.ReplyEntities;
using RpwsBlue.Services.WeatherProxy.RequestEntities;
using System;
using System.Collections.Generic;
using System.Text;

namespace RpwsBlue.Services.WeatherProxy
{
    class WeatherProxyCached
    {
        public WeatherRequest weather;
    }

    public static class WeatherProxy
    {
        private const string END_CREDIT = "\r\n\r\nPowered by DarkSky";

        private static CacheBlock<WeatherProxyCached> cache = new CacheBlock<WeatherProxyCached>(1000, (long)TimeSpan.FromMinutes(30).TotalSeconds);

        public static void OnClientRequest(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee)
        {
            //First, extract the request data.
            float lat = float.Parse(ee.GET["lat"]);
            float lon = float.Parse(ee.GET["lon"]);

            //Round lat/lon to help cache
            lat = RoundCoord(lat);
            lon = RoundCoord(lon);

            //Units
            bool srcF = true;
            bool destF = true;
            char unit = 'F';
            if(ee.GET["units"] != "e")
            {
                destF = false;
                unit = 'C';
            }
            //Get the data.
            string cache_key = $"{lat}/{lon}";
            if (!cache.TryGetItem(cache_key, out WeatherProxyCached weatherData))
            {
                //We do not have the weather data. Get it.
                weatherData = new WeatherProxyCached
                {
                    weather = LibRpwsCore.GetObjectHttp<WeatherRequest>($"https://api.darksky.net/forecast/{Program.config.secure_creds["darksky"]["apiKey"]}/{lat},{lon}")
                };
                //Add to cache
                cache.AddItem(weatherData, cache_key);
            }
            //Now, request the object from Darksky.
            WeatherRequest req = weatherData.weather;
            //Start creating output.
            RE_CurrentConditions currentConditions = new RE_CurrentConditions();
            //Create the current conditions.
            currentConditions.expire_time_gmt = (int)req.currently.time + (60 * 60); //I don't know. Set this to an hour ahead.
            currentConditions.icon_code = GetIconCode(req.currently.icon);
            currentConditions.imperial = new RE_TemperatureHolder(ConvertUnitsIfNeeded(req.currently.temperature, srcF, destF));
            currentConditions.metric = currentConditions.imperial;
            currentConditions.uk_hybrid = currentConditions.metric;
            currentConditions.phrase_12char = req.currently.summary;
            //Now, convert each day.
            List<RE_DailyForecast> forecasts = new List<RE_DailyForecast>();
            for(int d = 0; d<req.daily.data.Count; d++)
            {
                var data = req.daily.data[d];
                RE_DailyForecast forecast = new RE_DailyForecast();
                DateTime dayGmt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified).AddSeconds(data.time).AddHours(req.offset);
                DateTime dayLocal = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(data.time);
                forecast.dow = dayLocal.DayOfWeek.ToString();
                forecast.expire_time_gmt = (long)(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc) - dayGmt.AddHours(24)).TotalSeconds;
                forecast.fcst_valid_local = dayLocal;
                forecast.max_temp = ConvertUnitsIfNeeded(data.temperatureMax, srcF, destF).ToString();
                forecast.min_temp = ConvertUnitsIfNeeded(data.temperatureMin, srcF, destF).ToString();
                forecast.narrative = data.summary.TrimEnd('.') + ". High of " + forecast.max_temp.ToString() + "°" + unit + ", and a low of "+forecast.min_temp.ToString()+ "°"+unit+". " + CreateWindString(data)+ END_CREDIT;
                
                //Now, generate the times of day.
                RE_DayPartForecast day = new RE_DayPartForecast();
                day.daypart_name = dayLocal.DayOfWeek.ToString();
                day.day_ind = "D";
                day.fcst_valid_local = dayGmt;
                day.icon_code = GetIconCode(data.icon);
                day.narrative = data.summary.TrimEnd('.')+". High of "+ forecast.max_temp.ToString()+ "°"+unit+". "+ CreateWindString(data) + END_CREDIT;
                day.phrase_22char = CapitalizeString(data.icon).Replace(" day","");
                day.phrase_32char = day.phrase_22char;
                day.shortcast = data.summary;
                day.temp = int.Parse(forecast.max_temp);
                day.temp_phrase = forecast.max_temp + "°" + unit;
                forecast.day = day;
                //Generate nighttime
                RE_DayPartForecast night = new RE_DayPartForecast();
                night.daypart_name = dayLocal.DayOfWeek.ToString()+" night";
                night.day_ind = "N";
                night.fcst_valid_local = dayGmt.AddHours(12);
                night.icon_code = GetIconCode(data.icon);
                night.narrative = data.summary.TrimEnd('.') + ". Low of " + forecast.min_temp.ToString() + "°" + unit + ". " + CreateWindString(data) + END_CREDIT;
                night.phrase_22char = CapitalizeString(data.icon).Replace(" day", "");
                night.phrase_32char = night.phrase_22char;
                night.shortcast = data.summary;
                night.temp = int.Parse(forecast.min_temp);
                night.temp_phrase = forecast.min_temp + "°" + unit;
                forecast.night = night;
                //Get the sunset and sunrise times.
                string ssurl = $"https://api.sunrise-sunset.org/json?lat={lat}&lng={lon}&date=-{dayGmt.Year.ToString()}-{IntToPaddedString(dayGmt.Month)}-{IntToPaddedString(dayGmt.Day)}&formatted=0";
                SunriseSunset ss = LibRpwsCore.GetObjectHttp<SunriseSunset>(ssurl);
                forecast.sunrise = ss.results.sunrise.Trim('-');
                forecast.sunset = ss.results.sunset.Trim('-');
                //Add to the forecasts now.
                forecasts.Add(forecast);
            }
            //Generate final output.
            RE_AggregateReport report = new RE_AggregateReport();
            report.conditions = new conditions();
            report.conditions.data = new conditions_data();
            report.conditions.data.observation = currentConditions;
            report.fcstdaily7 = new fcstdaily7();
            report.fcstdaily7.data = new fcstdaily7_data();
            report.fcstdaily7.data.forecasts = forecasts.ToArray();
            Program.QuickWriteToDoc(e, JsonConvert.SerializeObject(report), "application/json");
        }

        private static float RoundCoord(float input)
        {
            float s = MathF.Round(input * 2);
            s /= 2;
            return s;
        }

        private static string CreateWindString(DataDaily dd)
        {
            int windDir = (int)dd.windBearing;
            int windSpeed = (int)dd.windSpeed;
            WindDirection dir = (WindDirection)((windDir / 90) % 4);
            string data = "Winds " + dir.ToString() + " at " + windSpeed + " MPH. ";
            //If gusts are over 40 MPH, note it.
            if (dd.windGust > 40)
                data += "Wind gusts up to " + ((int)dd.windGust).ToString() + " MPH. ";
            return data;
        }

        private static string IntToPaddedString(int i)
        {
            string o = i.ToString();
            if (o.Length == 1)
                o = "0" + o;
            return o;
        }

        private static string CapitalizeString(string s)
        {
            s = s.Replace('-', ' ');
            char[] cs = s.ToCharArray();
            cs[0] = cs[0].ToString().ToUpper()[0];
            return new string(cs);
        }

        private static int ConvertUnitsIfNeeded(double input, bool sourceF, bool destF)
        {
            if (sourceF && destF)
                return (int)input;
            else if (!sourceF && !destF)
                return (int)input;
            else if (sourceF && !destF)
                return (int)((input - 32) * 0.5556f); //Convert from F to C
            else
                return (int)(input * 1.8f) + 32; //Convert C to F
        }

        private static int GetIconCode(string icon)
        {
            //Get enum
            WeatherIcon ico = Enum.Parse<WeatherIcon>(icon.Replace('-','_'));
            return (int)ico;
        }

        enum WeatherIcon
        {
            clear_day = 31,
            clear_night = 32,
            rain = 11,
            snow = 41,
            sleet = 35,
            wind = 26,
            fog = 27,
            cloudy = 28,
            partly_cloudy_day = 29,
            partly_cloudy_night = 30,
            hail = 0,
            thunderstorm = 1,
            tornado = 2
        }

        enum WindDirection
        {
            North,
            East,
            South,
            West
        }
    }
}
