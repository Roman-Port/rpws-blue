using System;
using System.Collections.Generic;
using System.Text;

namespace LibRpwsDatabase.Entities
{
    [Serializable]
    public class E_TestingObject
    {
        public string cool { get; set; }
        public long time { get; set; }
        public string timeString { get; set; }

        public int _id { get; set; }

        public E_TestingObject()
        {

        }

        public E_TestingObject(string test)
        {
            cool = test;
            time = DateTime.UtcNow.Ticks;
            timeString = DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString();
        }
    }
}
