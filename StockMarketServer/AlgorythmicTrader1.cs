using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace StockMarketServer {
    enum BidOffer { bid, offer }
    class AlgorythmicTrader1 {
        public enum Segments { RapidlyRising, Rising, Falling, RapidlyFalling }
        public enum Stance { ShortTermLong, ShortTermShort }
        class AlgorithmsTrader1 : AlgoTrader {
            DateTime LastTurn;
            int UserID;
            string TargetStock;
            public List<StockTurn> StockTurns = new List<StockTurn>();
            List<MarketStance> stances = new List<MarketStance>();


            //Trader Description
            //Takes a look at last two cycles
            //establishes trend
            //purchases or sell depending on position eg int or short
            public AlgorithmsTrader1(int stocksOwned, string target, int ClientID) {
                StocksOwned = stocksOwned;
                TargetStock = target;
                UserID = ClientID;
            }

            public void RunTurn() {
                MySqlDataReader reader = DataBaseHandler.GetData("SELECT Price FROM Trades WHERE Time > " + LastTurn);
                List<float> Trades = new List<float>();
                while (reader.Read()) {
                    Trades.Add((float)reader["Price"]);
                }
                if (Trades.Count == 0) {
                    return;
                }
                StockTurns.Add(new StockTurn(Trades));
                CreateNewStance(true);
                if (StockTurns.Count >= 150) {
                    StockTurns.RemoveAt(0);
                }
                foreach (MarketStance MS in stances) {
                    MS.RunTurn();
                }
                stances.RemoveAll((MarketStance ms) => ms.isCompleted);
                LastTurn = DateTime.Now;
            }
            void CreateNewStance(bool ShortTerm) {
                if (StockTurns[StockTurns.Count - 1].AveragePrice < 1f) {
                    new MarketStance(Stance.ShortTermLong, 1000, UserID, TargetStock, this);
                }
                if (ShortTerm) {
                    if (StockTurns.Count < 4) {
                        return;
                    }
                    int TotalLast3Turns = 0;
                    for (int i = 1; i <= 3; i++) {
                        TotalLast3Turns += (int)StockTurns[StockTurns.Count - i].Trend + 1;
                    }
                    float AverageLast3Turns = (float)TotalLast3Turns / 3f;
                    if (AverageLast3Turns <= 1.6f) {
                        stances.Add(new MarketStance(Stance.ShortTermLong, MathsHelper.Lerp(10, 100, 1f - (AverageLast3Turns - 1f)), UserID, TargetStock,  this));
                    } else if (AverageLast3Turns >= 2.4f) {
                        stances.Add(new MarketStance(Stance.ShortTermShort, MathsHelper.Lerp(10, 100, (AverageLast3Turns - 2.4f) / (4f - 2.4f)), UserID, TargetStock, this));
                    }
                }
            }

            class MarketStance {
                public Stance stance;
                public int client;
                float SuccessPrice;
                float FailurePrice;
                int Quanity;
                AlgoTrader Owner;
                public bool isCompleted = false;
                bool OfferPlaced = false;
                string TargetStock;
                DateTime StartTime;
                int RequiredTime;

                public MarketStance(Stance s, int Quanity, int ClientID, string TargetStock, AlgoTrader owner) {
                    stance = s;
                    Owner = owner;
                    this.TargetStock = TargetStock;
                    StartTime = DateTime.Now;
                    float CurrentPrice = DataBaseHandler.GetCount("SELECT SUM(Price) FROM Stock WHERE StockName = '" + this.TargetStock + "'");
                    switch (stance) {
                        case Stance.ShortTermLong:
                            this.Quanity = Quanity;
                            SuccessPrice = CurrentPrice + 0.10f;
                            FailurePrice = CurrentPrice - 0.05f;
                            RequiredTime = 5;
                            ShortTermLong(Quanity, CurrentPrice);
                            break;
                        case Stance.ShortTermShort:
                            this.Quanity = Quanity;
                            SuccessPrice = CurrentPrice - 0.10f;
                            FailurePrice = CurrentPrice + 0.05f;
                            RequiredTime = 5;
                            ShortTermShort(Quanity, CurrentPrice);
                            break;
                    }
                }

                private void ShortTermShort(int Quanity, float Price) {
                    DataBaseHandler.SetData(string.Format("INSERT INTO Pool (Type, Price, User, StockName, Quantity) VALUES ({0}, {1}, {2}, '{3}', {4})", (int)BidOffer.offer, Price, client, TargetStock, Quanity));
                }

                void ShortTermLong(int Quanity, float Price) {
                    DataBaseHandler.SetData(string.Format("INSERT INTO Pool (Type, Price, User, StockName, Quantity) VALUES ({0}, {1}, {2}, '{3}', {4})", (int)BidOffer.bid, Price, client, TargetStock, Quanity));
                }

                public void RunTurn() {
                    float CurrentPrice = DataBaseHandler.GetCount("SELECT SUM(Price) FROM Stock WHERE StockName = '" + TargetStock + "'");
                    TimeSpan TimeTaken = DateTime.Now - StartTime;
                    switch (stance) {
                        case Stance.ShortTermLong:
                            if ((CurrentPrice >= SuccessPrice || CurrentPrice < FailurePrice) && !OfferPlaced && TimeTaken.TotalMinutes > RequiredTime) {
                                DataBaseHandler.SetData(string.Format("INSERT INTO Pool (Type, Price, User, StockName, Quantity) VALUES ({0}, {1}, {2}, '{3}', {4})", (int)BidOffer.offer, CurrentPrice, client, TargetStock, Quanity));
                                OfferPlaced = true;
                            }
                            break;
                        case Stance.ShortTermShort:
                            if ((CurrentPrice <= SuccessPrice || CurrentPrice > FailurePrice) && !OfferPlaced && TimeTaken.TotalMinutes > RequiredTime) {
                                DataBaseHandler.SetData(string.Format("INSERT INTO Pool (Type, Price, User, StockName, Quantity) VALUES ({0}, {1}, {2}, '{3}', {4})", (int)BidOffer.bid, CurrentPrice, client, TargetStock, Quanity));
                                OfferPlaced = true;
                            }
                            break;

                    }
                }
            }
        }

        class StockTurn {
            public float OpeningPrice;
            public float LowPrice;
            public float HighPrice;
            public float ClosePrice;
            public float AveragePrice;
            public Segments Trend;
            public StockTurn(List<float> trades) {
                OpeningPrice = trades[0];
                LowPrice = trades.OrderBy((float t) => t).ToList()[0];
                HighPrice = trades.OrderByDescending((float t) => t).ToList()[0];
                ClosePrice = trades[trades.Count - 1];
                float TotalPrice = 0;
                foreach (float t in trades) {
                    TotalPrice += t;
                }
                AveragePrice = TotalPrice / (float)trades.Count;
                Trend = AssignSegment();
                //Console.WriteLine(Trend);
            }
            Segments AssignSegment() {
                if (ClosePrice > OpeningPrice) {
                    if (ClosePrice > AveragePrice) {
                        return Segments.RapidlyRising;
                    } else {
                        return Segments.Rising;
                    }
                } else {
                    if (ClosePrice > AveragePrice) {
                        return Segments.Falling;
                    } else {
                        return Segments.RapidlyFalling;
                    }
                }
            }
        }

        public class AlgoTrader {
            public int StocksOwned;
        }
    }

}

