using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Text;

namespace ShadowProject
{
    public partial class ShadowProjectProccessor
    {


        private static void DateDB__CreateOrOpenTable(SQLiteConnection connection, string table)
        {
            //존재 검사 구현해야함
            string SQ_CREATE = $"CREATE TABLE IF NOT EXISTS {table} (path TEXT PRIMARY KEY, time INTEGER)";
            SQLiteCommand command = new SQLiteCommand(SQ_CREATE, connection);
            int result = command.ExecuteNonQuery();
        }

        private static void DateDB__UpdateOrInsert(SQLiteConnection connection, string table, string path, DateTime time)
        {
            string SQ_INSERT = $"INSERT OR REPLACE INTO {table} (path, time) values ('{path}',{time.Ticks})";

            SQLiteCommand command = new SQLiteCommand(SQ_INSERT, connection);
            int result = command.ExecuteNonQuery();
        }

        public static bool DateDB__CompareDate(SQLiteConnection connection, string table, string path, DateTime time)
        {
            string SQ_WHERE = $"SELECT * FROM {table} WHERE path = '{path}'";

            SQLiteCommand cmd = new SQLiteCommand(SQ_WHERE, connection);
            bool result = false;

            using (SQLiteDataReader rdr = cmd.ExecuteReader())
            {
                DateTime old = default;
                if (rdr.Read())
                {
                    old = new DateTime((long)rdr["time"]);
                    result = old == time;
                    goto end;
                }
            }

        end:
            DateDB__UpdateOrInsert(connection, table, path, time);
            return result;
        }

        const string SQL_TABLE__CREATEDTIME = "CREATED_TIME";
        const string SQL_TABLE__LASTACCESSEDTIME = "LASTACCESSED_TIME";
        const string SQL_TABLE__LASTMODIFIEDTIME = "LASTMODIFIED_TIME";

        private void PrepareDB()
        {
            DateDB__CreateOrOpenTable(m_db, SQL_TABLE__CREATEDTIME);
            DateDB__CreateOrOpenTable(m_db, SQL_TABLE__LASTACCESSEDTIME);
            DateDB__CreateOrOpenTable(m_db, SQL_TABLE__LASTMODIFIEDTIME);
        }
    }
}
