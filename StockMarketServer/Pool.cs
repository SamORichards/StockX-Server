using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StockMarketServer {
    class Pool {
        public static List<ThreadDataBaseHandler> DataBaseHandlers = new List<ThreadDataBaseHandler>();
        public static void RunPool() {
            DataBaseHandler.SetData("DELETE FROM Pool WHERE Quantity <= 0");
            MySqlDataReader reader = DataBaseHandler.GetData("SELECT StockName FROM Stock");
            List<string> Stocks = new List<string>();
            while (reader.Read()) {
                Stocks.Add((string)reader["StockName"]);
            }
            Task[] tasks = new Task[Stocks.Count];
            foreach (string s in Stocks) {
                int i = Stocks.FindIndex((e) => e == s);
                while (i > DataBaseHandlers.Count - 1) {
                    DataBaseHandlers.Add(new ThreadDataBaseHandler());
                }
                Task t = new Task(() => new MatchMaker().RunMatchMaker(s, i));
                t.Start();
                tasks[i] = t;
            }
            Task.WaitAll(tasks);
            DataBaseHandler.SetData("UPDATE Pool SET TurnsInPool = TurnsInPool + 1");
            MySqlDataReader Reader = DataBaseHandler.GetData("SELECT * FROM Pool WHERE TurnsInPool = 30");
            List<BidsAndOffers> bidsAndOffers = new List<BidsAndOffers>();
            while (Reader.Read()) {
                bidsAndOffers.Add(new BidsAndOffers((bool)Reader["Type"], (DateTime)Reader["TimePlaced"], (double)Reader["Price"], (int)Reader["User"], (string)Reader["StockName"], (int)Reader["Quantity"], (int)Reader["TurnsInPool"]));
            }
            foreach (BidsAndOffers b in bidsAndOffers) {
                MarketMaker.AddJob(b);
            }
            DataBaseHandler.SetData("DELETE FROM Pool WHERE `TurnsInPool` = 30");
            DataBaseHandler.SetData("DELETE FROM Trades WHERE Time < '" + DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd HH:mm:ss") + "'");
            MarketMaker.RunTurn();
        }
    }
}
