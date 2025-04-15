using System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace RDPDFReader.annotui
{
    public delegate void FTSClickCallback(bool activate);

    public sealed partial class PopSearch : UserControl
    {
        public FTSClickCallback mFTSClicked;

        public PopSearch()
        {
            this.InitializeComponent();
            mPanel.Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 160));
            mNext.Tag = 1;
            mPrev.Tag = -1;
        }
        public void init(RoutedEventHandler callback)
        {
            mNext.Click += callback;
            mPrev.Click += callback;
        }
        public bool IsCase()
        {
            return mCase.IsChecked.Value;
        }
        public bool IsWhole()
        {
            return mWord.IsChecked.Value;
        }
        public String getKey()
        {
            return mKey.Text;
        }

        public void SwitchFTSMode(bool activate, RoutedEventHandler searchEventHandler, RoutedEventHandler closeEventHandler)
        {
            Visibility ftsVisibility = activate ? Visibility.Visible : Visibility.Collapsed;
            Visibility normalVisibility = activate ? Visibility.Collapsed : Visibility.Visible;
            if (activate) {
                Close.Click += closeEventHandler;
                Search.Click += searchEventHandler;
            }
            else
            {
                Close.Click -= closeEventHandler;
                Search.Click -= searchEventHandler;
            }

            Close.Visibility = ftsVisibility;
            Search.Visibility = ftsVisibility;

            mNext.Visibility = normalVisibility;
            mPrev.Visibility = normalVisibility;
            mCase.Visibility = normalVisibility;
            mWord.Visibility = normalVisibility;
            mKey.Focus(FocusState.Programmatic);
        }

        private void mFTS_Click(object sender, RoutedEventArgs e)
        {
            mFTSClicked.Invoke(mFTS.IsChecked.Value);
        }

        public void EnableFTSMode() {
            mFTS.IsEnabled = true;
        }

        //public void ActivateFTSMode(RoutedEventHandler searchEventHandler, RoutedEventHandler closeEventHandler)
        //{
        //    Close.Click += closeEventHandler;
        //    Search.Click += searchEventHandler;
        //    Close.Visibility = Visibility.Visible;
        //    Search.Visibility = Visibility.Visible;
        //    mNext.Visibility = Visibility.Collapsed;
        //    mPrev.Visibility = Visibility.Collapsed;
        //    mCase.Visibility = Visibility.Collapsed;
        //    mWord.Visibility = Visibility.Collapsed;
        //    mKey.Focus(FocusState.Programmatic);
        //}
    }
}
