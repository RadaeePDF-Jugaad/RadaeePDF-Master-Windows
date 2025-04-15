using RDDLib.pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“内容对话框”项模板

namespace RDPDFMaster.PDFReaderPage
{
    public sealed partial class DlgSignatureProp : ContentDialog
    {
        public DlgSignatureProp(PDFSign sign, PDFDoc doc)
        {
            this.InitializeComponent();
            mIssueText.Text = sign.Issue;
            mSubjectText.Text = sign.Subject;
            mVersionText.Text = sign.Version.ToString();
            mReasonText.Text = sign.Reason;
            mLocationText.Text = sign.Location;
            mContackText.Text = sign.Contact;
            mSignTimeText.Text = sign.ModTime;
            int iret = doc.VerifySign(sign);
            if (iret == 0)
                mVerifyText.Text = "OK";
            else
                mVerifyText.Text = "Changed";
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }
    }
}
