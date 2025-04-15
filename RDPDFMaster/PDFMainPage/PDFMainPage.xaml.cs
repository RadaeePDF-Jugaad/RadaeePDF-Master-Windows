using RDDLib.comm;
using RDDLib.docx;
using RDDLib.docxv;
using RDDLib.pdf;
using RDDLib.pdfv;
using RDDOCXReader;
using RDPDFMaster.Modules.FTS;
using RDPDFReader;
using RDPDFReader.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RDPDFMaster
{
    public sealed class RecentItem
    {
        String _Title;
        //event PropertyChangedEventHandler _PropertyChanged;

        public RecentItem(String token)
        {
            _token = token;
        }
        //Title
        public String Title
        {
            get
            {
                return _Title;
            }
            set
            {
                _Title = value;
                //OnPropertyChanged("Title");
            }
        }
        public String get_token()
        {
            return _token;
        }
        private String _token;
    };

    [Windows.Foundation.Metadata.WebHostHiddenAttribute]
    public sealed class RecentListData
    {
        private List<RecentItem> _items;
        public RecentListData()
        {
            _items = new List<RecentItem>();
            //Uri _baseUri = new Uri("ms-appx:///");
            //String LONG_LOREM_IPSUM = "";//"Curabitur class aliquam vestibulum nam curae maecenas sed integer cras phasellus suspendisse quisque donec dis praesent accumsan bibendum pellentesque condimentum adipiscing etiam consequat vivamus dictumst aliquam duis convallis scelerisque est parturient ullamcorper aliquet fusce suspendisse nunc hac eleifend amet blandit facilisi condimentum commodo scelerisque faucibus aenean ullamcorper ante mauris dignissim consectetuer nullam lorem vestibulum habitant conubia elementum pellentesque morbi facilisis arcu sollicitudin diam cubilia aptent vestibulum auctor eget dapibus pellentesque inceptos leo egestas interdum nulla consectetuer suspendisse adipiscing pellentesque proin lobortis sollicitudin augue elit mus congue fermentum parturient fringilla euismod feugiat";
        }
        public List<RecentItem> Items
        {
            get
            {
                return _items;
            }
        }
        internal void add(String name, String token)
        {
            RecentItem item = new RecentItem(token);
            item.Title = name;
            _items.Insert(0, item);
            //_items.Add(item);
        }
    };

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PDFMainPage : Page
    {
        static private string mFileToken;
        static public string FileToken
        {
            get
            {
                return mFileToken;
            }
        }

        public PDFMainPage()
        {
            this.InitializeComponent();
        }
        private void pdf_error(IRandomAccessStream stream, String msg)
        {
            stream.Dispose();
            DlgError dlg = new DlgError();
            dlg.Title = "Error";
            dlg.m_msg = msg;
            dlg.ShowAsync().Completed = new AsyncOperationCompletedHandler<ContentDialogResult>((IAsyncOperation<ContentDialogResult> op, AsyncStatus status) =>
            {
                //do nothing
            });
        }
        private void pdf_open(IRandomAccessStream stream, PDFDoc doc)
        {
            PDFReaderPara para = new PDFReaderPara(stream, doc, pdf_close);
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey(mFileToken + "_vmode"))
                para.m_vmode = Convert.ToInt32(ApplicationData.Current.LocalSettings.Values[mFileToken + "_vmode"]);
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey(FileToken + "_page"))
                para.m_pos.pageno = Convert.ToInt32(ApplicationData.Current.LocalSettings.Values[FileToken + "_page"]);
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey(FileToken + "_x"))
                para.m_pos.x = Convert.ToInt32(ApplicationData.Current.LocalSettings.Values[FileToken + "_x"]);
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey(FileToken + "_y"))
                para.m_pos.y = Convert.ToInt32(ApplicationData.Current.LocalSettings.Values[FileToken + "_y"]);
            Frame.Navigate(typeof(RDPDFReader.PDFReaderPage), new object[] { para, mFileName });
            string p = ApplicationData.Current.LocalFolder.Path;

            //Extracts the text from the document and add it to the FTS DB table.
            Task.Run(() =>
            {
                _ = RadaeeFTSManager.AddIndexAsync(doc, false);
            });
        }
        private void docx_open(IRandomAccessStream stream, DOCXDoc doc)
        {
            DOCXReaderPara para = new DOCXReaderPara(stream, doc, docx_close);
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey(mFileToken + "_vmode"))
                para.m_vmode = Convert.ToInt32(ApplicationData.Current.LocalSettings.Values[mFileToken + "_vmode"]);
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey(FileToken + "_page"))
                para.m_pos.pageno = Convert.ToInt32(ApplicationData.Current.LocalSettings.Values[FileToken + "_page"]);
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey(FileToken + "_x"))
                para.m_pos.x = Convert.ToInt32(ApplicationData.Current.LocalSettings.Values[FileToken + "_x"]);
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey(FileToken + "_y"))
                para.m_pos.y = Convert.ToInt32(ApplicationData.Current.LocalSettings.Values[FileToken + "_y"]);
            Frame.Navigate(typeof(DOCXReaderPage), new object[] { para, mFileName });
        }
        private void file_open(IRandomAccessStream stream, String password)
        {
            PDFDoc doc_pdf = new PDFDoc();
            RD_ERROR err = doc_pdf.Open(stream, password);
            switch (err)
            {
                case RD_ERROR.err_open:
                    pdf_error(stream, "can't open the PDF file.");
                    break;
                case RD_ERROR.err_invalid_para:
                    pdf_error(stream, "invalid parameter.");
                    break;
                case RD_ERROR.err_encrypt:
                    pdf_error(stream, "PDF file encrypted with unknown filter.");
                    break;
                case RD_ERROR.err_bad_file:
                    break;
                case RD_ERROR.err_password:
                    {
                        DlgPassword dlg = new DlgPassword();
                        dlg.ShowAsync().Completed = new AsyncOperationCompletedHandler<ContentDialogResult>((IAsyncOperation<ContentDialogResult> op, AsyncStatus status) =>
                        {
                            if (op.GetResults() != ContentDialogResult.Primary)
                            {
                                stream.Dispose();
                                return;
                            }
                            //to ensure pdf_open function run on UI thread.
                            Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { file_open(stream, dlg.m_pswd); });
                        });
                    }
                    break;
                case RD_ERROR.err_ok:
                    pdf_open(stream, doc_pdf);
                    break;
                default:
                    pdf_error(stream, "Unknown error.");
                    break;
            }
            if (err != RD_ERROR.err_bad_file) return;
            doc_pdf = null;
            DOCXDoc doc_docx = new DOCXDoc();
            err = doc_docx.Open(stream, password);
            switch (err)
            {
                case RD_ERROR.err_open:
                    pdf_error(stream, "can't open the PDF file.");
                    break;
                case RD_ERROR.err_invalid_para:
                    pdf_error(stream, "invalid parameter.");
                    break;
                case RD_ERROR.err_encrypt:
                    pdf_error(stream, "PDF file encrypted with unknown filter.");
                    break;
                case RD_ERROR.err_bad_file:
                    pdf_error(stream, "File damaged or invalid format.");
                    break;
                case RD_ERROR.err_password:
                    {
                        DlgPassword dlg = new DlgPassword();
                        dlg.ShowAsync().Completed = new AsyncOperationCompletedHandler<ContentDialogResult>((IAsyncOperation<ContentDialogResult> op, AsyncStatus status) =>
                        {
                            if (op.GetResults() != ContentDialogResult.Primary)
                            {
                                stream.Dispose();
                                return;
                            }
                            //to ensure pdf_open function run on UI thread.
                            Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { file_open(stream, dlg.m_pswd); });
                        });
                    }
                    break;
                case RD_ERROR.err_ok:
                    docx_open(stream, doc_docx);
                    break;
                default:
                    pdf_error(stream, "Unknown error.");
                    break;
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null)
            {
                StorageFile file = e.Parameter as StorageFile;
                if (file != null)
                {
                    if (StorageApplicationPermissions.MostRecentlyUsedList.Entries.Count >= 25)
                    {
                        RemoveLastFile();
                    }
                    mFileToken = StorageApplicationPermissions.MostRecentlyUsedList.Add(file);
                    IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite);
                    if (stream != null) file_open(stream, "");
                }
            }
            UpdateRecentList();
        }

        private void pdf_close(int vmode, PDFPos pos)
        {
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(mFileToken + "_vmode"))
                ApplicationData.Current.LocalSettings.Values.Add(mFileToken + "_vmode", vmode);
            else
                ApplicationData.Current.LocalSettings.Values[mFileToken + "_vmode"] = pos.pageno;

            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(mFileToken + "_page"))
                ApplicationData.Current.LocalSettings.Values.Add(mFileToken + "_page", pos.pageno);
            else
                ApplicationData.Current.LocalSettings.Values[mFileToken + "_page"] = pos.pageno;

            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(mFileToken + "_x"))
                ApplicationData.Current.LocalSettings.Values.Add(mFileToken + "_x", pos.x);
            else
                ApplicationData.Current.LocalSettings.Values[mFileToken + "_x"] = pos.x;
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(mFileToken + "_y"))
                ApplicationData.Current.LocalSettings.Values.Add(mFileToken + "_y", pos.y);
            else
                ApplicationData.Current.LocalSettings.Values[mFileToken + "_y"] = pos.y;
        }
        private void docx_close(int vmode, DOCXPos pos)
        {
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(mFileToken + "_vmode"))
                ApplicationData.Current.LocalSettings.Values.Add(mFileToken + "_vmode", vmode);
            else
                ApplicationData.Current.LocalSettings.Values[mFileToken + "_vmode"] = pos.pageno;

            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(mFileToken + "_page"))
                ApplicationData.Current.LocalSettings.Values.Add(mFileToken + "_page", pos.pageno);
            else
                ApplicationData.Current.LocalSettings.Values[mFileToken + "_page"] = pos.pageno;

            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(mFileToken + "_x"))
                ApplicationData.Current.LocalSettings.Values.Add(mFileToken + "_x", pos.x);
            else
                ApplicationData.Current.LocalSettings.Values[mFileToken + "_x"] = pos.x;
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(mFileToken + "_y"))
                ApplicationData.Current.LocalSettings.Values.Add(mFileToken + "_y", pos.y);
            else
                ApplicationData.Current.LocalSettings.Values[mFileToken + "_y"] = pos.y;
        }

        private void About_Click(Object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(AboutPage));
        }

        async private void OnFileListItemClicked(Object sender, ItemClickEventArgs e)
        {
            try
            {
                RecentItem clickedItem = e.ClickedItem as RecentItem;
                if (clickedItem != null)
                {
                    String token = clickedItem.get_token();
                    StorageFile file = await StorageApplicationPermissions.MostRecentlyUsedList.GetFileAsync(token);
                    if (file != null)
                    {
                        mFileName = file.Name;
                        mFileToken = token;
                        try
                        {
                            IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite);
                            if (stream != null) file_open(stream, "");
                        }
                        catch (Exception ex)
                        {
                            Windows.UI.Popups.MessageDialog msgDlg = new Windows.UI.Popups.MessageDialog("Can't open The PDF file, maybe file is opened by other App, or deleted.") { Title = "Open Failed" };
                            msgDlg.Commands.Add(new Windows.UI.Popups.UICommand("OK", uiCommand => { }));
                            msgDlg.ShowAsync();
                        }
                    }
                }
            }
            catch (Exception)
            {
                await new MessageDialog("Cannot open file, please try again later", "Warning").ShowAsync();
            }
        }

        private void RemoveLastFile()
        {
            AccessListEntryView list = StorageApplicationPermissions.MostRecentlyUsedList.Entries;
            AccessListEntry first = list.First<AccessListEntry>();
            String token = first.Token;
            StorageApplicationPermissions.MostRecentlyUsedList.Remove(token);
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey(token))
            {
                ApplicationData.Current.LocalSettings.Values.Remove(token);
                if (mFileToken.Equals(token))
                    mFileToken = string.Empty;
            }
        }

        async private void UpdateRecentList()
        {
            //Load recent used files
            AccessListEntryView list = StorageApplicationPermissions.MostRecentlyUsedList.Entries;
            List<AccessListEntry> recentFileList = list.ToList();
            if (recentFileList.Count == 0)
            {
                mEmptyListHint.Visibility = Visibility.Visible;
            }
            else
            {
                mEmptyListHint.Visibility = Visibility.Collapsed;
                RecentListData data = new RecentListData();
                foreach (AccessListEntry entry in recentFileList)
                {
                    try
                    {
                        StorageFile file = await StorageApplicationPermissions.MostRecentlyUsedList.GetFileAsync(entry.Token);
                        if (file != null)
                        {
                            data.add(file.Name, entry.Token);
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
                FileList.ItemsSource = data.Items;
            }
        }
        private string mFileName;
        async private void BrowsFileButton_Click(Object sender, RoutedEventArgs e)
        {
            FileOpenPicker filePicker;
            filePicker = new FileOpenPicker();
            filePicker.ViewMode = PickerViewMode.List;
            filePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            filePicker.FileTypeFilter.Add(".PDF");
            filePicker.FileTypeFilter.Add(".pdf");
            filePicker.FileTypeFilter.Add(".DOCX");
            filePicker.FileTypeFilter.Add(".docx");
            StorageFile file = await filePicker.PickSingleFileAsync();
            if (file != null)
            {
                // Application now has read/write access to the picked file
                mFileName = file.Name;
                /*
                IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite);
                if (stream != null)
                {
                    mFileToken = StorageApplicationPermissions.MostRecentlyUsedList.Add(file);
                    file_open(stream, "");
                }
                */
                try
                {
                    IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite);
                    if (stream != null)
                    {
                        mFileToken = StorageApplicationPermissions.MostRecentlyUsedList.Add(file);
                        file_open(stream, "");
                    }
                }
                catch (Exception ex1)
                {
                    try
                    {
                        IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read);
                        if (stream != null)
                        {
                            mFileToken = StorageApplicationPermissions.MostRecentlyUsedList.Add(file);
                            file_open(stream, "");
                        }
                    }
                    catch (Exception ex2)
                    {
                        Windows.UI.Popups.MessageDialog msgDlg = new Windows.UI.Popups.MessageDialog("Can't open The PDF file, maybe file is opened by other App.") { Title = "Open Failed" };
                        msgDlg.Commands.Add(new Windows.UI.Popups.UICommand("OK", uiCommand => { }));
                        msgDlg.ShowAsync();
                    }
                }
            }
        }
    }

    public class TitleToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
                return null;
            string title = value as string;
            string resourcePath = Package.Current.InstalledLocation.Path + "\\Assets\\imgs\\";
            if (title.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                resourcePath = resourcePath + "pdf_icon.png";
            else if (title.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                resourcePath = resourcePath + "docx_icon.png";
            return new BitmapImage(new Uri(resourcePath));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
