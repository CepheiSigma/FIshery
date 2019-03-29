using System;
using System.Threading;

namespace Fishery.Core
{
    public class Clock
    {
        private Timer _timer;

        public Clock()
        {
            _timer = new Timer(Tick,null,0,30000);
        }

        public void Tick(object o)
        {
            EventRouter.GetInstance().FireEvent("Clock_Tick",null,null);
        }

        public static double GetTimeStamp(bool millisecond = false)
        {
            DateTime _197011 = new DateTime(1970, 1, 1);
            DateTime dateTime =
                DateTime.Now.ToUniversalTime();
            return millisecond ? Math.Ceiling((dateTime - _197011).TotalMilliseconds) : (dateTime - _197011).TotalSeconds;
        }

        public static double GetSpecifiedTimeStamp(string dayString, bool millisecond = false)
        {
            DateTime _197011 = new DateTime(1970, 1, 1);
            DateTime dateTime =
                DateTime.Parse(dayString)
                    .ToUniversalTime();
            return millisecond ? Math.Ceiling((dateTime - _197011).TotalMilliseconds) : (dateTime - _197011).TotalSeconds;
        }
    }
}
