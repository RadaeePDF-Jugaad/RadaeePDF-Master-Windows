using SQLitePCL;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;
using static RDPDFMaster.Modules.FTS.TextExtractor;

namespace RDPDFMaster.Modules.FTS
{
    class FTSJsonManager
    {
        private const string textKey = "text";
        private const string pageKey = "page";
        private const string blocksKey = "blocks";
        private const string rectTopKey = "rect_t";
        private const string rectLeftKey = "rect_l";
        private const string rectRightKey = "rect_r";
        private const string rectBottomKey = "rect_b";

        /// <summary>
        /// Returns the string reprsentation of the page blocks json
        /// </summary>
        /// <param name="pageBlocks">page blocks list to be converted to JSON</param>
        /// <returns>string reprsentation of the page blocks json</returns>
        internal static string ToPageBlocksJson(PageBlocks pageBlocks)
        { //convert blocks list into Json array
            JsonArray blocksArray = new JsonArray();
            foreach (Block textBlock in pageBlocks.Blocks)
            {
                JsonObject blockObject = new JsonObject
                {
                    [textKey] = JsonValue.CreateStringValue(textBlock.Text),
                    [rectTopKey] = JsonValue.CreateNumberValue(textBlock.RectTop),
                    [rectLeftKey] = JsonValue.CreateNumberValue(textBlock.RectLeft),
                    [rectRightKey] = JsonValue.CreateNumberValue(textBlock.RectRight),
                    [rectBottomKey] = JsonValue.CreateNumberValue(textBlock.RectBottom),
                };
                blocksArray.Add(blockObject);
            }

            //construct the page json object
            JsonObject pageObject = new JsonObject
            {
                [pageKey] = JsonValue.CreateNumberValue(pageBlocks.Page),
                [blocksKey] = blocksArray
            };

            return pageObject.Stringify();
        }

        internal static async Task ParseFTSJsonAsync(StorageFile ftsJsonFile, string docId)
        {
            try
            {
                string ftsContent = await FileIO.ReadTextAsync(ftsJsonFile);
                if (string.IsNullOrEmpty(ftsContent))
                    return;
                ftsContent = ftsContent.Replace("}{", "},{").Replace("},]", "}]");

                JsonObject ftsJson = JsonObject.Parse(ftsContent);
                JsonArray pages = ftsJson.GetNamedArray("pages");
                try
                {
                    using (SQLiteConnection connection = new SQLiteConnection(FTSTable.DBName))
                    {
                        using (var statement = connection.Prepare("BEGIN;"))
                        {
                            statement.Step();
                        }
                        for (uint i = 0; i < pages.Count; i++)
                        {
                            JsonObject current = null;
                            try
                            { //workaround to get over the null entry in the json array
                                current = pages.GetObjectAt(i);
                            }
                            catch (Exception e)
                            {
                                Debug.WriteLine(e.Message + e.StackTrace);
                            }
                            if (current != null)
                            {
                                int pageIndex = Convert.ToInt32(current.GetNamedNumber(pageKey));
                                JsonArray blocks = current.GetNamedArray(blocksKey);
                                for (uint j = 0; j < blocks.Count; j++)
                                {
                                    JsonObject block = blocks.GetObjectAt(j);
                                    FTS fts = new FTS
                                    {
                                        Document = docId,
                                        PageIndex = pageIndex,
                                        Text = block.GetNamedString(textKey),
                                        RectTop = block.GetNamedNumber(rectTopKey),
                                        RectLeft = block.GetNamedNumber(rectLeftKey),
                                        RectRight = block.GetNamedNumber(rectRightKey),
                                        RectBottom = block.GetNamedNumber(rectBottomKey)
                                    };
                                    FTSTable.Insert(connection, fts);
                                }
                            }
                        }
                        using (var statement = connection.Prepare("COMMIT;"))
                        { //  commit of pdf-specific FTS transaction
                            statement.Step();
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.StackTrace);
                }
                await ftsJsonFile.DeleteAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message + ex.StackTrace);
            }
        }
    }
}
