using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockMarketServer {
    class DataBaseHandler {
        public static string myConnectionString = "server=localhost;database=stockmarket;uid=Sam;pwd=230999;Convert Zero Datetime=True";
        public static MySqlConnection sqlCon = new MySqlConnection(myConnectionString);

        public static int UserID { get { return 1; } }

        public static MySqlDataReader GetData(string command) {
            ReadyConnection();
            MySqlCommand com = new MySqlCommand(command, sqlCon);
            MySqlDataReader reader;
            reader = com.ExecuteReader();
            return reader;
        }


        public static void SetData(string command) {
            ReadyConnection();
            MySqlCommand com = new MySqlCommand(command, sqlCon);
            com.ExecuteNonQuery();
        }

        public static int GetCount(string command) {
            ReadyConnection();
            MySqlCommand com = new MySqlCommand(command, sqlCon);
            string t = com.ExecuteScalar().ToString();
            if (t.Length == 0) {
                return 0;
            } else {
                return int.Parse(t);
            }
        }

        public static double GetCountDouble(string command) {
            ReadyConnection();
            MySqlCommand com = new MySqlCommand(command, sqlCon);
            string t = com.ExecuteScalar().ToString();
            if (t.Length == 0) {
                return 0;
            } else {
                return double.Parse(t);
            }
        }

        public static void ReadyConnection() {
            try {
                sqlCon.Close();
            } catch { }
            OpenConnection();
        }


        private static void OpenConnection() {
            sqlCon.Open();
        }
    }
}
