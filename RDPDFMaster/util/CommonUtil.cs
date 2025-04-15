using RDDLib.comm;
using RDDLib.pdf;
using System;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Windows.UI.Xaml.Media.Imaging;

namespace RDPDFMaster.util
{
    public class CommonUtil
    {
        public static PDFDocForm createImageForm(PDFDoc doc, WriteableBitmap bitmap, float width, float height)
        {
            PDFDocForm form = doc.NewForm();
            if (form != null)
            {
                PDFPageContent content = new PDFPageContent();
                //content.Create();
                content.GSSave();

                content.GSSave();
                float originalWidth = bitmap.PixelWidth;//.getWidth();
                float originalHeight = bitmap.PixelHeight;//.getHeight();
                float scale = height / originalHeight;
                float scaleW = width / originalWidth;
                if (scaleW < scale) scale = scaleW;

                float xTranslation = (width - originalWidth * scale) / 2.0f, yTranslation = (height - originalHeight * scale) / 2.0f;

                PDFDocImage dimg = doc.NewImage(bitmap, true, true);
                PDFResImage rimg = form.AddResImage(dimg);
                RDMatrix mat = new RDMatrix(scale * originalWidth, scale * originalHeight, xTranslation, yTranslation);
                content.GSSetMatrix(mat);
                //mat.Destroy();
                content.DrawImage(rimg);
                content.GSRestore();
                content.GSRestore();

                form.SetContent(content, 0, 0, width, height);
                //content.Destroy();
            }
            return form;
        }

        public static void ShowToastNotification(string title, string stringContent, double toastTimeout)
        {
            try
            {
                ToastNotifier ToastNotifier = ToastNotificationManager.CreateToastNotifier();
                XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
                XmlNodeList toastNodeList = toastXml.GetElementsByTagName("text");
                if (title != null && title.Length > 0)
                    toastNodeList.Item(0).AppendChild(toastXml.CreateTextNode(title));
                toastNodeList.Item(1).AppendChild(toastXml.CreateTextNode(stringContent));
                IXmlNode toastNode = toastXml.SelectSingleNode("/toast");
                XmlElement audio = toastXml.CreateElement("audio");
                audio.SetAttribute("src", "ms-winsoundevent:Notification.SMS");

                ToastNotification toast = new ToastNotification(toastXml)
                {
                    ExpirationTime = DateTime.Now.AddSeconds(toastTimeout)
                };
                ToastNotifier.Show(toast);
            }
            catch (Exception) { }
        }
    }
}
