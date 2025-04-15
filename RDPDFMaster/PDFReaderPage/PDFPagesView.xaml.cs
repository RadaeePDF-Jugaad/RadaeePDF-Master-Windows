using RDDLib.pdf;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace RDPDFReader
{
    public delegate void OnPageConfirmHandler(int[] rotates, bool[] deletes);
    public delegate void OnPageClickedHandler(int pageNo);
    public sealed partial class PDFPagesView : UserControl
    {
        private PDFPageList m_view;
        private OnPageConfirmHandler m_page_confirm_callback;
        private OnPageClickedHandler m_page_click_callback;
        public PDFPagesView()
        {
            this.InitializeComponent();
            m_view = new PDFPageList();
        }
        public void PDFOpen(PDFDoc doc, OnPageConfirmHandler onPageConfirmCallback, OnPageClickedHandler onPageClickCallback)
        {
            m_view.PDFOpen(mView, doc);
            m_page_confirm_callback = onPageConfirmCallback;
            m_page_click_callback = onPageClickCallback;
            m_view.mPageClickCallback = onPageClicked;
            Visibility = Visibility.Visible;
        }
        private void mBack_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            m_view.PDFClose();
            Visibility = Visibility.Collapsed;
        }
        private void mConfirm_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            m_page_confirm_callback(m_view.PDFGetRotate(), m_view.PDFGetRemove());
            m_view.PDFClose();
            Visibility = Visibility.Collapsed;
        }

        private void onPageClicked(int pageNo) {
            m_page_click_callback(pageNo);
            m_view.PDFClose();
            Visibility = Visibility.Collapsed;
        }
    }
}
