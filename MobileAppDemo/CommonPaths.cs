using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileAppDemo
{
    public static class CommonPaths
    {
        public readonly static string ticketFolder = ".\\Tickets";

        public readonly static string fontPathChinese = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "msjh.ttc,1");         // Microsoft JhengHei for Chinese
        public readonly static string fontPathCyrillicLatin = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "L_10646.ttf");  // Lucida Sans Unicode
    }
}
