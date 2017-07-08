using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockMarketServer {
    static class NetworkHandler {
        static string myConnectionString = "server=localhost;database=StockMarket;uid=root;pwd=mysql;";
        public static MySqlConnection sqlCon = new MySqlConnection(myConnectionString);
        public static void StartServer() {
            OpenConnection();
        }

        public static MySqlDataReader GetData(string command) {
            MySqlCommand com = new MySqlCommand(command, sqlCon);
            MySqlDataReader reader;
            for (int i = 0; i < 10; i++) {
                try {
                    reader = com.ExecuteReader();
                    return reader;
                } catch {
                    if (i == 9) {
                        try {
                            sqlCon.Close();
                        } catch { }
                        OpenConnection();
                        i = 0;
                    }
                }
            }
            return null;
        }

        private static void OpenConnection() {
            for (int i = 0; i < 10; i++) {
                try {
                    sqlCon.Open();
                    break;
                } catch {
                    if (i == 9) {
                        Console.WriteLine("Failed to connect to database");
                        throw new Exception("Failed to connect to database");
                    }
                }
            }
        }
        
    }
}
