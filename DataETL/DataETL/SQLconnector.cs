using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DataETL
{
    class SQLconnector
    {
        string connetionString = null;
        SqlConnection cnn;
        public void connect()
        {
            
            connetionString = "Data Source=ideam-upc.database.windows.net;Initial Catalog=ideam-upc;User ID=roland;Password=Cambiar123!";
            cnn = new SqlConnection(connetionString);
            try
            {
                cnn.Open();
                //table count
                List<string> tables = tableNames();
                foreach (string t in tables) selectAllInTable(t);

                cnn.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Can not open connection ! ");
            }
        }
        private void selectAllInTable(string table)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT * ");
            sb.Append("FROM ["+table+"]");
            String sql = sb.ToString();
            List<string> data = new List<string>();
            using (SqlCommand command = new SqlCommand(sql, cnn))
            {
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        //Console.WriteLine("{0} {1}", reader.GetString(0), reader.GetString(1));
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            data.Add(reader.GetString(i));
                        }
                    }
                }
            }
        }
        private List<string> tableNames()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT TABLE_NAME ");
            sb.Append("FROM INFORMATION_SCHEMA.TABLES ");
            sb.Append("WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_CATALOG = 'ideam-upc'");
            List<string> tableNames = new List<string>();
            

            String sql = sb.ToString();

            using (SqlCommand command = new SqlCommand(sql, cnn))
            {
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        //Console.WriteLine("{0} {1}", reader.GetString(0), reader.GetString(1));
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            tableNames.Add(reader.GetString(i));
                        }
                    }
                }
            }
            return tableNames;
        }
        private void tableCount()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT COUNT(*) FROM sys.Tables");
            
            String sql = sb.ToString();

            using (SqlCommand command = new SqlCommand(sql, cnn))
            {
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        //Console.WriteLine("{0} {1}", reader.GetString(0), reader.GetString(1));
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var v = reader.GetValue(i);
                        }
                    }
                }
            }
        }
    }
}
