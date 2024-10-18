using System;
using iTextSharp;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Windows.Forms;
using System.IO;
using System.Data;
using System.Linq;

namespace MobileAppDemo
{
    public static class PdfUtils
    {
        public static void ExportListView(ListView listView, string fileName)
        {
            using(FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (Document document = new Document())
                {
                    PdfWriter writer = PdfWriter.GetInstance(document, fs);
                    document.Open();

                    PdfPTable table = new PdfPTable(listView.Columns.Count);
                    //PdfPRow row = null;

                    float[] widths = new float[listView.Columns.Count];
                    for (int i = 0; i < listView.Columns.Count; i++)
                    {
                        widths[i] = 4f; // Adjust this as needed
                    }
                    table.SetWidths(widths);

                    table.WidthPercentage = 100;

                    for (int rowIndex = 0; rowIndex < listView.Items.Count; rowIndex++)
                    {
                        ListViewItem item = listView.Items[rowIndex];
                        BaseColor bgColor = (rowIndex % 2 == 0) ? BaseColor.White : new BaseColor(240, 240, 240); // Light gray for odd rows

                        Font font5 = FontFactory.GetFont(FontFactory.HELVETICA, 6);

                        // Add each subitem (column) from the ListViewItem
                        for (int i = 0; i < item.SubItems.Count; i++)
                        {

                            PdfPCell cell = new PdfPCell(new Phrase(item.SubItems[i].Text.Trim(), font5))
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
    }
}
