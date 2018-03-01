using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace StockMarketServer {
    class MatchMaker {
        ThreadDataBaseHandler threadDataBaseHandler;

        public async void RunMatchMaker(string s, int ThreadCounter) {
            //Store DB connection class from array in Pool in order to not overload the server with conncetions
            threadDataBaseHandler = Pool.DataBaseHandlers[ThreadCounter];
            //Get the Pricelevels of bids for the assigned stock from DB
            MySqlDataReader BidPoolReader = threadDataBaseHandler.GetData("SELECT DISTINCT Price FROM Pool WHERE Type = 0 AND StockName = '" + s + "' ORDER BY Price ASC");
            List<double> BidPriceLevels = new List<double>();
            while (BidPoolReader.Read()) {
                //Read redsults from DB
                BidPriceLevels.Add((double)BidPoolReader["Price"]);
            }
            if (BidPriceLevels.Count == 0) {
                return;
            }
            //Get the Pricelevels of offers for the assigned stock from DB
            MySqlDataReader OfferPoolReader = threadDataBaseHandler.GetData("SELECT DISTINCT Price FROM Pool WHERE Type = 1 AND StockName = '" + s + "' ORDER BY Price DESC");
            List<double> OfferPriceLevels = new List<double>();
            while (OfferPoolReader.Read()) {
                //Read redsults from DB
                OfferPriceLevels.Add((double)OfferPoolReader["Price"]);
            }
            if (OfferPriceLevels.Count == 0) {
                return;
            }
            //Close connection with DB
            threadDataBaseHandler.CloseCon();
            foreach (double BidPrice in BidPriceLevels) {
                for (int i = 0; i < OfferPriceLevels.Count; i++) {
                    //Cycle through the bid price levels with every offer price level to find offers which are lower than that of the bid price level
                    if (OfferPriceLevels[i] <= BidPrice) {
                        //if this is the case then lets grab all the bids at this level to be able to assign with offers at this price level
                        MySqlDataReader BidReader = threadDataBaseHandler.GetData("SELECT * FROM Pool WHERE Type = 0 AND StockName = '" + s + "' AND Price = " + BidPrice);
                        List<BidsAndOffers> bids = new List<BidsAndOffers>();
                        while (BidReader.Read()) {
							//These are commented out as they are used for debugging
                            //Console.WriteLine(BidReader["Type"].GetType());
                            //Console.WriteLine(BidReader["TimePlaced"].GetType());
                            //Console.WriteLine(BidReader["Price"].GetType());
                            //Console.WriteLine(BidReader["User"].GetType());
                            //Console.WriteLine(BidReader["StockName"].GetType());
                            //Console.WriteLine(BidReader["Quantity"].GetType());
                            //Console.WriteLine(BidReader["TurnsInPool"].GetType());
                            bids.Add(new BidsAndOffers((bool)BidReader["Type"], (DateTime)BidReader["TimePlaced"], (double)BidReader["Price"], (int)BidReader["User"], (string)BidReader["StockName"], (int)BidReader["Quantity"], (int)BidReader["TurnsInPool"]));
                        }
                        //Grab the offers at this price level for assignment
                        MySqlDataReader OfferReader = threadDataBaseHandler.GetData("SELECT * FROM Pool WHERE Type = 1 AND StockName = '" + s + "' AND Price = " + OfferPriceLevels[i]);
                        List<BidsAndOffers> offers = new List<BidsAndOffers>();
                        while (OfferReader.Read()) {
                            offers.Add(new BidsAndOffers((bool)OfferReader["Type"], (DateTime)OfferReader["TimePlaced"], (double)OfferReader["Price"], (int)OfferReader["User"], (string)OfferReader["StockName"], (int)OfferReader["Quantity"], (int)OfferReader["TurnsInPool"]));
                        }
                        //While there is atleast one offer and one bid available at this price range we allocate them
                        while (bids.Count != 0 && offers.Count != 0) {
                            BidsAndOffers b = bids[0];
                            LMMRound(ref b, ref offers);
                            #region Cleaner
                            foreach (BidsAndOffers o in offers) {
                                if (o.Quantity <= 0) {
                                    TradeManager.RemoveFromPool(o, threadDataBaseHandler);
                                }
                            }
                            offers.RemoveAll((o) => o.Quantity <= 0);
                            bids[0] = b;
                            if (bids[0].Quantity <= 0) {
                                TradeManager.RemoveFromPool(bids[0], threadDataBaseHandler);
                                bids.RemoveAt(0);
                                continue;
                            }
                            #endregion
                            ProRataWithLMM(ref b, ref offers, 5);
                            #region Cleaner
                            foreach (BidsAndOffers o in offers) {
                                if (o.Quantity <= 0) {
                                    TradeManager.RemoveFromPool(o, threadDataBaseHandler);
                                }
                            }
                            offers.RemoveAll((o) => o.Quantity <= 0);
                            bids[0] = b;
                            if (bids[0].Quantity <= 0) {
                                TradeManager.RemoveFromPool(bids[0], threadDataBaseHandler);
                                bids.RemoveAt(0);
                                continue;
                            }
                            #endregion
                            FIFO(ref b, ref offers);
                            #region Cleaner
                            foreach (BidsAndOffers o in offers) {
                                if (o.Quantity <= 0) {
                                    TradeManager.RemoveFromPool(o, threadDataBaseHandler);
                                }
                            }
                            offers.RemoveAll((o) => o.Quantity <= 0);
                            bids[0] = b;
                            if (bids[0].Quantity <= 0) {
                                TradeManager.RemoveFromPool(bids[0], threadDataBaseHandler);
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
            //In the LLM round we assign a selection of all bids at the price level to lead market makers, this help market makers complete orders faster
            for (int i = 0; i < Offers.Count; i++) {
                //Select their LMM percentage from DB
                MySqlDataReader r = threadDataBaseHandler.GetData("SELECT LMM FROM Users WHERE ID = " + Offers[i].User);
                double LMMPercentage = 0;
                while (r.Read()) {
                    LMMPercentage = (double)r["LMM"];
                }
                //Check if they have a percentage
                if (LMMPercentage > 0f) {
                    int LMMAmount = (int)(Bid.Quantity * LMMPercentage);
                    if (LMMAmount > 0) {
                        //If there percentage is more than avilable we clamp it to max
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

        void ProRataWithLMM(ref BidsAndOffers Bid, ref List<BidsAndOffers> Offers, int ProRataMinimumAllocation) {
            //This round assigns offer quantities in relation to the size of the pool
            int TotalQuanityOfOffers = 0;
            int BidQuanity = Bid.Quantity;
            foreach (BidsAndOffers o in Offers) {
                TotalQuanityOfOffers += o.Quantity;
            }
            for (int i = 0; i < Offers.Count; i++) {
                double ProRata = 0;
                if (Offers[i].Quantity != 0) {
                    //This equation gives us the percentage of the total quantity of offer this offer is
                    ProRata = (double)Offers[i].Quantity / (double)TotalQuanityOfOffers;
                }
                int ProRataAmount = (int)(ProRata * BidQuanity);
                //This stop there being a lower amount of stock moved if small value
                //Could increase this value to stop small trades of only 1 stock ect being assigned
                if (ProRataAmount >= ProRataMinimumAllocation) {
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
            //FIFO round simple assigns all remain bids and offers in a First in First Out approach
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

	//This class is used to store the bids and offers from the pool
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
