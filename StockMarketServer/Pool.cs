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
            //Clears all empty bids and offers from pool
            DataBaseHandler.SetData("DELETE FROM Pool WHERE Quantity <= 0");
            //Get all the stocks so we can assign match makers
            MySqlDataReader reader = DataBaseHandler.GetData("SELECT StockName FROM Stock");
            List<string> Stocks = new List<string>();
            while (reader.Read()) {
                Stocks.Add((string)reader["StockName"]);
            }
            //Create a list of task for multi treading
            Task[] tasks = new Task[Stocks.Count];
            foreach (string s in Stocks) {
				//Make sure there are enough db handlers avialable
				int i = Stocks.FindIndex((e) => e == s);
                while (i > DataBaseHandlers.Count - 1) {
                    DataBaseHandlers.Add(new ThreadDataBaseHandler());
                }
				//Create a new task which will be added into the queue to be assign a thread
                Task t = new Task(() => new MatchMaker().RunMatchMaker(s, i));
                t.Start();
				//Add the task to list so we can keep track of its progress
                tasks[i] = t;
            }
			//Wait until all the task have been complete before we advance
            Task.WaitAll(tasks);
            DataBaseHandler.SetData("UPDATE Pool SET TurnsInPool = TurnsInPool + 1");
			//If a bid / offer has been in the pool for 30 turns we grab it from the pool assign it to Market Maker
            MySqlDataReader Reader = DataBaseHandler.GetData("SELECT * FROM Pool WHERE TurnsInPool = 30");
            List<BidsAndOffers> bidsAndOffers = new List<BidsAndOffers>();
            while (Reader.Read()) {
                bidsAndOffers.Add(new BidsAndOffers((bool)Reader["Type"], (DateTime)Reader["TimePlaced"], (double)Reader["Price"], (int)Reader["User"], (string)Reader["StockName"], (int)Reader["Quantity"], (int)Reader["TurnsInPool"]));
            }
            foreach (BidsAndOffers b in bidsAndOffers) {
                MarketMaker.AddJob(b);
            }
			//Delete the bids / offers from the pool that we have now passed to the market maker to complete
            DataBaseHandler.SetData("DELETE FROM Pool WHERE `TurnsInPool` = 30");
			//Delete trades that are older than a day to reduce the size of the DB
            DataBaseHandler.SetData("DELETE FROM Trades WHERE Time < '" + DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd HH:mm:ss") + "'");
			//Run the market maker turn
            MarketMaker.RunTurn();
        }
    }
}
