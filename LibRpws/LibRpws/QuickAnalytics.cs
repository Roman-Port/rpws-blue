using System;
using System.Collections.Generic;
using System.Text;

namespace LibRpws
{
    public class QuickAnalytics
    {
        public static QuickAnalytics Start(string taskName)
        {
            QuickAnalytics qa = new QuickAnalytics();
            qa.start = DateTime.UtcNow;
            qa.name = taskName;
            return qa;
        }
        private DateTime start;
        private string name;

        public void End()
        {
            LibRpwsCore.Log("Finished '" + name + "' in " + (DateTime.UtcNow - start).TotalMilliseconds.ToString() + " ms!", RpwsLogLevel.Analytics);
        }
    }
}
