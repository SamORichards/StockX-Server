using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Threading;

namespace StockMarketServer {
    class ThreadDataBaseHandler {
        public static string myConnectionString = DataBaseHandler.myConnectionString;
        public MySqlConnection sqlCon = new MySqlConnection(myConnectionString);

        public static int UserID { get { return DataBaseHandler.UserID; } }

        public  void StartServer() {
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
            //for (int i = 0; i < 10; i++) {
            //try {
            reader = com.ExecuteReader();
            return reader;
            //} 
            //catch {
            //    if (i == 9) {
            //        try {
            //            sqlCon.Close();
            //        } catch { }
            //        OpenConnection();
            //        i = 0;
            //    }
            //}
            //}
            return null;
        }


        public  void SetData(string command) {
            ReadyConnection();
            MySqlCommand com = new MySqlCommand(command, sqlCon);
            //for (int i = 0; i < 10; i++) {
            //try {
            com.ExecuteNonQuery();
            //    } catch {
            //        if (i == 9) {
            //            try {
            //                sqlCon.Close();
            //            } catch { }
            //            OpenConnection();
            //            i = 0;
            //        }
            //    }
            //}
        }

        public  int GetCount(string command) {
            ReadyConnection();
            MySqlCommand com = new MySqlCommand(command, sqlCon);
            //for (int i = 0; i < 10; i++) {
            //try {
            string t = com.ExecuteScalar().ToString();
            if (t.Length == 0) {
                return 0;
            } else {
                return int.Parse(t);
            }
            //    } catch {
            //        if (i == 9) {
            //            try {
            //                sqlCon.Close();
            //            } catch { }
            //            OpenConnection();
            //            i = 0;
            //        }
            //    }
            //}
        }

        public  double GetCountDouble(string command) {
            ReadyConnection();
            MySqlCommand com = new MySqlCommand(command, sqlCon);
            //for (int i = 0; i < 10; i++) {
            //try {
            string t = com.ExecuteScalar().ToString();
            if (t.Length == 0) {
                return 0;
            } else {
                return double.Parse(t);
            }

            //    } catch {
            //        if (i == 9) {
            //            try {
            //                sqlCon.Close();
            //            } catch { }
            //            OpenConnection();
            //            i = 0;
            //        }
            //    }
            //}
        }

         void ReadyConnection() {
            try {
                sqlCon.Close();
            } catch { }
            StartServer();
        }


        private  void OpenConnection() {
            for (int i = 0; i < 10; i++) {
                try {
                sqlCon.Open();
                    break;
                } catch {
                }
            }
        }
    }
}
