using System;
using System.Data;
using System.Data.SqlClient;

namespace vanet_function_GC.Utilities
{
    public static class DbConnection
    {
        public static DataRowCollection QueryDatabase(string queryText)
        {
            var str = Environment.GetEnvironmentVariable("sqldb_connection");
            var ds = new DataSet();
            using (SqlConnection conn = new SqlConnection(str))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(queryText, conn))
                {
                    using (var adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(ds);
                        if(ds.Tables.Count == 0) return null;
                        return ds.Tables[0].Rows;
                    }
                }

            }

        }
    }
}
