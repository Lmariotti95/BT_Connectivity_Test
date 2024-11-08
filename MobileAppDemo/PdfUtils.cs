using System;
using iTextSharp;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Windows.Forms;
using System.IO;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace MobileAppDemo
{
    public static class PdfUtils
    {
        public static void ExportCsvList(List<List<string>> lines, string fileName)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (Document document = new Document())
                {
                    PdfWriter writer = PdfWriter.GetInstance(document, fs);
                    document.Open();

                    int maxLen = lines.Max(row => row.Count);

                    PdfPTable table = new PdfPTable(maxLen);

                    float[] widths = new float[maxLen];
                    for (int i = 0; i < maxLen; i++)
                        widths[i] = 4f; // Adjust this as needed

                    table.SetWidths(widths);

                    table.WidthPercentage = 100;

                    // Load fonts
                    BaseFont baseFontChinese = BaseFont.CreateFont(CommonPaths.fontPathChinese, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                    BaseFont baseFontCyrillicLatin = BaseFont.CreateFont(CommonPaths.fontPathCyrillicLatin, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);

                    Font fontChinese = new Font(baseFontChinese, 6);
                    Font fontCyrillicLatin = new Font(baseFontCyrillicLatin, 6);

                    for (int rowIndex = 0; rowIndex < lines.Count; rowIndex++)
                    {
                        BaseColor bgColor = (rowIndex % 2 == 0) ? BaseColor.White : new BaseColor(240, 240, 240); // Light gray for odd rows

                        // Add each subitem (column) from the ListViewItem
                        for (int i = 0; i < lines[rowIndex].Count; i++)
                        {
                            string cellText = lines[rowIndex][i].Trim();

                            // Choose font based on characters in the text
                            Font selectedFont = ContainsChineseCharacters(cellText) ? fontChinese : fontCyrillicLatin;

                            PdfPCell cell = new PdfPCell(new Phrase(cellText, selectedFont))
                            {
                                MinimumHeight = 20f,       // Set higher row height for data cells
                                BackgroundColor = bgColor // Set alternating background color
                            };

                            table.AddCell(cell);
                        }
                    }

                    document.Add(table);
                    document.Close();
                }
            }
        }
        private static bool ContainsChineseCharacters(string text)
        {
            // Basic check for Chinese character ranges
            return text.Any(c => (c >= 0x4E00 && c <= 0x9FFF) || (c >= 0x3400 && c <= 0x4DBF) || (c >= 0x20000 && c <= 0x2A6DF));
        }

        public static void ExportListView(ListView listView, string fileName)
        {
            List<List<string>> lines = new List<List<string>>();
            foreach(ListViewItem item in listView.Items)
            {
                List<string> fields = new List<string>
                {
                    item.Text
                };

                foreach (ListViewItem subItem in item.SubItems)
                {
                    fields.Add(subItem.Text);
                }

                lines.Add(fields);
            }

            ExportCsvList(lines, fileName);
        }

        public static void ExportRawLines(List<string> lines, string fileName)
        {
            List<List<string>> newLines = new List<List<string>>();

            foreach (string line in lines)
            {
                var fields = line.Trim('\n').Trim('\r').Split(';').ToList();

                while (fields.Count < 3)
                    fields.Add("");

                newLines.Add(fields);
            }

            ExportCsvList(newLines, fileName);
        }

        public static void ExportRawLines(string fileName, List<string> lines)
        {
            ExportRawLines(fileName, lines, false);
        }

        public static void ExportRawLines(string fileName, List<string> lines, bool openFile)
        {
            ExportRawLines(lines, fileName);

            if(openFile)
                Process.Start(fileName);
        }
    }
}
