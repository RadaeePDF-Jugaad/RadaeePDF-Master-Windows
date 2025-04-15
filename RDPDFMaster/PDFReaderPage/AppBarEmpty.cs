using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace RDPDFReader
{
    class AppBarEmpty : FrameworkElement, ICommandBarElement
    {
        public AppBarEmpty() : base()
        {
        }
        public bool IsCompact { get; set; }
    }
}
