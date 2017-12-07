
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;

public class Print 
 {
     private Font printTitleFont;
     private Font printFont;
     private Font printXLFont;
     private Font barcodeFont;
//     private StreamReader streamToPrint;
//     static string filePath;

//     Image s_imgBarcode;
//     Image s_imgQRcode;
     Image s_imgLogo;
     String sTitle;

     public Print() 
     {
         Printing();
     }

     public Print(string s,/* Image bc, Image QR,*/ Image lg)
     {
         sTitle = s;
//         s_imgBarcode = bc;
//         s_imgQRcode = QR;
         s_imgLogo = lg;
         Printing();
     }
     // The PrintPage event is raised for each page to be printed.

     private void pd_PrintPage(object sender, PrintPageEventArgs ev) 
     {
         string s1 = "MANUFACTURER:\nPHYTECH LTD\n\nFCCID: 2ALN6200";
         string s2 = "This device complies with Part 15 of the FCC Rules.\nOperation is subject to the following two conditions: \n1. This device may not cause harmful interference, and \n2. this device must accept any interference received,\n including interference that may cause undesired operation";
         string s3 = "S/N: " +sTitle;
         string s4 = '*' + sTitle + '*';

         int leftSticker = 60;
         int rightSticker = 210;

         ev.Graphics.DrawString(s1, printTitleFont, Brushes.Black, leftSticker, 250, new StringFormat());
         ev.Graphics.DrawString(s1, printTitleFont, Brushes.Black, rightSticker, 250, new StringFormat());
         //ev.Graphics.DrawString(s, printFont, Brushes.Black, 60, 150, new StringFormat());
         ev.Graphics.DrawString(s2, printFont, Brushes.Black, leftSticker, 300, new StringFormat());

         ev.Graphics.DrawString(s3, printTitleFont, Brushes.Black, leftSticker, 340, new StringFormat());
         ev.Graphics.DrawString(s3, printXLFont, Brushes.Black, rightSticker, 320, new StringFormat());
         //ev.Graphics.DrawImage(s_imgQRcode, 130, 330);
         ev.Graphics.DrawString(s4, barcodeFont, Brushes.Black, leftSticker, 355, new StringFormat());
         ev.Graphics.DrawString(s4, barcodeFont, Brushes.Black, rightSticker, 355, new StringFormat());

         ev.Graphics.DrawImage(s_imgLogo, 150, 240, s_imgLogo.Width, s_imgLogo.Height);
         ev.Graphics.DrawImage(s_imgLogo, 300, 240, s_imgLogo.Width, s_imgLogo.Height);
         ev.Graphics.DrawImage(XP_Monitor.Properties.Resources.rcmmark, 155, 330, 20, 20);
         ev.Graphics.DrawImage(XP_Monitor.Properties.Resources.rcmmark, 310, 290, 20, 20);

         /// Second time
//          ev.Graphics.DrawString(s1, printTitleFont, Brushes.Black, 450, 250, new StringFormat());
     }

     // Print the file.

     public void Printing()
     {
         try
         {
             //PrintDialog printDialog1 = new PrintDialog();
             //printDialog1.PrinterSettings.PrinterName = "Godex G530";
             //DialogResult result = printDialog1.ShowDialog();
             //if (result != DialogResult.OK)
             //    return;

            //try 
            //{
                float fSize = 3.5f;
                printFont = new Font("Antique Olive", fSize, FontStyle.Bold);
                printTitleFont = new Font("Antique Olive", 7, FontStyle.Bold);
                printXLFont = new Font("Antique Olive", 12, FontStyle.Bold);
                barcodeFont = new Font("Free 3 of 9", 28, System.Drawing.FontStyle.Regular);
                    //System.Drawing.GraphicsUnit.Point);
//                printFont = new Font("Arial", 10, FontStyle.Bold);
               PrintDocument pd = new PrintDocument(); 
               pd.PrintPage += new PrintPageEventHandler(pd_PrintPage);
               // Specify the printer to use.

               pd.PrinterSettings.PrinterName = "Godex G530";//printDialog1.PrinterSettings.PrinterName; //(String) 
               // Print the document.
               pd.Print();
            //} 
            //finally 
            //{
            //   //streamToPrint.Close() ;
            //}
        } 
        catch(Exception ex) 
        {
            MessageBox.Show(ex.Message);
        }

     }

 }