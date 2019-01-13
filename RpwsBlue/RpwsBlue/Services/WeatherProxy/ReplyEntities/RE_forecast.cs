using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace RpwsBlue.Services.WeatherProxy.ReplyEntities
{
    public class RE_AggregateReport
    {
        public conditions conditions;
        public fcstdaily7 fcstdaily7;
    }

    public class fcstdaily7
    {
        public fcstdaily7_data data;
    }

    public class fcstdaily7_data
    {
        public RE_DailyForecast[] forecasts;
    }

    public class conditions
    {
        public conditions_data data;
    }

    public class conditions_data
    {
        public RE_CurrentConditions observation;
    }

    public class RE_CurrentConditions
    {
        public long expire_time_gmt;
        public int icon_code;
        public RE_TemperatureHolder imperial;
        public RE_TemperatureHolder metric;
        public string phrase_12char;
        public RE_TemperatureHolder uk_hybrid;
    }

    public class RE_TemperatureHolder
    {
        public int temp;

        public RE_TemperatureHolder()
        {

        }

        public RE_TemperatureHolder(int _temp)
        {
            temp = _temp;
        }
    }

    public class RE_DailyForecast
    {
        public RE_DayPartForecast day;
        public string dow;
        public long expire_time_gmt;
        public DateTime fcst_valid_local;
        public string max_temp;
        public string min_temp;
        public string narrative;
        public RE_DayPartForecast night;
        public string sunrise;
        public string sunset;
    }

    public class RE_DayPartForecast
    {
        public string day_ind;
        public string daypart_name;
        public DateTime fcst_valid_local;
        public int icon_code;
        public string narrative;
        public string phrase_22char;
        public string phrase_32char;
        public string shortcast;
        public int temp;
        public string temp_phrase;
    }
}
