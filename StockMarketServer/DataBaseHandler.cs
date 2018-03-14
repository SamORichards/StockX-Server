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

		//This is 1 as that is the account number for the admin account in the DB
        public static int UserID { get { return 1; } }

		/// <summary>
		/// This method is for SELECT MySql commands
		/// It returns a MySqlDataReader
		/// </summary>
		/// <param name="command">
		/// This is the SELECT MySql command that will be sent to the DB
		/// </param>
		/// <returns></returns>
        public static MySqlDataReader GetData(string command) {
            ReadyConnection();
            MySqlCommand com = new MySqlCommand(command, sqlCon);
            MySqlDataReader reader;
            reader = com.ExecuteReader();
            return reader;
        }

		/// <summary>
		/// This Method is for UPDATE MySql commands
		/// It returns a MySqlDataReader
		/// </summary>
		/// <param name="command">
		/// This is the UPDATE MySql command that will be sent to the DB
		/// </param>
		public static void SetData(string command) {
            ReadyConnection();
            MySqlCommand com = new MySqlCommand(command, sqlCon);
            com.ExecuteNonQuery();
        }

		/// <summary>
		///  This Method is for COUNT MySql commands
		///	 It returns a int
		/// </summary>
		/// <param name="command">
		/// This is the COUNT MySql command that will be sent to the DB
		/// </param>
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

		/// <summary>
		///  This Method is for COUNT MySql commands
		///	 It returns a dpuble
		/// </summary>
		/// <param name="command">
		/// This is the COUNT MySql command that will be sent to the DB
		/// </param>
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

		/// <summary>
		/// This method closes the connection to the DB if it is open
		/// it then called Openconnection
		/// </summary>
        public static void ReadyConnection() {
            try {
                sqlCon.Close();
            } catch { }
            OpenConnection();
        }

		/// <summary>
		/// This method simply opens the connection to the DB
		/// </summary>
        private static void OpenConnection() {
            sqlCon.Open();
        }
    }
}
