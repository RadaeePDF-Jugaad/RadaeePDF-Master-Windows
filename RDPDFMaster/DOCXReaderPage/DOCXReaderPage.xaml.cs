using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using RDDLib.docx;
using RDDLib.docxv;
using Windows.Storage.Streams;
using com.radaee.master;
using RDDLib.reader;
using RDDLib.comm;
using Windows.UI.Core;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI;
using RDPDFReader.annotui;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
using Windows.Storage;
using RDDLib.pdf;
using Windows.UI.Xaml.Controls.Primitives;
using RDPDFMaster;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace RDDOCXReader
{
    public delegate void OnDOCXPageCloseHandler(int vmode, DOCXPos pos);
    public sealed class DOCXReaderPara
    {
        public DOCXReaderPara(IRandomAccessStream stream, DOCXDoc doc, OnDOCXPageCloseHandler close)
        {
            m_doc = doc;
            m_stream = stream;
            m_close = close;
            m_vmode = 0;
            m_pos.pageno = -1;
            m_pos.x = 0;
            m_pos.y = 0;
        }
        ~DOCXReaderPara()
        {
            release();
        }
        public void release()
        {
            if (m_doc != null)
            {
                m_doc.Close();
                m_doc = null;
            }
            if (m_stream != null)
            {
                m_stream.Dispose();
                m_stream = null;
            }
            m_close = null;
        }
        public IRandomAccessStream m_stream;
        public DOCXDoc m_doc;
        public OnDOCXPageCloseHandler m_close;
        public int m_vmode;
        public DOCXPos m_pos;
    }
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class DOCXReaderPage : Page, IDOCXViewListener, IThumbListener
    {
        private DOCXReaderPara m_docx;
        private DOCXReader m_reader;
        private DOCXThumb m_thumb;
        private string mFileName;
        public DOCXReaderPage()
        {
            this.InitializeComponent();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            NavigationMode mode = e.NavigationMode;
            object[] args = (object[])e.Parameter;
            m_docx = (DOCXReaderPara)args[0];
            mFileName = (string)args[1];
            m_reader = new DOCXReader();
            //m_reader.PDFOpen(mView, m_pdf.m_doc, PDF_LAYOUT_MODE.layout_hdual, this);

            if (m_docx.m_vmode == 0)
                m_reader.DOCXOpen(mView, m_docx.m_doc, RDLAYOUT_MODE.layout_vert, this);
            else
                m_reader.DOCXOpen(mView, m_docx.m_doc, (RDLAYOUT_MODE)m_docx.m_vmode, this);
            m_thumb = new DOCXThumb();
            m_thumb.DOCXOpen(mThumb, m_docx.m_doc, this);

            if (m_docx.m_pos.pageno >= 0)
                m_reader.DOCXGotoPage(m_docx.m_pos.pageno);
            Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => { m_thumb.DOCXSetSelPage(m_docx.m_pos.pageno); });
            enter_normal();
        }
        private void enter_normal()
        {
            mNormalCommandBar.Visibility = Visibility.Visible;
            mThumb.Visibility = Visibility.Visible;
            mAnnotCommandBar.Visibility = Visibility.Collapsed;
            //m_menu.dismiss();
        }
        private void enter_search()
        {
            mNormalCommandBar.Visibility = Visibility.Collapsed;
            mThumb.Visibility = Visibility.Collapsed;
            mAnnotCommandBar.Visibility = Visibility.Collapsed;
        }
        async void export_pdf()
        {
            FileSavePicker picker = new FileSavePicker();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeChoices.Add("PDF", new List<String>() { ".PDF", ".pdf" });
            StorageFile file = await picker.PickSaveFileAsync();
            if (file == null) return;
            IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite);
            PDFDoc doc = new PDFDoc();
            doc.Create(stream);
            m_docx.m_doc.ExportPDF(doc);
            doc.Save();
            doc.Close();
            stream.Dispose();
        }
        private void mBack_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (m_reader == null) return;
            m_thumb.DOCXClose();
            m_thumb = null;

            m_docx.m_close?.Invoke((int)m_reader.DOCXViewMode, m_reader.DOCXGetCurPos());
            m_reader.DOCXClose();
            m_reader = null;

            mBack.Click -= mBack_Click;
            mCancel.Click -= mCancel_Click;
            mOk.Click -= mOk_Click;
            mTool.Click -= mTool_Click;
            mVMode.Click -= mVMode_Click;
            m_docx.release();
            m_docx = null;

            Frame.GoBack();
        }
        private void mVMode_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (!mViewModeFlyout.IsOpen)
                FlyoutBase.ShowAttachedFlyout(mVMode);
            else
                mViewModeFlyout.Hide();
        }
        private void mTool_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)//search / find
        {
            if (!mToolsFlyout.IsOpen)
                FlyoutBase.ShowAttachedFlyout(mTool);
            else
                mToolsFlyout.Hide();
        }
        private void mOk_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            enter_normal();
        }
        private void mCancel_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            enter_normal();
        }

        public void OnDOCXScaleChanged(double scale)
        {
        }

        public void OnDOCXPageChanged(int pageno)
        {
            m_thumb.DOCXSetSelPage(pageno);
        }

        public void OnDOCXSingleTapped(float x, float y)
        {
            if (mNormalCommandBar.Visibility == Visibility.Visible)
            {
                mNormalCommandBar.Visibility = Visibility.Collapsed;
                mThumb.Visibility = Visibility.Collapsed;
            }
            else
            {
                mNormalCommandBar.Visibility = Visibility.Visible;
                mThumb.Visibility = Visibility.Visible;
            }
        }

        public void OnDOCXPageTapped(int pageno)
        {
        }

        public void OnDOCXLongPressed(float x, float y)
        {
        }

        public void OnDOCXFound(bool found)
        {
        }

        public void OnDOCXSelecting(Canvas canvas, RDRect rect1, RDRect rect2)
        {
        }

        public void OnDOCXSelected()
        {
            DlgMarkup content = new DlgMarkup();
            Rect rect;
            rect.X = (mRoot.ActualWidth - 180) * 0.5;
            rect.Y = (mRoot.ActualHeight - 210 - 40) * 0.5;
            rect.Width = 180;
            rect.Height = 210;
            PDFDialog dlg = null;
            dlg = new PDFDialog(content,
                (ContentDialog sender, ContentDialogButtonClickEventArgs e) =>
                {
                    dlg.Hide();
                    int itype = content.getSelType();
                    if (itype < 0) return;
                    if (itype == 100)
                    {
                        Clipboard.Clear();
                        DataPackage dp = new DataPackage();
                        dp.SetData(StandardDataFormats.Text, m_reader.DOCXSelGetText());
                        Clipboard.SetContent(dp);
                    }
                    else
                    {
                        uint color = 0xFFFFFF00;
                        if (itype == 1)
                            color = 0xFF0000C0;
                        else if (itype == 2)
                            color = 0xFFC00000;
                        else if (itype == 4)
                            color = 0xFF00C000;
                        //not supportted.
                    }
                    m_reader.DOCXSelEnd();
                },
                (ContentDialog sender, ContentDialogButtonClickEventArgs e) =>
                {
                    dlg.Hide();
                    m_reader.DOCXSelEnd();
                });
            dlg.ShowAsync();
        }

        public void OnDOCXURI(string uri)
        {
            //todo: shall open dialog and display in webview.
        }

        public void OnPageSelected(int pageno)
        {
            m_reader.DOCXGotoPage(pageno);
        }

        private void mViewMode_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem item = sender as MenuFlyoutItem;
            int tag = Convert.ToInt32(item.Tag);
            m_reader.DOCXViewMode = (RDLAYOUT_MODE)tag;
        }

        private void mTools_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem item = sender as MenuFlyoutItem;
            int tag = Convert.ToInt32(item.Tag);
            switch (tag)
            {
                case 0://search
                    {
                        enter_search();
                        PopSearch search = new PopSearch();
                        Rect rcl;
                        rcl.X = 0;
                        rcl.Y = 0;
                        rcl.Width = 220;
                        rcl.Height = 150;
                        String skey = "";
                        bool mcase = false;
                        bool mwhole = false;
                        PDFPopup pop_out = new PDFPopup(mRoot, search, rcl);
                        search.init((object sender1, RoutedEventArgs e1) =>
                        {
                            String sskey = search.getKey();
                            bool mmcase = search.IsCase();
                            bool mmwhole = search.IsWhole();
                            Button btn = (Button)sender1;
                            if (skey != sskey || mcase != mmcase || mwhole != mmwhole)
                            {
                                skey = sskey;
                                mcase = mmcase;
                                mwhole = mmwhole;
                                m_reader.DOCXFindStart(skey, mcase, mwhole);
                            }
                            m_reader.DOCXFind((int)btn.Tag);
                        });
                    }
                    break;
                case 1://selection
                    m_reader.DOCXSelStart();
                    break;
                case 2://export PDF
                    export_pdf();
                    break;
                default:
                    break;
            }
        }

        private void mCommandBar_SizeChanged(object sender, SizeChangedEventArgs e)
        {

            double btn_w = mBack.ActualWidth;
            int btn_cnt = mNormalCommandBar.PrimaryCommands.Count - 1;//remove empty button.
            double empty_w = 0;
            if (mNormalCommandBar.IsOpen)
            {
                empty_w = mNormalCommandBar.ActualWidth - btn_w * btn_cnt - 48;//48 is padding width.
                if (empty_w < 0) empty_w = 0;
                mNormalEmpty.Width = empty_w;
            }
            else
            {
                empty_w = mAnnotCommandBar.ActualWidth - btn_w * btn_cnt - 48;//48 is padding width.
                if (empty_w < 0) empty_w = 0;
                mAnnotEmpty.Width = empty_w;
            }

        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(AboutPage));
        }
    }
}
