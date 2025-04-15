namespace RDPDFMaster.Modules.FTS
{
    public class FTS
    {
        public int PageIndex { get; set; }
        public int PageNumber { get; set; }
        public int Occurrences { get; set; }

        public double RectTop { get; set; }
        public double RectLeft { get; set; }
        public double RectRight { get; set; }
        public double RectBottom { get; set; }

        public string Text { get; set; }
        public string Snippet { get; set; }
        public string Document { get; set; }
    }
}
