using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Text;

namespace ShadowProject
{
    public partial class ShadowProjectProccessor
    {
        private SQLiteConnection DB__Open(string name, out bool created)
        {
            name = GetSDWPFilePath(name);
            created = false;
            if (!File.Exists(name))
            {
                created = true;
                SQLiteConnection.CreateFile(name);
            }

            var conn = new SQLiteConnection($"Data Source={name};Version=3;");
            conn.Open();

            return conn;
        }

        private static void DateDB__CreateOrOpenTable(SQLiteConnection connection, string table)
        {
            string SQ_CREATE = $"CREATE TABLE {table} (path TEXT PRIMARY KEY, time INTEGER)";
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

        SQLiteConnection m_sql;
        const string SQLDB_NAME = "mixed.sqlite";
        const string SQL_TABLE__CREATEDTIME = "CREATED_TIME";
        const string SQL_TABLE__LASTACCESSEDTIME = "LASTACCESSED_TIME";
        const string SQL_TABLE__LASTMODIFIEDTIME = "LASTMODIFIED_TIME";

        private void OpenDB()
        {
            bool created_db;
            if (m_sql != null) return;
            m_sql = DB__Open(NICKNAME + SQLDB_NAME, out created_db);

            if (created_db)
            {
                DateDB__CreateOrOpenTable(m_sql, SQL_TABLE__CREATEDTIME);
                DateDB__CreateOrOpenTable(m_sql, SQL_TABLE__LASTACCESSEDTIME);
                DateDB__CreateOrOpenTable(m_sql, SQL_TABLE__LASTMODIFIEDTIME);
            }
        }

        private void CloseDB()
        {
            if (m_sql == null) return;
            m_sql.CloseAsync().Wait();
            m_sql.DisposeAsync().AsTask().Wait();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            m_sql = null;
        }
    }
}
