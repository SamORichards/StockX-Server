using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace StockMarketServer {
    enum MathOperator { Greater, Less }
    enum BuySell { Buy, Sell }
    class AlgoTraderManager {
		//List of thread database handler class for different class to grab to access the database
		//This is done to keep track of number of connection to the database to make sure that
		//it does exceed the limit of the DB server
        public static ThreadDataBaseHandler threadDataBaseHandler = new ThreadDataBaseHandler();
        static List<AlgoTrader> Traders = new List<AlgoTrader>();

        static Random _random = new Random();

		//Return a random double between bounds
        private static double RandomNumberBetween(double minValue, double maxValue) {
            var next = _random.NextDouble();

            return minValue + (next * (maxValue - minValue));
        }

		//This method is called from TimingManager and manages the excutions of Algorythemic Traders
        public static async void RunTrader() {
			//Get the trader ids from the database
            MySqlDataReader reader = threadDataBaseHandler.GetData("SELECT ID FROM AlgoTrader");
            List<int> TradersInDB = new List<int>();
            List<int> TradersToDelete = new List<int>();
			//Checks which traders we have stored locally
			//if the id exists in the DB but not in this program them grab from the DB and create local version
			//else if if it exists in this local version but not in the DB then delete the local instance
            while (reader.Read()) {
                TradersInDB.Add((int)reader["ID"]);
            }
            for (int i = 0; i < Traders.Count; i++) {
                if (!TradersInDB.Contains(Traders[i].ID)) {
                    TradersToDelete.Add(i);
                }
            }
            for (int i = 0; i < TradersInDB.Count; i++) {
                if (!Traders.Exists((t)=> t.ID == TradersInDB[i])) {
                    reader = threadDataBaseHandler.GetData("SELECT * FROM AlgoTrader WHERE ID = " + TradersInDB[i]);
                    while (reader.Read()) {
                        Traders.Add(new AlgorithmsTrader1(TradersInDB[i], (string)reader["Target"], (int)reader["UserId"], (double)reader["ShortRequirement"], (double)reader["LongRequirement"], (int)reader["MinAmount"], (int)reader["MaxAmount"], (double)reader["Aggresion"]));
                    }
                }
            }
            while (TradersToDelete.Count > 0) {
                threadDataBaseHandler.SetData("DELETE FROM Users WHERE ID = " + Traders[TradersToDelete[0]].UserID);
                threadDataBaseHandler.SetData("UPDATE Inventories SET UserID = 1 WHERE UserID = "+ Traders[TradersToDelete[0]].UserID);
                threadDataBaseHandler.SetData("DELETE FROM AlgoTrader WHERE ID = " + Traders[TradersToDelete[0]].ID);
                Traders.Remove(Traders[TradersToDelete[0]]);
                TradersToDelete.RemoveAt(0);
            }
			//Now we have all the correct traders stored locally we can run a turn on them
            foreach (AlgoTrader t in Traders) {
                t.RunTurn();
            }
			//Basic traders are the ones create by the user which just excute traders if certain criteria is met
			BasicTraders();
            TimingManager.TraderTimer.Start();
        }
        class Trader {
            public int TraderID;
            public int OwnerId;
            public Trigger Trigger;
            public Action action;
        }
        class Trigger {
            public string Target;
            public MathOperator Operator;
            public double Value;
        }
        class Action {
            public string Target;
            public BuySell BuyOrSell;
            public int Quantity;
        }
        private static void BasicTraders() {
            List<Trader> Traders = new List<Trader>();
			//Grab the basic traders from the DB
            MySqlDataReader reader = threadDataBaseHandler.GetData("SELECT * FROM UserAlgoTraders");
			//Load them into instance of the local class so that we can evaluate them
            while (reader.Read()) {
                Trader trader = new Trader();
                trader.TraderID = (int)reader["ID"];
                trader.OwnerId = (int)reader["OwnerID"];
                trader.Trigger = new Trigger();
                trader.Trigger.Target = (string)reader["TTarget"];
                trader.Trigger.Operator = (MathOperator)((int)reader["TOperator"]);
                trader.Trigger.Value = (double)reader["TValue"];
                trader.action = new Action();
                trader.action.Target = (string)reader["ATarget"];
                trader.action.BuyOrSell = (BuySell)((int)reader["ABuyOrSell"]);
                trader.action.Quantity = (int)reader["AQuantity"];
                Traders.Add(trader);
            }
            List<Trader> TradersToDelete = new List<Trader>();
			//Now for all of the basic traders stored locally we now evaulute against it condition
            foreach (Trader trader in Traders) {
                bool TriggersSuccesful = true;
                    double CurrentPrice = threadDataBaseHandler.GetCountDouble("SELECT SUM(CurrentPrice) FROM Stock WHERE StockName = '" + trader.Trigger.Target + "'");
                    if (trader.Trigger.Operator == MathOperator.Greater) {
                        if (!(trader.Trigger.Value > CurrentPrice)) {
                            TriggersSuccesful = false;
                        }
                    } else {
                        if (!(trader.Trigger.Value < CurrentPrice)) {
                            TriggersSuccesful = false;
                        }
                    }
				//If the condition is met we now excute position set by the user
                if (TriggersSuccesful) {
                    CurrentPrice = threadDataBaseHandler.GetCountDouble("SELECT SUM(CurrentPrice) FROM Stock WHERE StockName = '" + trader.action.Target + "'");
                    threadDataBaseHandler.SetData(string.Format("INSERT INTO Pool(Type, Price, User, StockName, Quantity) VALUES({0}, {1}, {2}, '{3}', {4})", (int)trader.action.BuyOrSell, CurrentPrice, trader.OwnerId, trader.action.Target, trader.action.Quantity));
                    threadDataBaseHandler.SetData("DELETE FROM AlgoTraders WHERE ID = " + trader.TraderID);
					//Add to traders to be delete as the basic trader is now complete
                    TradersToDelete.Add(trader);
                }
            }
            foreach (Trader t in TradersToDelete) {
                Traders.Remove(t);
            }
            TradersToDelete.Clear();
			//Close the DB thread to free it up for other parts of the program to use
            threadDataBaseHandler.CloseCon();
        }
    }
}