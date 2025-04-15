using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace com.radaee.master
{
    class PDFDialog : ContentDialog
    {
        public PDFDialog(FrameworkElement content, TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs> onOk, TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs> onCancel) : base()
        {
            Content = content;
            PrimaryButtonText = "OK";
            PrimaryButtonClick += onOk;
            SecondaryButtonText = "Cancel";
            SecondaryButtonClick += onCancel;
            Background = new SolidColorBrush(Color.FromArgb(255, 224, 224, 224));
        }
    }
}
