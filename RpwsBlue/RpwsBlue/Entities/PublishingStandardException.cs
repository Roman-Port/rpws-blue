using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace RpwsBlue
{
    public class RpwsStandardHttpException : Exception
    {
        public string http_message;
        public string http_top;
        public bool retryable;
        public int code;

        public RpwsStandardHttpException(string msg, bool retryable = false, int code = 500)
        {
            http_top = "Error";
            http_message = msg;
            this.retryable = retryable;
            this.code = code;
        }

        public RpwsStandardHttpException(string top, string message, bool retryable = false, int code = 500)
        {
            http_top = top;
            http_message = message;
            this.retryable = retryable;
            this.code = code;
        }

        public string ToJsonString()
        {
            return JsonConvert.SerializeObject(new RSHE_Output
            {
                title = http_top,
                retryable = retryable,
                message = http_message,
                SERVER_VERSION = Program.SERVER_VERISON,
                SERVER_DATE_CODE = Program.SERVER_DATE_CODE
            });
        }

        
    }

    public class RSHE_Output
    {
        public string message;
        public bool retryable;
        public string title;
        public string SERVER_VERSION;
        public string SERVER_DATE_CODE;
    }
}
