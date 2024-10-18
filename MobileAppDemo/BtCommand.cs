using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileAppDemo
{
    public class BtCommand
    {
        public string Text {  get; set; }
        public Action Callback { get; set; }
    }
}
