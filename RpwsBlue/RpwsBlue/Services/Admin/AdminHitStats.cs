using LibRpws;
using LibRpwsDatabase.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RpwsBlue.Services.Admin
{
    class AdminHitStats
    {
        private static string GetService(int serviceId)
        {
            var ser = Program.services.Where(x => x.id == (RpwsServiceId)serviceId).ToArray();
            if(ser.Length == 1)
                return ser[0].pathname;
            return null;
        }

        public static string OnReq(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee)
        {
            string[] color_ids = new string[] { "#f44242", "#ed9e3d", "#eaea3f", "#74e83e", "#40e5d4", "#3c93e0", "#4735e8", "#d035e8", "#e8358b" };
            string[] exec_color_ids = new string[] { "#2e22d6", "#2276d6", "#22d66c", "#48d622", "#b2d622", "#d6c122", "#4735e8", "#e0aa23", "#e2671f", "#e22b1e" };
            

            //Produce a graph. First, fetch graph requirements.
            int steps = 60;
            HitStatsColors colorCodesType = HitStatsColors.ServiceId;
            long step_length_ticks = 9000000000; // https://www.venea.net/web/net_ticks_timespan_converter#time_span_to_net_ticks_conversion
            if (e.Request.Query.ContainsKey("steps"))
                steps = int.Parse(e.Request.Query["steps"]);
            if (e.Request.Query.ContainsKey("step_length_ticks"))
                step_length_ticks = long.Parse(e.Request.Query["step_length_ticks"]);
            if (e.Request.Query.ContainsKey("color_type"))
                colorCodesType = Enum.Parse<HitStatsColors>(e.Request.Query["color_type"]);
            //Fetch the data from the database.
            DateTime minTime = DateTime.UtcNow - new TimeSpan(step_length_ticks * steps);
            long minTimeTicks = minTime.Ticks;
            var stats = LibRpwsCore.lite_database.GetCollection<E_RPWS_AnalyticsObject>("analytics").Find(x => x.time > minTimeTicks).ToArray();
            //Split by step.
            List<E_RPWS_AnalyticsObject>[] stats_sorted = new List<E_RPWS_AnalyticsObject>[steps];
            int min = 0;
            int max = 0;
            Dictionary<int, int> service_color = new Dictionary<int, int>();
            int current_service_color = 0;
            foreach(var s in stats)
            {
                //Get the index.
                TimeSpan time = DateTime.UtcNow - new DateTime(s.time);
                int index = (int)(time.Ticks / step_length_ticks);
                //Console.WriteLine(index);
                if (stats_sorted[index] == null)
                    stats_sorted[index] = new List<E_RPWS_AnalyticsObject>();
                //Add this.
                stats_sorted[index].Add(s);
                //Do min/max calc
                min = Math.Min(min, stats_sorted[index].Count);
                max = Math.Max(max, stats_sorted[index].Count);
                //Calculate the service color.
                switch(colorCodesType)
                {
                    case HitStatsColors.ServiceId:
                        int service = s.serviceId;
                        if (!service_color.ContainsKey(service))
                        {
                            service_color.Add(service, current_service_color % color_ids.Length);
                            current_service_color++;
                        }
                        break;
                }
                
            }
            //Now, start generating the HTML graph.
            float step_length_precent = 100f / steps;
            string outer_graph_html = "";
            string main_graph_html = "";
            string graph_html = "";
            foreach(var l in stats_sorted)
            {
                //Calculate the size of this.
                int count = min;
                if (l != null)
                    count = l.Count;
                int size = count - min;
                float graph_size = ((float)size / (max - min)) * 100;
                //Create the element
                graph_html += "<div style=\"width:" + step_length_precent.ToString() + "%; height:" + graph_size.ToString() + "%; display:inline-block;\">";
                //Add the inners. This is run for each action. We sort this first.
                List<E_RPWS_AnalyticsObject> ls = new List<E_RPWS_AnalyticsObject>();
                if(l != null)
                    ls = l.OrderBy(x => x.serviceId).ToList();
                float eleSize = graph_size / count;
                for(int i = 0; i<count; i++)
                {
                    var action = ls[i];
                    string color = "";
                    //Decide on the color.
                    switch(colorCodesType)
                    {
                        case HitStatsColors.ServiceId:
                            //Based on the path.
                            color = color_ids[service_color[action.serviceId]];
                            break;
                        case HitStatsColors.None:
                            //Always gray
                            color = "gray";
                            break;
                        case HitStatsColors.ExecutionTime:
                            //Use response time
                            int execTime = (int)new TimeSpan(action.timeTicks).TotalMilliseconds / 100;
                            color = exec_color_ids[Math.Min(exec_color_ids.Length - 1, execTime)];
                            break;
                    }
                    //Now, add this.
                    graph_html += "<div style=\"height:" + eleSize.ToString() + "%; background-color:" + color + "; display:inline-block; width:100%;\"></div>";
                }
                graph_html += "</div>";
            }
            //Now that we have the bars done, add it to the graph html. Then, add the background lines.
            main_graph_html += "<div style=\"position:absolute; top:0; bottom:0; left:0; right:0; z-index:10;\">" + graph_html + "</div>";
            string graph_bg_html = "";
            float step_length_precent_y = ((float)1 / (max - min)) * 100;
            for (int x = 0; x<steps; x++)
            {
                //Start section
                graph_bg_html += "<div style=\"width:" + step_length_precent.ToString() + "%; display:inline-block;\">";
                for(int y = 0; y<(max - min); y++)
                {
                    graph_bg_html += "<div style=\"width:calc(100% - 1px); height:calc(" + step_length_precent_y.ToString() + "% - 1px); border-bottom:1px solid #e1e1e1; border-left:1px solid #e1e1e1; display:inline-block;\"></div>";
                }
                //End section
                graph_bg_html += "</div>";
            }
            //Add behind the actual graph data.
            main_graph_html += "<div style=\"position:absolute; top:0; bottom:0; left:0; right:0; z-index:9;\">" + graph_bg_html + "</div>";
            //Add timestamps
            int timestamp_count = 4;
            for(int i = 0; i<timestamp_count; i++)
            {
                DateTime time = DateTime.UtcNow - new TimeSpan(step_length_ticks * i);
                outer_graph_html += "<div style=\"width:calc(" + (100f / timestamp_count).ToString() + "% - 1px); text-align:center; display:inline-block;\">" + time.ToShortDateString() + " " + time.ToShortTimeString() + "</div>";
            }
            //Add key
            outer_graph_html += "<hr><div>";
            foreach(var k in service_color)
            {
                outer_graph_html += "<div style=\"display:inline-block; line-height:20px; padding:2px;\"><div style=\"height:20px; width:20px; display:inline-block; background-color:" + color_ids[k.Value].ToString() + ";\"></div> " + GetService(k.Key) + "</div>";
            }
            outer_graph_html += "</div>";
            //Add options
            outer_graph_html += "<hr><form method=\"get\" action=\"/admin\"><input type=\"hidden\" name=\"service\" value=\"stats\"><select name=\"step_length_ticks\" value=\""+step_length_ticks.ToString()+"\">";
            outer_graph_html += "<option value=\"300000000\">Unit Size: 30 seconds</options>";
            outer_graph_html += "<option value=\"1200000000\">Unit Size: 120 seconds</options>";
            outer_graph_html += "<option value=\"9000000000\">Unit Size: 15 minutes</options>";
            outer_graph_html += "<option value=\"18000000000\">Unit Size: 30 minutes</options>";
            outer_graph_html += "<option value=\"36000000000\">Unit Size: 1 hour</options>";
            outer_graph_html += "<option value=\"216000000000\">Unit Size: 6 hours</options>";
            outer_graph_html += "<option value=\"432000000000\">Unit Size: 12 hours</options>";
            outer_graph_html += "<option value=\"864000000000\">Unit Size: 1 day</options>";
            outer_graph_html += "<option value=\"1728000000000\">Unit Size: 2 days</options>";
            outer_graph_html += "<option value=\"8640000000000\">Unit Size: 10 days</options>";
            outer_graph_html += "</select><br><select name=\"steps\" value=\""+steps.ToString()+"\">";
            outer_graph_html += "<option value=\"30\">Count: 30</option>";
            outer_graph_html += "<option value=\"60\">Count: 60</option>";
            outer_graph_html += "<option value=\"120\">Count: 120 (slow)</option>";
            outer_graph_html += "</select><br><select name=\"color_type\" value=\"" + colorCodesType.ToString() + "\">";
            outer_graph_html += "<option value=\"ServiceId\">Service</options>";
            outer_graph_html += "<option value=\"ExecutionTime\">Execution Time</options>";
            outer_graph_html += "<option value=\"None\">None</options>";
            outer_graph_html += "</select><br><input type=\"submit\" value=\"Refresh\"></form>";

            return "<div style=\"width:100%; height:500px; position:relative;\">" + main_graph_html + "</div>"+outer_graph_html;
        }

        enum HitStatsColors
        {
            None,
            ServiceId,
            ExecutionTime
        }
    }
}
