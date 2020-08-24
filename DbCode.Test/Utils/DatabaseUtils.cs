using Microsoft.Data.SqlClient;
using System.Linq;
using System;

namespace DbCode.Test.Utils
{

    public static class DatabaseUtils
    {
        const int expectedVersion = 2;

        static object setupObj = new object();
        public static void SetupDb(string dataPath, string constr)
        {
            lock (setupObj)
            {
                if (System.IO.File.Exists(System.IO.Path.Combine(dataPath, "CodeGenTestDb.mdf")))
                {
                    string testCommand = "SELECT VersionNr FrOM  dbo.TestDbVersion";
                    int version;
                    try
                    {
                        using (SqlConnection connection = new SqlConnection(constr))
                        {
                            connection.Open();
                            var cmd = connection.CreateCommand();
                            cmd.CommandType = System.Data.CommandType.Text;
                            cmd.CommandText = testCommand;
                            using (var rdr = cmd.ExecuteReader())
                            {
                                rdr.Read();
                                version = rdr.GetInt32(0);
                            }
                        }
                    }
                    catch (SqlException)
                    {
                        version = 0;// Do reset it
                    }

                    if (version < expectedVersion)
                    {
                        using (SqlConnection connection = new SqlConnection(@"server=(localdb)\MSSQLLocalDB"))
                        {
                            connection.Open();
                            var cmdDropCon = new SqlCommand("ALTER DATABASE [CodeGenTestDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE", connection);
                            cmdDropCon.ExecuteNonQuery();
                            SqlCommand command = new SqlCommand("DROP DATABASE CodeGenTestDb", connection);
                            command.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        return; // Everything ok
                    }
                }


                using (SqlConnection connection = new SqlConnection(@"server=(localdb)\MSSQLLocalDB"))
                {
                    connection.Open();

                    string sql = string.Format(@"
        CREATE DATABASE
            [CodeGenTestDb]
        ON PRIMARY (
           NAME=CodeGenTestDb,
           FILENAME = '{0}\CodeGenTestDb.mdf'
        )
        LOG ON (
            NAME = CodeGenTestDb_log,
            FILENAME = '{0}\CodeGenTestDb.ldf'
        )",
                        dataPath
                    );

                    SqlCommand command = new SqlCommand(sql, connection);
                    command.ExecuteNonQuery();



                }

                var sqls = System.IO.File.ReadAllText(System.IO.Path.Combine(dataPath, "sqlscript.sql"))
                    .Replace("{{DbVersion}}", expectedVersion.ToString())
                    .Replace("\r\n", "\n")
                    .Replace("\r", "\n")
                    .Split(new string[] { "\nGO\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Replace("\n", Environment.NewLine));
                using (SqlConnection connection = new SqlConnection(constr))
                {
                    connection.Open();
                    foreach (var sql in sqls)
                    {
                        SqlCommand datacommand = new SqlCommand(sql, connection);
                        datacommand.ExecuteNonQuery();
                    }
                }
            }
        }


    }
}
