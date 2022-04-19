using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Text;

namespace ShadowProject
{
    public partial class ShadowProjectProccessor
    {
        private static SQLiteConnection DB__Open(string path)
        {
            if (!File.Exists(path))
            {
                SQLiteConnection.CreateFile(path);
            }

            var conn = new SQLiteConnection($"Data Source={path};Version=3;");
            conn.Open();

            return conn;
        }

        private static SQLiteConnection DateDB__Open(string table, string path)
        {
            string SQ_CREATE = $"CREATE TABLE {table} (path TEXT PRIMARY KEY, time INTEGER)";

            var connection = DB__Open(path);

            SQLiteCommand command = new SQLiteCommand(SQ_CREATE, connection);
            int result = command.ExecuteNonQuery();

            return connection;
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
            SQLiteDataReader rdr = cmd.ExecuteReader();
            rdr.Read();
            DateTime old = new DateTime((long)rdr["time"]);
            rdr.Close();

            return old == time;
        }

        SQLiteConnection m_sql;
        const string SQLDB_NAME = "mixed.db";
        const string SQL_TABLE__CREATEDTIME = "CREATED_TIME";
        const string SQL_TABLE__LASTACCESSEDTIME = "LASTACCESSED_TIME";
        const string SQL_TABLE__LASTMODIFIEDTIME = "LASTMODIFIED_TIME";

        private void OpenDB()
        {
            if (m_sql != null) return;
            m_sql = DateDB__Open(SQLDB_NAME,SQL_TABLE__CREATEDTIME);
            m_sql = DateDB__Open(SQLDB_NAME, SQL_TABLE__LASTACCESSEDTIME);
            m_sql = DateDB__Open(SQLDB_NAME, SQL_TABLE__LASTMODIFIEDTIME);
        }

        private void CloseDB()
        {
            m_sql.Close();
            m_sql = null;
        }
    }
}
