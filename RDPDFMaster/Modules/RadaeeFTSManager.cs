using RDDLib.pdf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace RDPDFMaster.Modules.FTS
{
    public class RadaeeFTSManager
    {
        public const int FtsQueryMinLength = 3;
        //the current search type, 0: standard search, 1: FTS search
        public static int SearchType { get; set; }
        //used to check if document is enabled for FTS (already added to the FTS table)
        public static bool FTSDocEnabled { get; set; }
        public static string SearchError { get; set; }

        /// <summary>
        /// Initializes the FTS search mode, creates the FTS DB and table if needed.
        /// </summary>
        public static void Init()
        {
            SearchType = 1;
            FTSTable.CreateTable();
        }

        /// <summary>
        /// Extracts the text from the given document and add it to the FTS DB table.
        /// </summary>
        /// <param name="doc">The document opened object</param>
        /// <param name="selRtoL">if true text selection starts from right to left</param>
        /// <returns>error message in case of error</returns>
        public static async Task<string> AddIndexAsync(PDFDoc doc, bool selRtoL)
        {
            try
            { //first check if the document already added to the FTS table
                string docId = GetDocumentID(doc);
                if (FTSTable.DoesFTSDocumentExist(docId))
                    return "Warning: Document already added, please call FTS_RemoveFromIndex first";

                //extract text into file
                StorageFile ftsJsonFile = await ApplicationData.Current.LocalCacheFolder.CreateFileAsync("fts.json",
                    CreationCollisionOption.ReplaceExisting);
                int extractResult = await TextExtractor.ExtractDocumentTextAsync(doc, ftsJsonFile);
                if (extractResult == 1) //Text extracted successfully
                    await FTSJsonManager.ParseFTSJsonAsync(ftsJsonFile, docId);
                else //text extraction error
                    return extractResult == -1 ? "Extract text error" : "No text to extract";
                return "SUCCESS: Index added successfully";
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message + ex.StackTrace);
            }
            return "ERROR";
        }

        /// <summary>
        /// Removes the given document from the FTS DB table.
        /// </summary>
        /// <param name="doc">The document opened object</param>
        /// <returns>error message in case of error</returns>
        public static string RemoveFromIndex(PDFDoc doc)
        {
            try
            {
                string docId = GetDocumentID(doc);
                if (!FTSTable.DoesFTSDocumentExist(docId))
                    return "Error:Document is not added into Index";
                FTSTable.Delete(docId);
                return "Index removed successfully";
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message + ex.StackTrace);
            }
            return "ERROR";
        }

        public static List<FTS> Search(PDFDoc document, string query)
        {
            try
            {
                SearchError = string.Empty;
                FTSDocEnabled = false; //will be true after document validation

                string docId = GetDocumentID(document);
                if (!FTSTable.DoesFTSDocumentExist(docId))
                { //Document is not yet added into Index
                    SearchError = "Error:Document is not yet added into Index, try again later";
                    return null;
                }

                FTSDocEnabled = true;
                return FTSTable.SearchInDocument(docId, query.Trim());
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message + ex.StackTrace);
            }
            return null;
        }

        private static String GetDocumentID(PDFDoc document)
        {
            try
            {
                string docId = document.GetMeta("UUID");
                if (string.IsNullOrEmpty(docId))
                { //generate an id for the document and set it as meta data
                    docId = Md5(Guid.NewGuid().ToString());
                    document.SetMeta("UUID", docId);
                    document.Save();
                }
                return docId;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message + ex.StackTrace);
            }
            return string.Empty;
        }

        private static string Md5(string input)
        {
            try
            {
                using (MD5 md5Hash = MD5.Create())
                {
                    return MD5CryptString(md5Hash, input);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.StackTrace);
            }
            return null;
        }

        private static string MD5CryptString(MD5 md5Hash, string input)
        {
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            StringBuilder sBuilder = new StringBuilder();

            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            return sBuilder.ToString();
        }
    }
}
