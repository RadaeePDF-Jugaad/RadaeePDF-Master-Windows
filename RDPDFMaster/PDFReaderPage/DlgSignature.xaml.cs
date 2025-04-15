using RDDLib.comm;
using RDDLib.pdf;
using RDPDFMaster.util;
using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“内容对话框”项模板

namespace RDPDFMaster.PDFReaderPage
{
    public sealed partial class DlgSignature : ContentDialog
    {
        private PDFDoc mDoc;
        private RDRect mRect;
        private PDFAnnot mAnnot;
        private WriteableBitmap mBmp;
        private string mCertPath;

        public DlgSignature(PDFDoc doc, RDRect rect, PDFAnnot annot)
        {
            this.InitializeComponent();
            mDoc = doc;
            mRect = rect;
            mAnnot = annot;
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (mCertPath == null || mCertPath.Length <= 0)
                return;

            string password = mPasswordInput.Password;
            if (password.Length <= 0)
                return;

            PDFDocForm form = CommonUtil.createImageForm(mDoc, mBmp, mRect.right - mRect.left, mRect.bottom - mRect.top);

            mAnnot.SignField(form, mCertPath, password, "radaee", "", "", "");

        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private async void mBrowseBtn_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker filePicker;
            filePicker = new FileOpenPicker();
            filePicker.ViewMode = PickerViewMode.List;
            filePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            filePicker.FileTypeFilter.Add(".pfx");
            filePicker.FileTypeFilter.Add(".p12");
            StorageFile file = await filePicker.PickSingleFileAsync();
            if (file == null)
                return;
            string path = file.Path;
            mPathInput.Text = path;
            string name = file.Name;
            StorageFile targetFile = await ApplicationData.Current.LocalCacheFolder.CreateFileAsync(name, CreationCollisionOption.ReplaceExisting);
            await file.CopyAndReplaceAsync(targetFile);
            mCertPath = targetFile.Path;
        }

        private async void mImagePickerBtn_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker filePicker;
            filePicker = new FileOpenPicker();
            filePicker.ViewMode = PickerViewMode.List;
            filePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            filePicker.FileTypeFilter.Add(".jgp");
            filePicker.FileTypeFilter.Add(".jepg");
            filePicker.FileTypeFilter.Add(".bmp");
            filePicker.FileTypeFilter.Add(".png");
            StorageFile file = await filePicker.PickSingleFileAsync();
            if (file == null)
                return;

            mBmp = new WriteableBitmap(200, 200);
            //Stream stream = mBmp.PixelBuffer.AsStream();
            using (var stream = await file.OpenReadAsync())
            {
                mBmp.SetSource(stream);
                mSignField.Source = mBmp;
            }
            }
    }
}
