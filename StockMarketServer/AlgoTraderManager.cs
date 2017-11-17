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
        public static ThreadDataBaseHandler threadDataBaseHandler = new ThreadDataBaseHandler();
        static List<AlgoTrader> Traders = new List<AlgoTrader>();

        static Random _random = new Random();

        private static double RandomNumberBetween(double minValue, double maxValue) {
            var next = _random.NextDouble();

            return minValue + (next * (maxValue - minValue));
        }

        public static void RunTrader() {
            MySqlDataReader reader = threadDataBaseHandler.GetData("SELECT ID FROM AlgoTrader");
            List<int> TradersInDB = new List<int>();
            List<int> TradersToDelete = new List<int>();
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
            foreach (AlgoTrader t in Traders) {
                t.RunTurn();
            }
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
            MySqlDataReader reader = threadDataBaseHandler.GetData("SELECT * FROM UserAlgoTraders");
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
                if (TriggersSuccesful) {
                    CurrentPrice = threadDataBaseHandler.GetCountDouble("SELECT SUM(CurrentPrice) FROM Stock WHERE StockName = '" + trader.action.Target + "'");
                    threadDataBaseHandler.SetData(string.Format("INSERT INTO Pool(Type, Price, User, StockName, Quantity) VALUES({0}, {1}, {2}, '{3}', {4})", (int)trader.action.BuyOrSell, CurrentPrice, trader.OwnerId, trader.action.Target, trader.action.Quantity));
                    threadDataBaseHandler.SetData("DELETE FROM AlgoTraders WHERE ID = " + trader.TraderID);
                    TradersToDelete.Add(trader);
                }
            }
            foreach (Trader t in TradersToDelete) {
                Traders.Remove(t);
            }
            TradersToDelete.Clear();
            threadDataBaseHandler.CloseCon();
        }
    }
}