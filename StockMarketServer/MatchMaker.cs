using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace StockMarketServer {
    class MatchMaker {
        ThreadDataBaseHandler threadDataBaseHandler;

        public void RunMatchMaker(string s, ThreadDataBaseHandler localThreadDataBaseHandler) {
            threadDataBaseHandler = localThreadDataBaseHandler;
            MySqlDataReader BidPoolReader = threadDataBaseHandler.GetData("SELECT DISTINCT Price FROM Pool WHERE Type = 0 AND StockName = '" + s + "' ORDER BY Price ASC");
            List<double> BidPriceLevels = new List<double>();
            while (BidPoolReader.Read()) {
                BidPriceLevels.Add((double)BidPoolReader["Price"]);
            }
            if (BidPriceLevels.Count == 0) {
                return;
            }
            MySqlDataReader OfferPoolReader = threadDataBaseHandler.GetData("SELECT DISTINCT Price FROM Pool WHERE Type = 1 AND StockName = '" + s + "' ORDER BY Price DESC");
            List<double> OfferPriceLevels = new List<double>();
            while (OfferPoolReader.Read()) {
                OfferPriceLevels.Add((double)OfferPoolReader["Price"]);
            }
            if (OfferPriceLevels.Count == 0) {
                return;
            }
            foreach (double BidPrice in BidPriceLevels) {
                for (int i = 0; i < OfferPriceLevels.Count; i++) {
                    if (OfferPriceLevels[i] <= BidPrice) {
                        MySqlDataReader BidReader = threadDataBaseHandler.GetData("SELECT * FROM Pool WHERE Type = 0 AND StockName = '" + s + "' AND Price = " + BidPrice);
                        List<BidsAndOffers> bids = new List<BidsAndOffers>();
                        while (BidReader.Read()) {
                            //Console.WriteLine(BidReader["Type"].GetType());
                            //Console.WriteLine(BidReader["TimePlaced"].GetType());
                            //Console.WriteLine(BidReader["Price"].GetType());
                            //Console.WriteLine(BidReader["User"].GetType());
                            //Console.WriteLine(BidReader["StockName"].GetType());
                            //Console.WriteLine(BidReader["Quantity"].GetType());
                            //Console.WriteLine(BidReader["TurnsInPool"].GetType());

                            bids.Add(new BidsAndOffers((bool)BidReader["Type"], (DateTime)BidReader["TimePlaced"], (double)BidReader["Price"], (int)BidReader["User"], (string)BidReader["StockName"], (int)BidReader["Quantity"], (int)BidReader["TurnsInPool"]));
                        }
                        MySqlDataReader OfferReader = threadDataBaseHandler.GetData("SELECT * FROM Pool WHERE Type = 1 AND StockName = '" + s + "' AND Price = " + OfferPriceLevels[i]);
                        List<BidsAndOffers> offers = new List<BidsAndOffers>();
                        while (OfferReader.Read()) {
                            offers.Add(new BidsAndOffers((bool)OfferReader["Type"], (DateTime)OfferReader["TimePlaced"], (double)OfferReader["Price"], (int)OfferReader["User"], (string)OfferReader["StockName"], (int)OfferReader["Quantity"], (int)OfferReader["TurnsInPool"]));
                        }
                        while (bids.Count != 0 && offers.Count != 0) {
                            BidsAndOffers b = bids[0];
                            LMMRound(ref b, ref offers);
                            #region Cleaner
                            foreach (BidsAndOffers o in offers) {
                                if (o.Quantity <= 0) {
                                    TradeManager.RemoveFromPool(o);
                                }
                            }
                            offers.RemoveAll((o) => o.Quantity <= 0);
                            bids[0] = b;
                            if (bids[0].Quantity <= 0) {
                                TradeManager.RemoveFromPool(bids[0]);
                                bids.RemoveAt(0);
                                continue;
                            }
                            #endregion
                            ProRataWithLMM(ref b, ref offers, 5);
                            #region Cleaner
                            foreach (BidsAndOffers o in offers) {
                                if (o.Quantity <= 0) {
                                    TradeManager.RemoveFromPool(o);
                                }
                            }
                            offers.RemoveAll((o) => o.Quantity <= 0);
                            bids[0] = b;
                            if (bids[0].Quantity <= 0) {
                                TradeManager.RemoveFromPool(bids[0]);
                                bids.RemoveAt(0);
                                continue;
                            }
                            #endregion
                            FIFO(ref b, ref offers);
                            #region Cleaner
                            foreach (BidsAndOffers o in offers) {
                                if (o.Quantity <= 0) {
                                    TradeManager.RemoveFromPool(o);
                                }
                            }
                            offers.RemoveAll((o) => o.Quantity <= 0);
                            bids[0] = b;
                            if (bids[0].Quantity <= 0) {
                                TradeManager.RemoveFromPool(bids[0]);
                                bids.RemoveAt(0);
                                continue;
                            }
                            #endregion
                        }
                    }
                }
            }
            threadDataBaseHandler.CloseCon();
        }




        private void LMMRound(ref BidsAndOffers Bid, ref List<BidsAndOffers> Offers) {
            for (int i = 0; i < Offers.Count; i++) {
                MySqlDataReader r = threadDataBaseHandler.GetData("SELECT LMM FROM Users WHERE ID = " + Offers[i].User);
                double LMMPercentage = 0;
                while (r.Read()) {
                    LMMPercentage = (double)r["LMM"];
                }
                if (LMMPercentage > 0f) {
                    int LMMAmount = (int)(Bid.Quantity * LMMPercentage);
                    if (LMMAmount > 0) {
                        if (LMMAmount > Offers[i].Quantity) {
                            BidsAndOffers Offer = Offers[i];
                            TradeManager.CreateTrade(ref Bid, ref Offer, Offer.Quantity, threadDataBaseHandler);
                            Offers[i] = Offer;
                        } else {
                            BidsAndOffers Offer = Offers[i];
                            TradeManager.CreateTrade(ref Bid, ref Offer, LMMAmount, threadDataBaseHandler);
                            Offers[i] = Offer;
                        }
                    }
                }
            }
        }

         void ProRataWithLMM(ref BidsAndOffers Bid, ref List<BidsAndOffers> Offers, int ProRataMinimumAlloaction) {
            int TotalQuanityOfOffers = 0;
            int BidQuanity = Bid.Quantity;
            foreach (BidsAndOffers o in Offers) {
                TotalQuanityOfOffers += o.Quantity;
            }
            for (int i = 0; i < Offers.Count; i++) {
                double ProRata = 0;
                if (Offers[i].Quantity != 0) {
                    ProRata = (double)Offers[i].Quantity / (double)TotalQuanityOfOffers;
                }
                int ProRataAmount = (int)(ProRata * BidQuanity);
                if (ProRataAmount >= ProRataMinimumAlloaction) {
                    if (ProRataAmount > Offers[i].Quantity) {
                        BidsAndOffers Offer = Offers[i];
                        TradeManager.CreateTrade(ref Bid, ref Offer, Offer.Quantity, threadDataBaseHandler);
                        Offers[i] = Offer;
                    } else {
                        BidsAndOffers Offer = Offers[i];
                        TradeManager.CreateTrade(ref Bid, ref Offer, ProRataAmount, threadDataBaseHandler);
                        Offers[i] = Offer;
                    }
                }
            }
        }

        void FIFO(ref BidsAndOffers Bid, ref List<BidsAndOffers> Offers) {
            Offers.OrderBy((o) => o.TimePlaced);
            for (int i = 0; i < Offers.Count; i++) {
                if (Bid.Quantity == 0) {
                    break;
                }
                if (Offers[i].Quantity == 0) {
                    continue;
                }
                if (Offers[i].Quantity > Bid.Quantity) {
                    BidsAndOffers Offer = Offers[i];
                    TradeManager.CreateTrade(ref Bid, ref Offer, Bid.Quantity, threadDataBaseHandler);
                    Offers[i] = Offer;
                } else {
                    BidsAndOffers Offer = Offers[i];
                    TradeManager.CreateTrade(ref Bid, ref Offer, Offer.Quantity, threadDataBaseHandler);
                    Offers[i] = Offer;
                }
            }
        }
    }

    class BidsAndOffers {
        public bool Type;
        public DateTime TimePlaced;
        public double Price;
        public int User;
        public string StockName;
        public int Quantity;
        public int TurnsInPool;
        public BidsAndOffers(bool type, DateTime timePlace, double price, int user, string stockName, int quantity, int turnsInPool) {
            Type = type;
            TimePlaced = timePlace;
            Price = price;
            User = user;
            StockName = stockName;
            Quantity = quantity;
            TurnsInPool = turnsInPool;
        }
    }
}
