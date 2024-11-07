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

                    // Use a Unicode-compatible font
                    string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "Msjh.ttc"); // Adjust font file name as needed
                    int fontIndex = 1; // 1 = Cinese
                    string fontPathWithIndex = fontPath + "," + fontIndex;
                    BaseFont baseFont = BaseFont.CreateFont(fontPathWithIndex, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                    Font unicodeFont = new Font(baseFont, 6);

                    for (int rowIndex = 0; rowIndex < lines.Count; rowIndex++)
                    {
                        BaseColor bgColor = (rowIndex % 2 == 0) ? BaseColor.White : new BaseColor(240, 240, 240); // Light gray for odd rows

                        // Add each subitem (column) from the ListViewItem
                        for (int i = 0; i < lines[rowIndex].Count; i++)
                        {
                            PdfPCell cell = new PdfPCell(new Phrase(lines[rowIndex][i].Trim(), unicodeFont))
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
