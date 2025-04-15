using RDDLib.comm;
using RDDLib.pdf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage;

namespace RDPDFMaster.Modules.FTS
{
    class TextExtractor
    {
        private static double HorzGap;
        private static double VertGap;
        private static RDRect BlockRect;
        private static RDRect CurCharRect;
        private static RDRect NextCharRect;
        private static double FontHeightDiff;

        /// <summary>
        /// Extracts all the text from a pdf and puts it into a json file.
        /// </summary>
        /// <param name="document">the input document</param>
        /// <param name="outputPath">FTS json file path</param>
        /// <returns>0: no text, 1:success, -1:failure</returns>
        internal static async Task<int> ExtractDocumentTextAsync(PDFDoc document, StorageFile ftsJsonFile)
        {
            try
            {
                bool firstEntry = true;
                bool firstPageEntry = true;
                int pageCount = document.PageCount;
                for (int i = 0; i < pageCount; i++)
                {
                    PDFPage page = document.GetPage(i);
                    if (page != null)
                    {
                        PageBlocks pageBlocks = ExtractPageText(page, i);
                        if (pageBlocks != null)
                        {
                            if (firstEntry)
                            { //start the pages json array ({"pages":[)
                                await FileIO.WriteTextAsync(ftsJsonFile, "{\"pages\":[");
                                firstEntry = false;
                            }
                            string pageBlocksJson = FTSJsonManager.ToPageBlocksJson(pageBlocks);
                            if (!string.IsNullOrEmpty(pageBlocksJson)) //write the page blocks to the json file
                            {
                                await FileIO.AppendTextAsync(ftsJsonFile, firstPageEntry ? pageBlocksJson : "," + pageBlocksJson);
                                if (firstPageEntry)
                                    firstPageEntry = false;
                            }
                        }
                        page.Close();
                    }
                }

                if (!firstEntry)
                { //close the pages json array
                    await FileIO.AppendTextAsync(ftsJsonFile, "]}");
                    return 1;
                }
                return 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message + ex.StackTrace);
            }
            return -1;
        }

        /// <summary>
        /// Extracts text from pages, divided into blocks
        /// </summary>
        /// <param name="page">current page object</param>
        /// <param name="pageIndex">page index in the document</param>
        /// <returns>page's text into blocks</returns>
        private static PageBlocks ExtractPageText(PDFPage page, int pageIndex)
        {
            try
            {
                page.ObjsStart();
                int blockStartIndex = 0;

                BlockRect = page.ObjsGetCharRect(0);

                List<Block> blocksArray = null;
                int charCount = page.ObjsGetCharCount();
                for (int charIndex = 0; charIndex < charCount; charIndex++)
                {
                    CurCharRect = page.ObjsGetCharRect(charIndex); //get char's box in PDF coordinate system

                    if (charIndex < charCount - 1)
                        NextCharRect = page.ObjsGetCharRect(charIndex + 1); //get next char's box in PDF coordinate system

                    BlockRect = AdjustBlockRect(CurCharRect, BlockRect);

                    bool nextCharOutOfBlock = IsNextOutOfBlock();

                    if (StartNewTextBlock() || nextCharOutOfBlock || charIndex >= charCount - 1)
                    {
                        if (nextCharOutOfBlock || charIndex >= charCount - 1)
                            charIndex++;
                        String text = blockStartIndex == charIndex ? null : page.ObjsGetString(blockStartIndex, charIndex);
                        if (text != null && text.Length > 0)
                            text = text.Trim();
                        if (text != null && text.Length > 0)
                        { //add to json
                            text = HandleUtf16Chars(text.Trim());
                            text = HandleSpecialChars(text);
                            Block block = CreateTextBlock(text);
                            if (block != null)
                            {
                                if (blocksArray == null) blocksArray = new List<Block>();
                                blocksArray.Add(block);
                            }
                        }
                        int newIndex = nextCharOutOfBlock ? charIndex : charIndex + 1;
                        if (newIndex >= charCount)
                            break;
                        BlockRect = page.ObjsGetCharRect(nextCharOutOfBlock ? charIndex : charIndex + 1); //reset block rect with next char's rect
                        blockStartIndex = nextCharOutOfBlock ? charIndex : charIndex + 1;
                    }
                }

                PageBlocks pageBlocks = null;
                if (blocksArray != null)
                {
                    pageBlocks = new PageBlocks
                    {
                        Page = pageIndex,
                        Blocks = blocksArray
                    };
                }

                return pageBlocks;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message + ex.StackTrace);
            }
            return null;
        }

        /// <summary>
        /// Adds the passed character rectangle to the whole paragraph rectangle.
        /// </summary>
        /// <param name="charRect">current character rectangle</param>
        /// <param name="blockRect">the block rectangle.</param>
        /// <returns>the modified paragraph rectangle.</returns>
        private static RDRect AdjustBlockRect(RDRect charRect, RDRect blockRect)
        {
            try
            {
                if (blockRect.left > charRect.left)
                    blockRect.left = charRect.left;
                if (blockRect.top > charRect.top)
                    blockRect.top = charRect.top;
                if (blockRect.right < charRect.right)
                    blockRect.right = charRect.right;
                if (blockRect.bottom < charRect.bottom)
                    blockRect.bottom = charRect.bottom;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message + ex.StackTrace);
            }

            return blockRect;
        }

        /// <summary>
        /// Detect the case of next char is actually out of the range of current block
        /// </summary>
        /// <returns>true if the next character is out of current block, false otherwise</returns>
        private static bool IsNextOutOfBlock()
        {
            bool sameLine = Math.Abs(NextCharRect.top - BlockRect.top) < 1.5 && Math.Abs(NextCharRect.bottom - BlockRect.bottom) < 1.5;
            float gap = (NextCharRect.bottom - NextCharRect.top) / 2;
            return (sameLine && NextCharRect.left < BlockRect.left && NextCharRect.right < BlockRect.left) ||
                    (!sameLine && NextCharRect.left - BlockRect.right > gap && NextCharRect.right - BlockRect.right > gap);
        }

        /// <summary>
        /// Determines whether we need to start a new text block or not
        /// </summary>
        /// <returns>true if we need to save current block and start a new one, false otherwise</returns>
        private static bool StartNewTextBlock()
        {
            FontHeightDiff = Math.Abs((NextCharRect.bottom - NextCharRect.top) - (CurCharRect.bottom - CurCharRect.top));
            HorzGap = Math.Abs(NextCharRect.left - CurCharRect.right); //horizontal gap
            VertGap = NextCharRect.top - CurCharRect.bottom; //vertical gap

            bool sameLine = Math.Abs(CurCharRect.top - NextCharRect.top) < 1.5 && Math.Abs(CurCharRect.bottom - NextCharRect.bottom) < 1.5;
            bool sameColumn = Math.Abs(CurCharRect.left - NextCharRect.left) < 1.5 && Math.Abs(CurCharRect.right - NextCharRect.right) < 1.5;
            bool sameBlock = Math.Abs(BlockRect.left - NextCharRect.left) < 3 && Math.Abs(BlockRect.right - NextCharRect.right) > 0;

            if ((FontHeightDiff >= 2 && !sameColumn) || (sameLine && HorzGap >= 85) || (sameBlock && (VertGap <= -30 || VertGap >= 20))
                    || (!sameLine && !sameBlock && (VertGap >= 15 || VertGap <= -43 || HorzGap >= 800)))
            { //save current block and start a new one
              //Debug.WriteLine("font diff = " + sFontHeightDiff + " ... horz gap = " + sHorzGap + " ... vert gap = " + sVertGap);
                return true;
            }
            return false;
        }

        private static String HandleUtf16Chars(String input)
        {
            input = input.Replace("\u0092", "'");
            input = input.Replace("\u0095", "•");
            input = input.Replace("\u00B0", "°");
            input = input.Replace("\u0006", "[ACK]");
            input = input.Replace("\udddd", string.Empty);
            return input;
            //return new String(Charset.forName(StandardCharsets.UTF_8.name()).encode(input).array(), StandardCharsets.UTF_8);
        }

        private static String HandleSpecialChars(String input)
        {
            input = input.Replace("’", "'");
            input = input.Replace("‘", "'");
            input = input.Replace("“", "\"");
            input = input.Replace("”", "\"");
            input = input.Replace("\r\n", " ");
            //input = new String(input.getBytes(), StandardCharsets.UTF_8);
            return input;
        }

        private static Block CreateTextBlock(String text)
        {
            try
            {
                Block block = new Block
                {
                    Text = text,
                    RectTop = BlockRect.top, //top
                    RectLeft = BlockRect.left, //left
                    RectRight = BlockRect.right, //right
                    RectBottom = BlockRect.bottom //bottom
                };
                return block;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message + e.StackTrace);
            }
            return null;
        }

        public class PageBlocks
        {
            public int Page { get; set; }
            public List<Block> Blocks { get; set; }
        }

        public class Block
        {
            public string Text { get; set; }
            public double RectTop { get; set; }
            public double RectLeft { get; set; }
            public double RectRight { get; set; }
            public double RectBottom { get; set; }
        }
    }
}
