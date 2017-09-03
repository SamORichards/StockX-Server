﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace StockMarketServer {
    enum MathOperator { Greater, Less }
    enum BuySell { Buy, Sell }
    class AlgoTraderManager {
        public static void RunTrader() {
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
            MySqlDataReader reader = DataBaseHandler.GetData("SELECT * FROM AlgoTraders");
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
                    double CurrentPrice = DataBaseHandler.GetCountDouble("SELECT SUM(CurrentPrice) FROM Stock WHERE StockName = '" + trigger.Target + "'");
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
                        double CurrentPrice = DataBaseHandler.GetCountDouble("SELECT SUM(CurrentPrice) FROM Stock WHERE StockName = '" + action.Target + "'");
                        DataBaseHandler.SetData(string.Format("INSERT INTO Pool(Type, Price, User, StockName, Quantity) VALUES({0}, {1}, {2}, '{3}', {4})", (int)action.BuyOrSell, CurrentPrice, trader.OwnerId, action.Target, action.Quantity));
                    }
                    DataBaseHandler.SetData("DELETE FROM AlgoTraders WHERE ID = " + trader.TraderID);
                    TradersToDelete.Add(trader);
                }
            }
            foreach (Trader t in TradersToDelete) {
                Traders.Remove(t);
            }
            TradersToDelete.Clear();
        }
    }
}