using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Threading;
using System.Diagnostics;

namespace StockMarketServer {
    class ThreadDataBaseHandler {
        public static string myConnectionString = DataBaseHandler.myConnectionString;
        public MySqlConnection sqlCon = new MySqlConnection(myConnectionString);

        public static int UserID { get { return DataBaseHandler.UserID; } }

        public void StartServer() {
            OpenConnection();
        }
        public void CloseCon() {
            try {
                sqlCon.Close();
            } catch { }
        }

        public MySqlDataReader GetData(string command) {
            ReadyConnection();
            MySqlCommand com = new MySqlCommand(command, sqlCon);
            MySqlDataReader reader;
            reader = com.ExecuteReader();
            return reader;
        }


        public void SetData(string command) {
            ReadyConnection();
            MySqlCommand com = new MySqlCommand(command, sqlCon);
            com.ExecuteNonQuery();
        }

        public int GetCount(string command) {
            ReadyConnection();
            MySqlCommand com = new MySqlCommand(command, sqlCon);
            string t = com.ExecuteScalar().ToString();
            if (t.Length == 0) {
                return 0;
            } else {
                return int.Parse(t);
            }
        }

        public double GetCountDouble(string command) {
            ReadyConnection();
            MySqlCommand com = new MySqlCommand(command, sqlCon);
            string t = com.ExecuteScalar().ToString();
            if (t.Length == 0) {
                return 0;
            } else {
                return double.Parse(t);
            }
        }

        void ReadyConnection() {
            try {
                sqlCon.Close();
            } catch { }
            StartServer();
        }


        private void OpenConnection() {
            sqlCon.Open();
        }
    }
}
