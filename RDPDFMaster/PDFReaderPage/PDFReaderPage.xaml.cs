using com.radaee.master;
using RDDLib.comm;
using RDDLib.pdf;
using RDDLib.pdfv;
using RDDLib.reader;
using RDPDFMaster;
using RDPDFMaster.Modules.FTS;
using RDPDFMaster.PDFReaderPage;
using RDPDFMaster.util;
using RDPDFReader.annotui;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Core.Preview;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace RDPDFReader
{
    public delegate void OnPDFPageCloseHandler(int vmode, PDFPos pos);
    public sealed class PDFReaderPara
    {
        public PDFReaderPara(IRandomAccessStream stream, PDFDoc doc, OnPDFPageCloseHandler close)
        {
            m_doc = doc;
            m_stream = stream;
            m_close = close;
            m_vmode = 0;
            m_pos.pageno = -1;
            m_pos.x = 0;
            m_pos.y = 0;
        }
        ~PDFReaderPara()
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
        public PDFDoc m_doc;
        public OnPDFPageCloseHandler m_close;
        public int m_vmode;
        public PDFPos m_pos;
    }

    public class AttachmentItem
    {
        public string ESName;
        public int index;
    }

    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class PDFReaderPage : Page, IPDFViewListener, IThumbListener
    {
        private PDFReaderPara m_pdf;
        private PDFReader m_reader;
        private PDFThumb m_thumb;
        private MenuAnnot m_menu;
        private PopEdit m_pedit;
        private PopCombo m_pcombo;
        private ObservableCollection<AttachmentItem> mAttachmentsList;
        private string mFileName = "";

        PDFPopup mSearchPopup;
        FTS CurrentSearchResult;
        private List<FTS> FTSResult;

        public PDFReaderPage()
        {
            this.InitializeComponent();
            //NavigationCacheMode = NavigationCacheMode.Required;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            NavigationMode mode = e.NavigationMode;
            object[] args = (object[])e.Parameter;
            m_pdf = (PDFReaderPara)args[0];
            mFileName = (string)args[1];
            m_reader = new PDFReader();
            //m_reader.PDFOpen(mView, m_pdf.m_doc, PDF_LAYOUT_MODE.layout_hdual, this);

            if (m_pdf.m_vmode == 0)
                m_reader.PDFOpen(mView, m_pdf.m_doc, RDLAYOUT_MODE.layout_vert, this);
            else
                m_reader.PDFOpen(mView, m_pdf.m_doc, (RDLAYOUT_MODE)m_pdf.m_vmode, this);
            m_thumb = new PDFThumb();
            m_thumb.PDFOpen(mThumb, m_pdf.m_doc, this);
            m_menu = new MenuAnnot();
            m_menu.init(onAnnotPerform, onAnnotEdit, onAnnotRemove, onAnnotProperty);

            if (m_pdf.m_pos.pageno >= 0)
                m_reader.PDFGotoPage(m_pdf.m_pos.pageno);
            m_pedit = new PopEdit();
            m_pcombo = new PopCombo();
            Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => { m_thumb.PDFSetSelPage(m_pdf.m_pos.pageno); });
            enter_normal();
            mAttachmentsList = new ObservableCollection<AttachmentItem>();
            initAttachmentList();

            App.SetPage(this);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            App.SetPage(null);
            base.OnNavigatingFrom(e);
        }

        private void initAttachmentList()
        {
            mAttachmentsList.Clear();
            int count = m_pdf.m_doc.EFCount;
            for (int index = 0; index < count; index++)
            {
                AttachmentItem item = new AttachmentItem();
                item.ESName = m_pdf.m_doc.GetEFName(index);
                item.index = index;
                mAttachmentsList.Add(item);
            }
        }
        private void play_media(String path, PDFAnnot annot)
        {
            RichMediaControl media = new RichMediaControl();
            MediaElement player = media.getPlayer();
            player.Source = new Uri(path);
            player.Play();
            RDRect rc = annot.Rect;
            float aw = rc.right - rc.left;
            float ah = rc.bottom - rc.top;
            Rect rect = new Rect();
            rect.X = (mRoot.ActualWidth - 400) * 0.5;
            rect.Y = (mRoot.ActualHeight - 400 * ah / aw) * 0.5;
            rect.Width = 400;
            rect.Height = 400 * ah / aw;
            PDFDialog dlg = null;
            dlg = new PDFDialog(media,
                (ContentDialog sender1, ContentDialogButtonClickEventArgs e1) =>
                {
                    m_reader.PDFAnnotEnd();
                    dlg.Hide();
                },
                (ContentDialog sender1, ContentDialogButtonClickEventArgs e1) =>
                {
                    m_reader.PDFAnnotEnd();
                    dlg.Hide();
                });
            player.MediaEnded += (object sender1, RoutedEventArgs e1) =>
            {
                player.Stop();
                dlg.Hide();
            };
            media.OnButtonClick += (int btnCode) =>
            {
                switch (btnCode)
                {
                    case 0:
                        //play
                        player.Play();
                        break;
                    case 1:
                        //pause
                        player.Pause();
                        break;
                    case 2:
                        //stop
                        player.Stop();
                        dlg.Hide();
                        break;
                }
            };
            dlg.ShowAsync();
        }
        private void enter_normal()
        {
            mNormalCommandBar.Visibility = Visibility.Visible;
            mThumb.Visibility = Visibility.Visible;
            mAnnotCommandBar.Visibility = Visibility.Collapsed;
            m_annot_status = -1;
            m_menu.dismiss();
            m_pedit.dismiss();
            m_pcombo.dismiss();
            m_annot = null;
            m_annot_pno = -1;
        }
        private void enter_annot()
        {
            mNormalCommandBar.Visibility = Visibility.Collapsed;
            mThumb.Visibility = Visibility.Collapsed;
            mAnnotCommandBar.Visibility = Visibility.Visible;
        }
        private void enter_annot_edit()
        {
            mNormalCommandBar.Visibility = Visibility.Collapsed;
            mThumb.Visibility = Visibility.Collapsed;
            mAnnotCommandBar.Visibility = Visibility.Collapsed;
        }
        private void enter_search()
        {
            mNormalCommandBar.Visibility = Visibility.Collapsed;
            mThumb.Visibility = Visibility.Collapsed;
            mAnnotCommandBar.Visibility = Visibility.Collapsed;
        }
        private void onAnnotPerform(object sender, RoutedEventArgs e)
        {
            m_menu.dismiss();
            m_pedit.dismiss();
            m_pcombo.dismiss();
            m_reader.PDFAnnotPerform();
        }
        private void onAnnotEdit(object sender, RoutedEventArgs e)
        {
            m_menu.dismiss();
            m_pedit.dismiss();
            m_pcombo.dismiss();
            DlgPoptext content = new DlgPoptext();
            content.setLabel(m_annot.PopupLabel);
            content.setPopup(m_annot.PopupText);
            Rect rect = new Rect();
            rect.X = (mRoot.ActualWidth - 280) * 0.5;
            rect.Y = (mRoot.ActualHeight - 210 - 40) * 0.5;
            rect.Width = 280;
            rect.Height = 210;
            PDFDialog dlg = null;
            dlg = new PDFDialog(content,
                (ContentDialog sender1, ContentDialogButtonClickEventArgs e1) =>
                {
                    m_annot.PopupLabel = content.getLabel();
                    m_annot.PopupText = content.getPopup();
                    m_reader.PDFAnnotEnd();
                    dlg.Hide();
                },
                (ContentDialog sender1, ContentDialogButtonClickEventArgs e1) =>
                {
                    m_reader.PDFAnnotEnd();
                    dlg.Hide();
                }
                );
            dlg.ShowAsync();

        }
        private void onAnnotRemove(object sender, RoutedEventArgs e)
        {
            m_menu.dismiss();
            m_pedit.dismiss();
            m_pcombo.dismiss();
            m_reader.PDFAnnotRemove();
        }
        private void onAnnotProperty(object sender, RoutedEventArgs e)
        {
            m_menu.dismiss();
            m_pedit.dismiss();
            m_pcombo.dismiss();
            PDFDialog dlg = null;
            FrameworkElement view = null;
            IDlgProp iprop = null;
            Rect rect;
            int atype = m_annot.Type;
            switch (m_annot.Type)
            {
                case 4:
                case 8:
                    {
                        DlgPropLine prop = new DlgPropLine();
                        prop.loadAnnot(m_annot);
                        view = prop;
                        iprop = prop;
                        rect.X = (mRoot.ActualWidth - 310) * 0.5;
                        rect.Y = (mRoot.ActualHeight - 350 - 40) * 0.5;
                        rect.Width = 310;
                        rect.Height = 350;
                    }
                    break;
                case 3:
                case 5:
                case 6:
                case 7:
                    {
                        DlgPropComm prop = new DlgPropComm();
                        prop.loadAnnot(m_annot, true);
                        view = prop;
                        iprop = prop;
                        rect.X = (mRoot.ActualWidth - 310) * 0.5;
                        rect.Y = (mRoot.ActualHeight - 280 - 40) * 0.5;
                        rect.Width = 310;
                        rect.Height = 280;
                    }
                    break;
                case 15:
                    {
                        DlgPropComm prop = new DlgPropComm();
                        prop.loadAnnot(m_annot, false);
                        view = prop;
                        iprop = prop;
                        rect.X = (mRoot.ActualWidth - 310) * 0.5;
                        rect.Y = (mRoot.ActualHeight - 280 - 40) * 0.5;
                        rect.Width = 310;
                        rect.Height = 280;
                    }
                    break;
                case 9:
                case 10:
                case 11:
                case 12:
                    {
                        DlgPropMarkup prop = new DlgPropMarkup();
                        prop.loadAnnot(m_annot);
                        view = prop;
                        iprop = prop;
                        rect.X = (mRoot.ActualWidth - 310) * 0.5;
                        rect.Y = (mRoot.ActualHeight - 180 - 40) * 0.5;
                        rect.Width = 310;
                        rect.Height = 180;
                    }
                    break;
                case 1:
                case 17:
                    {
                        DlgPropIcon prop = new DlgPropIcon();
                        prop.loadAnnot(m_annot);
                        view = prop;
                        iprop = prop;
                        rect.X = (mRoot.ActualWidth - 310) * 0.5;
                        rect.Y = (mRoot.ActualHeight - 220 - 40) * 0.5;
                        rect.Width = 310;
                        rect.Height = 220;
                    }
                    break;
                default:
                    m_reader.PDFAnnotEnd();
                    return;
                    //break;
            }
            dlg = new PDFDialog(view,
                (ContentDialog sender1, ContentDialogButtonClickEventArgs e1) =>
                {
                    iprop.updateAnnot();
                    m_reader.PDFUpdateAnnotPage();
                    m_reader.PDFAnnotEnd();
                    dlg.Hide();
                },
                (ContentDialog sender1, ContentDialogButtonClickEventArgs e1) =>
                {
                    m_reader.PDFAnnotEnd();
                    dlg.Hide();
                });
            dlg.ShowAsync();
        }
        public void OnPDFPageUpdated(int pageno)
        {
            if (m_thumb != null)
                m_thumb.PDFUpdatePage(pageno);
        }
        public void OnPDFScaleChanged(double scale)
        {
        }
        public void OnPDFPageChanged(int pageno)
        {
            if (m_thumb != null)
                m_thumb.PDFSetSelPage(pageno);
        }
        public void OnPDFSingleTapped(float x, float y)
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

            if (mAttachmentPanel.Visibility == Visibility.Visible)
                mAttachmentPanel.Visibility = Visibility.Collapsed;
        }
        public void OnPDFPageTapped(int pageno)
        {
        }
        public void OnPDFLongPressed(float x, float y)
        {
        }
        public void OnPDFFound(bool found)
        {
        }
        public void OnPDFSelecting(Canvas canvas, RDRect rect1, RDRect rect2)
        {
        }
        public void OnPDFSelected()
        {
            DlgMarkup content = new DlgMarkup();
            Rect rect;
            rect.X = (mRoot.ActualWidth - 180) * 0.5;
            rect.Y = (mRoot.ActualHeight - 240 - 40) * 0.5;
            rect.Width = 180;
            rect.Height = 240;
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
                        dp.SetData(StandardDataFormats.Text, m_reader.PDFSelGetText());
                        Clipboard.SetContent(dp);
                    }
                    else if (itype == 10)
                    {
                        m_reader.PDFSelRemove();
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
                        m_reader.PDFSelSetMarkup(color, itype);
                    }
                    m_reader.PDFSelEnd();
                },
                (ContentDialog sender, ContentDialogButtonClickEventArgs e) =>
                {
                    dlg.Hide();
                    m_reader.PDFSelEnd();
                });
            dlg.ShowAsync();
        }
        private PDFAnnot m_annot;
        private RDRect m_annot_rect;
        private int m_annot_pno;
        public async void OnPDFAnnotClicked(PDFPage page, int pageno, PDFAnnot annot, RDRect rect)
        {
            m_annot = annot;
            m_annot_pno = pageno;
            m_annot_rect = rect;
            if (m_pdf.m_doc.CanSave && annot.SignStatus == 0)
            {
                //Not signed
                DlgSignature dlgSignature = new DlgSignature(m_pdf.m_doc, rect, annot);
                dlgSignature.PrimaryButtonClick += (sender, args) =>
                {
                    m_reader.PDFUpdateAnnotPage();
                };
                await dlgSignature.ShowAsync();
            }
            else if (m_pdf.m_doc.CanSave && annot.SignStatus == 1)
            {
                //Signed
                DlgSignatureProp dlgSignatureProp = new DlgSignatureProp(annot.Sign, m_pdf.m_doc);
                await dlgSignatureProp.ShowAsync();
            }
            else if (m_pdf.m_doc.CanSave && annot.EditType > 0)//edit box
            {
                Rect rc;
                rc.X = rect.left;
                rc.Y = rect.top;
                rc.Width = rect.right - rect.left;
                rc.Height = rect.bottom - rect.top;

                double windowWidth = Window.Current.Bounds.Width;
                if (rc.X + rc.Width > windowWidth)
                {
                    rc.X = windowWidth - rc.Width;
                }

                Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    m_pedit.Text = annot.EditText;
                    m_pedit.show(mView, rc, annot.EditTextSize * m_reader.PDFGetScale(), annot.EditType == 3);
                    enter_annot_edit();
                });
            }
            else if (m_pdf.m_doc.CanSave && annot.ComboItemCount > 0)
            {
                Rect rc;
                rc.X = rect.left;
                rc.Y = rect.top;
                rc.Width = rect.right - rect.left;
                rc.Height = rect.bottom - rect.top;

                double windowWidth = Window.Current.Bounds.Width;
                if (rc.X + rc.Width > windowWidth)
                {
                    rc.X = windowWidth - rc.Width;
                }

                int cnt = annot.ComboItemCount;
                String[] opts = new string[cnt];
                for (int cur = 0; cur < cnt; cur++)
                    opts[cur] = annot.GetComboItem(cur);
                m_pcombo.show(mView, rc, m_reader.PDFGetScale(), opts, (object sender, RoutedEventArgs e) =>
                {
                    Button ele = (Button)sender;
                    int idx = (int)ele.Tag;
                    m_annot.ComboItemSel = idx;
                    m_pcombo.dismiss();
                    m_reader.PDFUpdateAnnotPage();
                    m_reader.PDFAnnotEnd();
                });
                enter_annot_edit();
            }
            else
            {
                Point pt;
                pt.X = rect.left;
                pt.Y = rect.top - 40;
                if (pt.Y < 0)
                    pt.Y = rect.bottom;
                m_menu.show(mView, annot.Type, pt);
                enter_annot_edit();
            }
        }

        public void OnPDFAnnotEnd()
        {
            if (m_pedit.isShow())
            {
                m_annot.EditText = m_pedit.Text;
                m_reader.PDFUpdateAnnotPage();
            }
            enter_normal();
        }
        public void OnPDFAnnotGoto(int pageno)
        {
            m_reader.PDFGotoPage(pageno);
        }
        public void OnPDFAnnotURI(string uri)
        {
        }
        private string Between(string STR, string FirstString, string LastString)
        {
            string FinalString;
            int Pos1 = STR.IndexOf(FirstString) + FirstString.Length;
            int Pos2 = STR.IndexOf(LastString);
            FinalString = STR.Substring(Pos1, Pos2 - Pos1);
            return FinalString;
        }
        private String m_media;
        public void OnPDFAnnotRichMedia(PDFAnnot annot)
        {
            try
            {
                if (annot != null)
                {
                    String movie;
                    if (annot.RichMediaItemCount > 0)
                    {
                        movie = Between(annot.GetRichMediaItemPara(0), "source=", "&");
                    }
                    else
                        movie = annot.GetMovieName();
                    if (movie != "")
                    {
                        movie = movie.Replace("%20", " "); //to solve the problem of the library returning the space replaced with %20
                        StorageFolder folder = ApplicationData.Current.TemporaryFolder;
                        String path = folder.Path;
                        m_media = path + "\\" + movie;
                        if (annot.GetRichMediaData(movie, m_media))
                            play_media(m_media, annot);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }
        public void OnPDFAnnotRendition(PDFAnnot annot)
        {
        }
        public void OnPDFAnnotPopup(PDFAnnot annot, string subj, string text)
        {
        }
        public void OnPDFAnnotRemoteDest(string dest)
        {
        }
        public void OnPDFAnnotFileLink(string filelink)
        {
        }

        public void vClose(bool close_doc)
        {
            if (m_reader == null) return;
            m_thumb.PDFClose();
            m_thumb = null;

            m_pdf.m_close?.Invoke((int)m_reader.PDFViewMode, m_reader.PDFGetCurPos());
            m_reader.PDFClose();
            m_reader = null;

            m_menu.uninit(onAnnotPerform, onAnnotEdit, onAnnotRemove, onAnnotProperty);
            m_menu = null;
            m_pedit = null;
            m_pcombo = null;
            mField.Click -= mField_Click;
            mAnnot.Click -= mAnnot_Click;
            mBack.Click -= mBack_Click;
            mCancel.Click -= mCancel_Click;
            mOk.Click -= mOk_Click;
            mTool.Click -= mTool_Click;
            mVMode.Click -= mVMode_Click;
            /*
            mAnnot = null;
            mBack = null;
            mCancel = null;
            mOk = null;
            mTool = null;
            mVMode = null;

            mBarAnnot = null;
            mBarCmd = null;
            mView = null;
            mThumb = null;
            mRoot = null;
            */
            if (close_doc) m_pdf.release();
            m_pdf = null;
            if (m_def != null)
                m_def.Complete();
            else if (Frame != null && Frame.CanGoBack)
                Frame.GoBack();
        }
        private Deferral m_def;
        public void OnAppClose(SystemNavigationCloseRequestedPreviewEventArgs eve)
        {
            if (m_reader.PDFModified && m_pdf.m_doc.CanSave)
            {
                ContentDialog dlg = new ContentDialog
                {
                    Title = "Save file?",
                    Content = "Do you want save PDF file?",
                    PrimaryButtonText = "Yes",
                    SecondaryButtonText = "No",
                    CloseButtonText = "Cancel"
                };
                m_def = eve.GetDeferral();
                dlg.DefaultButton = ContentDialogButton.Primary;
                dlg.PrimaryButtonClick += (ContentDialog sender1, ContentDialogButtonClickEventArgs e1) =>
                {
                    m_pdf.m_doc.Save();
                    vClose(true);
                };
                dlg.SecondaryButtonClick += (ContentDialog sender1, ContentDialogButtonClickEventArgs e1) =>
                {
                    vClose(true);
                };
                dlg.CloseButtonClick += (ContentDialog sender1, ContentDialogButtonClickEventArgs e1) =>
                {
                    eve.Handled = true;
                    m_def.Complete();
                };
                dlg.ShowAsync();
            }
            else
            {
                m_def = eve.GetDeferral();
                vClose(true);
            }
        }
        private void mBack_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (m_reader == null)
            {
                m_pdf.m_stream.Dispose();
                Frame.GoBack();
            }
            else if (m_reader.PDFModified && m_pdf.m_doc.CanSave)
            {
                ContentDialog dlg = new ContentDialog
                {
                    Title = "Save file?",
                    Content = "Do you want save PDF file?",
                    PrimaryButtonText = "Yes",
                    SecondaryButtonText = "No",
                    CloseButtonText = "Cancel"
                };
                m_def = null;
                dlg.DefaultButton = ContentDialogButton.Primary;
                dlg.PrimaryButtonClick += (ContentDialog sender1, ContentDialogButtonClickEventArgs e1) =>
                {
                    m_pdf.m_doc.Save();
                    vClose(true);
                };
                dlg.SecondaryButtonClick += (ContentDialog sender1, ContentDialogButtonClickEventArgs e1) =>
                {
                    vClose(true);
                };
                dlg.ShowAsync();
            }
            else
                vClose(true);
        }

        public void OnPageSelected(int pageno)
        {
            m_reader.PDFGotoPage(pageno);
        }
        private void mVMode_Click(object sender, RoutedEventArgs e)
        {
            if (!mViewModeFlyout.IsOpen)
                FlyoutBase.ShowAttachedFlyout(mVMode);
            else
                mViewModeFlyout.Hide();
        }
        private void OnPageConfirmed(int[] rotates, bool[] deletes)
        {
            if (rotates == null || deletes == null) return;
            m_reader.PDFSaveView();
            m_thumb.PDFSaveView();
            PDFDoc doc = m_pdf.m_doc;
            int cur = deletes.Length;
            bool modified = false;
            while (cur > 0)
            {
                cur--;
                if (deletes[cur])
                {
                    doc.RemovePage(cur);
                    modified = true;
                }
                else
                {
                    int rot_org = rotates[cur] >> 16;
                    int rot_new = rotates[cur] & 0xFFFF;
                    if (rot_org != rot_new)
                    {
                        //if (rot_new > rot_org) rot_new -= rot_org;
                        //else rot_new = rot_new + 360 - rot_org;
                        PDFPage page = doc.GetPage(cur);
                        page.UpdateWithPGEditor();//required for page editing mdoe.
                        page.Close();
                        doc.SetPageRotate(cur, rot_new);
                        modified = true;
                    }
                }
            }
            m_thumb.PDFRestoreView();
            m_reader.PDFRestoreView();
            if (modified) m_reader.PDFModified = true;
        }
        private void mTool_Click(object sender, RoutedEventArgs e)
        {
            if (!mToolsFlyout.IsOpen)
                FlyoutBase.ShowAttachedFlyout(mTool);
            else
                mToolsFlyout.Hide();
        }
        private int m_annot_status = -1;
        private void mAnnot_Click(object sender, RoutedEventArgs e)
        {
            if (!m_pdf.m_doc.CanSave)
            {
                Windows.UI.Popups.MessageDialog msgDlg = new Windows.UI.Popups.MessageDialog("The PDF File is not writable, maybe opened by other App.") { Title = "Readonly" };
                msgDlg.Commands.Add(new Windows.UI.Popups.UICommand("OK", uiCommand => { }));
                msgDlg.ShowAsync();
                return;
            }
            if (!mAnnotFlyout.IsOpen)
                FlyoutBase.ShowAttachedFlyout(mAnnot);
            else
                mAnnotFlyout.Hide();
        }
        private void mField_Click(object sender, RoutedEventArgs e)
        {
            if (!mFieldFlyout.IsOpen)
                FlyoutBase.ShowAttachedFlyout(mField);
            else
                mFieldFlyout.Hide();
        }

        private void mCancel_Click(object sender, RoutedEventArgs e)
        {
            switch (m_annot_status)
            {
                case 0:
                    m_reader.PDFInkCancel();
                    break;
                case 1:
                    m_reader.PDFNoteCancel();
                    break;
                case 2:
                    m_reader.PDFLineCancel();
                    break;
                case 3:
                    m_reader.PDFRectCancel();
                    break;
                case 4:
                    m_reader.PDFEllipseCancel();
                    break;
                case 5:
                    m_reader.PDFStampCancel();
                    break;
                case 6:
                    m_reader.PDFEditTextCancel();
                    break;
                case 7:
                    m_reader.PDFPolygonCancel();
                    break;
                case 8:
                    m_reader.PDFPolylineCancel();
                    break;
                case 20:
                    m_reader.PDFFieldCheckCancel();
                    break;
                case 21:
                    m_reader.PDFFieldRadioCancel();
                    break;
                case 22:
                    m_reader.PDFFieldSignCancel();
                    break;
                case 23:
                    m_reader.PDFFieldEditTextCancel();
                    break;
                case 24:
                    m_reader.PDFPageEditCancel();
                    break;
                default:
                    break;
            }
            enter_normal();
            m_annot_status = -1;
        }

        private void mOk_Click(object sender, RoutedEventArgs e)
        {
            switch (m_annot_status)
            {
                case 0:
                    m_reader.PDFInkEnd();
                    break;
                case 1:
                    m_reader.PDFNoteEnd();
                    break;
                case 2:
                    m_reader.PDFLineEnd();
                    break;
                case 3:
                    m_reader.PDFRectEnd();
                    break;
                case 4:
                    m_reader.PDFEllipseEnd();
                    break;
                case 5:
                    m_reader.PDFStampEnd();
                    break;
                case 6:
                    m_reader.PDFEditTextEnd();
                    break;
                case 7:
                    m_reader.PDFPolygonEnd();
                    break;
                case 8:
                    m_reader.PDFPolylineEnd();
                    break;
                case 20:
                    m_reader.PDFFieldCheckEnd();
                    break;
                case 21:
                    m_reader.PDFFieldRadioEnd();
                    break;
                case 22:
                    m_reader.PDFFieldSignEnd();
                    break;
                case 23:
                    m_reader.PDFFieldEditTextEnd();
                    break;
                case 24:
                    m_reader.PDFPageEditEnd();
                    break;
                default:
                    break;
            }
            enter_normal();
            m_annot_status = -1;
        }

        private void mEditText_Click(object sender, RoutedEventArgs e)
        {
            if (!m_pdf.m_doc.CanSave)
            {
                Windows.UI.Popups.MessageDialog msgDlg = new Windows.UI.Popups.MessageDialog("The PDF File is not writable, maybe opened by other App.") { Title = "Readonly" };
                msgDlg.Commands.Add(new Windows.UI.Popups.UICommand("OK", uiCommand => { }));
                msgDlg.ShowAsync();
                return;
            }
            m_annot_status = 24;
            m_reader.PDFPageEditStart();
            enter_annot();
        }

        private void mEditPage_Click(object sender, RoutedEventArgs e)
        {
            mPages.PDFOpen(m_pdf.m_doc, OnPageConfirmed, OnPageSelected);
        }

        private void Delete_ES_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Image image = sender as Image;
            int index = (int)image.Tag;
            m_pdf.m_doc.DelEFData(index);
            initAttachmentList();
            if (m_pdf.m_doc.CanSave)
                m_reader.PDFModified = true;

        }

        private void mAttachmentBtn_Click(object sender, RoutedEventArgs e)
        {
            if (mAttachmentPanel.Visibility == Visibility.Visible)
                mAttachmentPanel.Visibility = Visibility.Collapsed;
            else
                mAttachmentPanel.Visibility = Visibility.Visible;
        }

        //private async void mAttachmentView_ItemClick(object sender, ItemClickEventArgs e)
        //{
        //    AttachmentItem item = e.ClickedItem as AttachmentItem;
        //    string name = m_pdf.m_doc.GetEFName(item.index);
        //    string path = ApplicationData.Current.LocalCacheFolder.Path + "\\" + name;
        //    bool success = m_pdf.m_doc.GetEFData(item.index, path);
        //    if (success)
        //    {
        //        StorageFile file = await ApplicationData.Current.LocalCacheFolder.GetFileAsync(name);
        //        await Windows.System.Launcher.LaunchFileAsync(file);
        //    }
        //}

        private async void mAddAttachment_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FileOpenPicker filePicker;
            filePicker = new FileOpenPicker();
            filePicker.ViewMode = PickerViewMode.List;
            filePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            filePicker.FileTypeFilter.Add("*");
            StorageFile file = await filePicker.PickSingleFileAsync();
            if (file == null)
                return;
            string name = file.Name;
            StorageFile targetFile = await ApplicationData.Current.LocalCacheFolder.CreateFileAsync(name, CreationCollisionOption.ReplaceExisting);
            await file.CopyAndReplaceAsync(targetFile);
            bool success = m_pdf.m_doc.NewEF(targetFile.Path);
            if (success)
            {
                initAttachmentList();
                if (m_pdf.m_doc.CanSave)
                    m_reader.PDFModified = true;


            }
            else
            {
                ContentDialog contentDialog = new ContentDialog();
                contentDialog.Content = "Failed to add attachment!";
                contentDialog.Title = "Error";
                contentDialog.PrimaryButtonText = "OK";
                await contentDialog.ShowAsync();
            }
            targetFile.DeleteAsync();
        }

        private async void mAttachmentView_ItemClick(object sender, TappedRoutedEventArgs e)
        {
            TextBlock text = sender as TextBlock;
            int index = (int)text.Tag;

            string name = m_pdf.m_doc.GetEFName(index);
            string path = ApplicationData.Current.LocalCacheFolder.Path + "\\" + name;
            bool success = m_pdf.m_doc.GetEFData(index, path);
            if (success)
            {
                StorageFile file = await ApplicationData.Current.LocalCacheFolder.GetFileAsync(name);
                await Windows.System.Launcher.LaunchFileAsync(file);
            }
        }

        private void mViewMode_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem item = sender as MenuFlyoutItem;
            int tag = Convert.ToInt32(item.Tag);
            m_reader.PDFViewMode = (RDLAYOUT_MODE)tag;
        }

        private void mTools_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem item = sender as MenuFlyoutItem;
            int tag = Convert.ToInt32(item.Tag);
            switch (tag)
            {
                case 0://undo
                    m_reader.PDFUndo();
                    break;
                case 1://redo
                    m_reader.PDFRedo();
                    break;
                case 2://search
                    {
                        enter_search();
                        PopSearch search = new PopSearch();
                        Rect rcl;
                        rcl.X = 0;
                        rcl.Y = 0;
                        rcl.Width = 300;
                        rcl.Height = 180;
                        String skey = "";
                        bool mcase = false;
                        bool mwhole = false;
                        mSearchPopup = new PDFPopup(mRoot, search, rcl);
                        if (RadaeeFTSManager.SearchType != 1)
                        {
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
                                    m_reader.PDFFindStart(skey, mcase, mwhole);
                                }
                                m_reader.PDFFind((int)btn.Tag);
                            });
                            //search.SwitchFTSMode(true,
                            //    (searchBtn, searchArgs) =>
                            //    {
                            //        bool newQuery = false;
                            //        String currentQuery = search.getKey();
                            //        if (skey != currentQuery)
                            //        {
                            //            newQuery = true;
                            //            skey = currentQuery;
                            //        }
                            //        StartFTSSearch(skey.Trim(), newQuery);
                            //    },
                            //    (closeBtn, closeArgs) => { CloseSearch(); });
                        }
                        else
                        {
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
                                    m_reader.PDFFindStart(skey, mcase, mwhole);
                                }
                                m_reader.PDFFind((int)btn.Tag);
                            });
                        }

                        search.EnableFTSMode();
                        search.mFTSClicked += activate =>
                        {
                            mSearchPopup.DismissOnTouchOutside = !activate;
                            search.SwitchFTSMode(
                                activate,
                                (searchBtn, searchArgs) =>
                                {
                                    bool newQuery = false;
                                    String currentQuery = search.getKey();
                                    if (skey != currentQuery)
                                    {
                                        newQuery = true;
                                        skey = currentQuery;
                                    }
                                    StartFTSSearch(skey.Trim(), newQuery);
                                },
                                    (closeBtn, closeArgs) => { CloseSearch(); }
                                );
                        };
                    }
                    break;
                case 3://selection
                    m_reader.PDFSelStart();
                    break;
                case 4://meta
                    {
                        DlgMeta dlg = new DlgMeta();
                        dlg.loadMeta(m_pdf.m_doc);
                        dlg.ShowAsync();
                    }
                    break;
                case 5://outline
                    {
                        PopOutline outline = new PopOutline();
                        Rect rcl;
                        rcl.X = 0;
                        rcl.Y = mNormalCommandBar.Height;
                        rcl.Width = 200;
                        rcl.Height = mRoot.Height - rcl.Y;
                        PDFPopup pop_out = new PDFPopup(mRoot, outline, rcl);

                        outline.loadOutline(m_pdf.m_doc, (object sender1, RoutedEventArgs e1) =>
                        {
                            Button btn = (Button)sender1;
                            PDFOutline iout = (PDFOutline)btn.Tag;
                            if (iout.dest >= 0) OnPDFAnnotGoto(iout.dest);
                            pop_out.dismiss();
                        });
                    }
                    break;
                default:
                    break;
            }
        }

        private void mAnnotButton_Click(object sender, RoutedEventArgs e)
        {
            if (!m_pdf.m_doc.CanSave)
            {
                Windows.UI.Popups.MessageDialog msgDlg = new Windows.UI.Popups.MessageDialog("The PDF File is not writable, maybe opened by other App.") { Title = "Readonly" };
                msgDlg.Commands.Add(new Windows.UI.Popups.UICommand("OK", uiCommand => { }));
                msgDlg.ShowAsync();
                return;
            }
            MenuFlyoutItem item = sender as MenuFlyoutItem;
            int tag = Convert.ToInt32(item.Tag);
            m_annot_status = tag;
            switch (m_annot_status)
            {
                case 0:
                    m_reader.PDFInkStart();
                    break;
                case 1:
                    m_reader.PDFNoteStart();
                    break;
                case 2:
                    m_reader.PDFLineStart();
                    break;
                case 3:
                    m_reader.PDFRectStart();
                    break;
                case 4:
                    m_reader.PDFEllipseStart();
                    break;
                case 5:
                    m_reader.PDFStampStart();
                    break;
                case 6:
                    m_reader.PDFEditTextStart();
                    break;
                case 7:
                    m_reader.PDFPolygonStart();
                    break;
                case 8:
                    m_reader.PDFPolylineStart();
                    break;
                default:
                    break;
            }
            enter_annot();
        }

        private void mFieldButton_Click(object sender, RoutedEventArgs e)
        {
            if (!m_pdf.m_doc.CanSave)
            {
                Windows.UI.Popups.MessageDialog msgDlg = new Windows.UI.Popups.MessageDialog("The PDF File is not writable, maybe opened by other App.") { Title = "Readonly" };
                msgDlg.Commands.Add(new Windows.UI.Popups.UICommand("OK", uiCommand => { }));
                msgDlg.ShowAsync();
                return;
            }
            MenuFlyoutItem item = sender as MenuFlyoutItem;
            int tag = Convert.ToInt32(item.Tag);
            m_annot_status = tag;
            switch (m_annot_status)
            {
                case 20://check
                    m_reader.PDFFieldCheckStart();
                    break;
                case 21://radio
                    m_reader.PDFFieldRadioStart();
                    break;
                case 22://signature
                    m_reader.PDFFieldSignStart();
                    break;
                case 23:
                    m_reader.PDFFieldEditTextStart();
                    break;
                default:
                    break;
            }
            enter_annot();
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

        private void StartFTSSearch(string query, bool newQuery)
        {
            if (newQuery)
            {
                if (query.Length < RadaeeFTSManager.FtsQueryMinLength)
                { //FTS search type, word to search too short
                    _ = new MessageDialog("Enter at least " + RadaeeFTSManager.FtsQueryMinLength + " characters to start searching", "Warning").ShowAsync();
                    return;
                }
                FTSResult = RadaeeFTSManager.Search(m_pdf.m_doc, query);
                if (FTSResult == null && !string.IsNullOrEmpty(RadaeeFTSManager.SearchError))
                { //switch to normal search
                    CommonUtil.ShowToastNotification("Warning", RadaeeFTSManager.SearchError, 1);
                    m_reader.PDFFindStart(query, false, false);
                    m_reader.PDFFind(1);
                }
                else if (FTSResult == null || FTSResult.Count == 0)
                {
                    _ = new MessageDialog("No search results found for (" + query + ")", "Warning").ShowAsync();
                }
                else
                {
                    splitView.IsPaneOpen = true;
                    SearchNextBtn.IsEnabled = true;
                    SearchPrevBtn.IsEnabled = true;
                    SearchPivotItem.IsEnabled = true;
                    SearchNextBtn.Focus(FocusState.Programmatic);
                    FTSSearchResultList.ItemsSource = FTSResult;
                    FTSSearchResultList.SelectedItem = FTSResult[0];
                    int index = FTSResult.IndexOf(CurrentSearchResult);
                    if (index == 0)
                        SearchPrevBtn.IsEnabled = false;
                    if (index == FTSResult.Count - 1)
                        SearchNextBtn.IsEnabled = false;
                }
            }
            else if (RadaeeFTSManager.FTSDocEnabled)
                splitView.IsPaneOpen = true;
            else
                m_reader.PDFFind(1);
        }

        private void CloseSearch()
        {
            if (mSearchPopup != null)
            {
                mSearchPopup.dismiss();
                mSearchPopup = null;
            }

            if (FTSResult != null) FTSResult.Clear();
            SearchPivotItem.IsEnabled = false;
            splitView.IsPaneOpen = false;
            FTSResult = null;
            FTSSearchResultList.ItemsSource = null;
            m_reader.PDFSetFTSRect(null, -1);
            if (!RadaeeFTSManager.FTSDocEnabled)
                m_reader.PDFFindEnd();
        }

        private async void FTSSearchResultList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CurrentSearchResult = FTSSearchResultList.SelectedItem as FTS;
            if (CurrentSearchResult != null)
            {
                int pageIndex = CurrentSearchResult.PageIndex;
                if (pageIndex >= 0 && pageIndex < m_pdf.m_doc.PageCount)
                {
                    if (m_reader.PDFGetCurPageNo() != pageIndex)
                    {
                        m_reader.PDFSetFTSRect(null, -1);
                        m_reader.PDFGotoPage(pageIndex);
                        await Task.Delay(100);
                    }
                    RDRect ftsRect;
                    ftsRect.top = (float)CurrentSearchResult.RectTop;
                    ftsRect.left = (float)CurrentSearchResult.RectLeft;
                    ftsRect.right = (float)CurrentSearchResult.RectRight;
                    ftsRect.bottom = (float)CurrentSearchResult.RectBottom;
                    m_reader.PDFSetFTSRect(ftsRect, pageIndex);
                }
            }
        }

        private void OnSearchBtnClick(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            string name = button.Name;
            if (name.Equals(CloseSearchBtn.Name))
                CloseSearch();
            else if (name.Equals(SearchPrevBtn.Name))
                NavigateOnSearchResults(false);
            else if (name.Equals(SearchNextBtn.Name))
                NavigateOnSearchResults(true);
        }

        private void NavigateOnSearchResults(bool next)
        {
            SearchNextBtn.IsEnabled = true;
            SearchPrevBtn.IsEnabled = true;
            int index = FTSResult.IndexOf(CurrentSearchResult);
            if (!next && index > 0)
                index--;
            if (next && index < FTSResult.Count - 1)
                index++;
            FTSSearchResultList.SelectedIndex = index;

            if (index == 0)
                SearchPrevBtn.IsEnabled = false;
            if (index == FTSResult.Count - 1)
                SearchNextBtn.IsEnabled = false;
            FTSSearchResultList.ScrollIntoView(CurrentSearchResult);
        }
    }
}
