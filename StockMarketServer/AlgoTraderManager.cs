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

        public static void CreateShortTermTarders() {
            threadDataBaseHandler.SetData("DELETE FROM Inventories WHERE UserID > 404");
            threadDataBaseHandler.SetData("DELETE FROM Users WHERE Nickname = 'AlgoTrader'");
            MySqlDataReader reader = threadDataBaseHandler.GetData("SELECT StockName, CurrentPrice FROM Stock");
            List<KeyValuePair<string, double>> Stocks = new List<KeyValuePair<string, double>>();
            while (reader.Read()) {
                string StockName = (string)reader["StockName"];
                double CurrentPrice = (double)reader["CurrentPrice"];
                Stocks.Add(new KeyValuePair<string, double>(StockName, CurrentPrice));
            }
            foreach (KeyValuePair<string, double> s in Stocks) {
                string StockName = s.Key;
                double CurrentPrice = s.Value;
                for (int i = 0; i < _random.Next(1, 3); i++) {
                    string email = "AlgoTrader" + StockName + "@" + i + ".com";
                    string command = string.Format("INSERT INTO Users(NickName, Email, Password, Balance, Admin, LMM) VALUES ('{0}', '{1}', '{2}', {3}, {4}, {5})", "AlgoTrader", email, "Password", 1000000, 0, 0.20f);
                    threadDataBaseHandler.SetData(command);
                    int UserId = threadDataBaseHandler.GetCount("SELECT SUM(ID) FROM Users WHERE Email = '" + email + "'");
                    DataBaseHandler.SetData(string.Format("INSERT INTO Inventories(UserID, StockName, Quantity, LastTradedPrice) VALUES({0}, '{1}', {2}, {3})", UserId, StockName, _random.Next(100, 200), CurrentPrice));
                    Traders.Add(new AlgorithmsTrader1(StockName, UserId, RandomNumberBetween(1.1, 2.1), RandomNumberBetween(1.9, 2.9), _random.Next(1, 3), _random.Next(20, 30), RandomNumberBetween(0, 1)));
                }
            }
            threadDataBaseHandler.CloseCon();
        }

        public static void RunTrader() {
            foreach (AlgoTrader t in Traders) {
                t.RunTurn();
            }
            BasicTraders();
            TimingManager.TraderTimer.Start();
        }
        class Trader {
            public int TraderID;
            public int OwnerId;
            public List<Trigger> Triggers = new List<Trigger>();
            public List<Action> Actions = new List<Action>();
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
            MySqlDataReader reader = threadDataBaseHandler.GetData("SELECT * FROM AlgoTraders");
            while (reader.Read()) {
                Trader trader = new Trader();
                trader.TraderID = (int)reader["ID"];
                trader.OwnerId = (int)reader["OwnerID"];
                string Command = (string)reader["Command"];
                string Triggers = Command.Split('#')[0];
                string Actions = Command.Split('#')[1];
                foreach (string s in Triggers.Split('*')) {
                    if (s.Length == 0) {
                        continue;
                    }
                    Trigger t = new Trigger();
                    t.Target = s.Split('|')[0];
                    t.Operator = (MathOperator)int.Parse(s.Split('|')[1]);
                    t.Value = double.Parse(s.Split('|')[2]);
                    trader.Triggers.Add(t);
                }
                foreach (string s in Actions.Split('*')) {
                    if (s.Length == 0) {
                        continue;
                    }
                    Action a = new Action();
                    a.Target = s.Split('|')[0];
                    a.BuyOrSell = (BuySell)int.Parse(s.Split('|')[1]);
                    a.Quantity = int.Parse(s.Split('|')[2]);
                    trader.Actions.Add(a);
                }
                Traders.Add(trader);
            }
            List<Trader> TradersToDelete = new List<Trader>();
            foreach (Trader trader in Traders) {
                bool TriggersSuccesful = true;
                foreach (Trigger trigger in trader.Triggers) {
                    double CurrentPrice = threadDataBaseHandler.GetCountDouble("SELECT SUM(CurrentPrice) FROM Stock WHERE StockName = '" + trigger.Target + "'");
                    if (trigger.Operator == MathOperator.Greater) {
                        if (!(trigger.Value > CurrentPrice)) {
                            TriggersSuccesful = false;
                        }
                    } else {
                        if (!(trigger.Value < CurrentPrice)) {
                            TriggersSuccesful = false;
                        }
                    }
                }
                if (TriggersSuccesful) {
                    foreach (Action action in trader.Actions) {
                        double CurrentPrice = threadDataBaseHandler.GetCountDouble("SELECT SUM(CurrentPrice) FROM Stock WHERE StockName = '" + action.Target + "'");
                        threadDataBaseHandler.SetData(string.Format("INSERT INTO Pool(Type, Price, User, StockName, Quantity) VALUES({0}, {1}, {2}, '{3}', {4})", (int)action.BuyOrSell, CurrentPrice, trader.OwnerId, action.Target, action.Quantity));
                    }
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