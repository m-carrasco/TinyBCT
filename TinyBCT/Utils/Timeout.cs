using System;
using System.Threading;

namespace TinyBCT.Utils
{
    public class Timeout
    {
        public Timeout()
        {
        }

        public static void InitiateTimeoutCheck()
        {
            Thread t = new Thread(() => CheckTimeout(Settings.TimeoutMinutes));
            t.IsBackground = true;
            t.Start();
        }
        private static void CheckTimeout(int timeInMinutes)
        {
            Thread.Sleep(timeInMinutes * 60 * 1000);
            throw new TimeoutException();
        }
    }
}
