using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MobileAppDemo
{
    static class Utils
    {
        public static void CloseThread(Thread t)
        {
            if (t != null)
                if (t.IsAlive)
                    t.Abort();
        }
    }
}
