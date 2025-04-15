using System.Text.RegularExpressions;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;

namespace RDPDFMaster.Modules.FTS
{
    public static class TextBlockHelper
    {
        public static string GetFormattedText(DependencyObject obj)
        {
            return (string)obj.GetValue(FormattedTextProperty);
        }

        public static void SetFormattedText(DependencyObject obj, string value)
        {
            obj.SetValue(FormattedTextProperty, value);
        }

        public static readonly DependencyProperty FormattedTextProperty =
            DependencyProperty.RegisterAttached("FormattedText", typeof(string), typeof(TextBlockHelper),
            new PropertyMetadata(string.Empty, FormattedTextChanged));

        private static void FormattedTextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            string value = e.NewValue as string;

            TextBlock textBlock = sender as TextBlock;

            if (textBlock != null)
            {
                textBlock.Inlines.Clear();

                Regex regx = new Regex(@"(<b>[^\s]+</b>)", RegexOptions.IgnoreCase);
                var str = regx.Split(value);
                for (int i = 0; i < str.Length; i++)
                    if (i % 2 == 0)
                        textBlock.Inlines.Add(new Run { Text = str[i] });
                    else
                    {
                        Span span = new Span();
                        span.FontWeight = FontWeights.Bold;
                        span.Inlines.Add(new Run { Text = str[i].Replace("<b>", string.Empty).Replace("</b>", string.Empty) });
                        textBlock.Inlines.Add(span);
                    }
            }
        }
    }
}
