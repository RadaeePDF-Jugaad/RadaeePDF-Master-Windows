using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RDPDFMaster.Modules.FTS
{
    class FTSTable
    {
        public static readonly string DBName = "fts.db";
        private static readonly string Snippet = "snippet";
        private static readonly string TextType = " TEXT";
        private static readonly string IntType = " INTEGER";
        private static readonly string DoubleType = " DOUBLE";
        private static readonly string CommaSep = ",";
        private static readonly string CreateFtsTable =
                "CREATE VIRTUAL TABLE " + FTSEntry.TableName + " USING fts4 (" +
                        FTSEntry.DocName + TextType + CommaSep +
                        FTSEntry.PageIndex + IntType + CommaSep +
                        FTSEntry.RectLeft + DoubleType + CommaSep +
                        FTSEntry.RectTop + DoubleType + CommaSep +
                        FTSEntry.RectRight + DoubleType + CommaSep +
                        FTSEntry.RectBottom + DoubleType + CommaSep +
                        FTSEntry.Text + TextType + ")";

        public static void CreateTable()
        {
            using (SQLiteConnection db = new SQLiteConnection(DBName))
            {
                string querySql = "SELECT DISTINCT tbl_name FROM sqlite_master WHERE tbl_name = '" + FTSEntry.TableName + "'";
                using (var statement = db.Prepare(querySql))
                {
                    if (statement.Step() != SQLiteResult.ROW)
                    {
                        using (var createStatement = db.Prepare(CreateFtsTable))
                        {
                            createStatement.Step();
                        }
                    }
                }
            }
        }

        public static void Insert(SQLiteConnection db, FTS fts)
        {
            string insertSql = "INSERT INTO " + FTSEntry.TableName + " (" + FTSEntry.Text + CommaSep + FTSEntry.DocName + CommaSep + FTSEntry.RectTop + CommaSep
                + FTSEntry.RectLeft + CommaSep + FTSEntry.RectRight + CommaSep + FTSEntry.RectBottom + CommaSep + FTSEntry.PageIndex
                + ") VALUES(? , ?, ?, ?, ?, ?, ?);";
            using (var statement = db.Prepare(insertSql))
            {
                statement.Bind(1, fts.Text);
                statement.Bind(2, fts.Document);
                statement.Bind(3, fts.RectTop);
                statement.Bind(4, fts.RectLeft);
                statement.Bind(5, fts.RectRight);
                statement.Bind(6, fts.RectBottom);
                statement.Bind(7, fts.PageIndex);
                statement.Step();
            }
        }

        public static void Delete(string document)
        {
            string deleteSql = "DELETE FROM " + FTSEntry.TableName + " WHERE " + FTSEntry.DocName + " MATCH '" + document + "';";
            using (SQLiteConnection db = new SQLiteConnection(DBName))
            {
                using (var statement = db.Prepare(deleteSql))
                {
                    statement.Step();
                }
            }
        }

        public static bool DoesFTSDocumentExist(string document)
        {
            bool result = false;
            string querySql = "SELECT COUNT(*) FROM " + FTSEntry.TableName + " WHERE " + FTSEntry.DocName + " MATCH '" + document + "';";
            using (SQLiteConnection db = new SQLiteConnection(DBName))
            {
                using (var statement = db.Prepare(querySql))
                {
                    if (statement.Step() == SQLiteResult.ROW)
                    {
                        long count = statement.GetInteger("COUNT(*)");
                        if (count > 0)
                            result = true;
                    }
                }
            }
            return result;
        }

        public static List<FTS> SearchInDocument(string document, string query)
        {
            List<FTS> result = null;
            string querySql = "SELECT *, snippet(" + FTSEntry.TableName + ", '<b>', '</b>', '...', 6, 10) as " + Snippet + " FROM "
                + FTSEntry.TableName + " WHERE " + FTSEntry.DocName + " = ? AND " + FTSEntry.Text + " MATCH ? ORDER BY " + FTSEntry.PageIndex + CommaSep +
                FTSEntry.RectTop + " DESC, " + FTSEntry.RectLeft;

            query = string.Join("* ", query.Split(" ".ToCharArray()));
            try
            {
                using (SQLiteConnection db = new SQLiteConnection(DBName))
                {
                    using (var statement = db.Prepare(querySql))
                    {
                        statement.Bind(1, document);
                        statement.Bind(2, query + "*");
                        result = new List<FTS>();
                        while (statement.Step() == SQLiteResult.ROW)
                        {
                            result.Add(ReadFromStatement(statement));
                        }

                    }
                }
                return result;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message + e.StackTrace);
                return null;
            }
        }

        public static List<FTS> SearchAllDocuments(string query)
        {
            List<FTS> result = null;

            query = string.Join("* ", query.Split(" ".ToCharArray()));
            string selection = FTSEntry.Text + " MATCH ?";
            string orderBy = FTSEntry.DocName + CommaSep + FTSEntry.PageIndex + CommaSep + FTSEntry.RectTop + " DESC, " + FTSEntry.RectLeft;
            string projectionFTS = FTSEntry.DocName + CommaSep + FTSEntry.Text + CommaSep + FTSEntry.PageIndex + CommaSep + FTSEntry.RectTop + CommaSep + FTSEntry.RectLeft +
                CommaSep + FTSEntry.RectRight + CommaSep + FTSEntry.RectBottom + ", snippet(" + FTSEntry.TableName + ", '<b>', '</b>', '...', 6, 10) as " + Snippet;
            string projectionGroup = FTSEntry.DocName + CommaSep + FTSEntry.Text + CommaSep + ", MIN(" + FTSEntry.PageIndex + ") AS first_page, COUNT(" + FTSEntry.PageIndex +
                ") AS Occurrences, " + FTSEntry.RectTop + CommaSep + FTSEntry.RectLeft + CommaSep + FTSEntry.RectRight + CommaSep + FTSEntry.RectBottom + ", " + Snippet;
            string fullQuery = "SELECT " + projectionGroup + " FROM (SELECT " + projectionFTS + " FROM " + FTSEntry.TableName + " WHERE " + selection + " ORDER BY " + orderBy +
                ") GROUP BY " + FTSEntry.DocName;

            using (SQLiteConnection db = new SQLiteConnection(DBName))
            {
                using (var statement = db.Prepare(fullQuery))
                {
                    statement.Bind(1, query + "*");
                    result = new List<FTS>();
                    while (statement.Step() == SQLiteResult.ROW)
                    {
                        FTS fts = ReadFromStatement(statement);
                        fts.PageIndex = Convert.ToInt32(statement.GetInteger(statement.ColumnIndex("first_page")));
                        fts.Occurrences = Convert.ToInt32(statement.GetInteger(statement.ColumnIndex("Occurrences")));

                        result.Add(fts);
                    }
                }
            }

            return result;
        }

        private static FTS ReadFromStatement(ISQLiteStatement statement)
        {
            FTS fts = new FTS();

            try
            { //to avoid (Unable to find column with the specified name)
                fts.Snippet = statement.GetText(statement.ColumnIndex(Snippet));
            }
            catch (Exception) { }
            try
            { //to avoid (Unable to find column with the specified name)
                fts.PageIndex = Convert.ToInt32(statement.GetInteger(statement.ColumnIndex(FTSEntry.PageIndex)));
                fts.PageNumber = fts.PageIndex + 1;
            }
            catch (Exception) { }

            fts.Text = statement.GetText(statement.ColumnIndex(FTSEntry.Text));
            fts.RectTop = statement.GetFloat(statement.ColumnIndex(FTSEntry.RectTop));
            fts.Document = statement.GetText(statement.ColumnIndex(FTSEntry.DocName));
            fts.RectLeft = statement.GetFloat(statement.ColumnIndex(FTSEntry.RectLeft));
            fts.RectRight = statement.GetFloat(statement.ColumnIndex(FTSEntry.RectRight));
            fts.RectBottom = statement.GetFloat(statement.ColumnIndex(FTSEntry.RectBottom));

            return fts;
        }

        private static class FTSEntry
        {
            public static string Id = "_id";
            public static string TableName = "FTS";
            public static string DocName = "document";
            public static string PageIndex = "page";
            public static string RectLeft = "rect_left";
            public static string RectTop = "rect_top";
            public static string RectRight = "rect_right";
            public static string RectBottom = "rect_bottom";
            public static string Text = "text";
        }
    }
}
