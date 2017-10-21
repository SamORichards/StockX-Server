using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StockMarketServer {
    class Pool {
        public static void RunPool() {
            DataBaseHandler.SetData("DELETE FROM Pool WHERE Quantity <= 0");
            MySqlDataReader reader = DataBaseHandler.GetData("SELECT StockName FROM Stock");
            List<string> Stocks = new List<string>();
            while (reader.Read()) {
                Stocks.Add((string)reader["StockName"]);
            }
            foreach (string s in Stocks) {
                Task.Factory.StartNew(() => new MatchMaker().RunMatchMaker(s, new ThreadDataBaseHandler()));
            }
            DataBaseHandler.SetData("UPDATE Pool SET TurnsInPool = TurnsInPool + 1");
            MySqlDataReader Reader = DataBaseHandler.GetData("SELECT * FROM Pool WHERE TurnsInPool = 30");
            List <BidsAndOffers> bidsAndOffers = new List<BidsAndOffers>();
            while (Reader.Read()) {
                bidsAndOffers.Add(new BidsAndOffers((bool)Reader["Type"], (DateTime)Reader["TimePlaced"], (double)Reader["Price"], (int)Reader["User"], (string)Reader["StockName"], (int)Reader["Quantity"], (int)Reader["TurnsInPool"]));
            }
            foreach (BidsAndOffers b in bidsAndOffers) {
                MarketMaker.AddJob(b);
            }
            DataBaseHandler.SetData("DELETE FROM Pool WHERE `TurnsInPool` = 30");
            MarketMaker.RunTurn();
        }
    }
}
