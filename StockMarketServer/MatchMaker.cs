using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace StockMarketServer {
    class MatchMaker {
        public static void RunMatchMaker() {
            MySqlDataReader reader = DataBaseHandler.GetData("SELECT DISTINCT StockName FROM Pool");
            List<string> StocksInPool = new List<string>();
            while (reader.Read()) {
                StocksInPool.Add((string)reader["StockName"]);
            }
            foreach (string s in StocksInPool) {
                MySqlDataReader BidPoolReader = DataBaseHandler.GetData("SELECT DISTINCT Price FROM Pool WHERE Type = 0 AND StockName = '" + s + "' ORDER BY Price ASC");
                List<double> BidPriceLevels = new List<double>();
                while (BidPoolReader.Read()) {
                    BidPriceLevels.Add((double)BidPoolReader["Price"]);
                }
                if (BidPriceLevels.Count == 0) {
                    continue;
                }
                MySqlDataReader OfferPoolReader = DataBaseHandler.GetData("SELECT DISTINCT Price FROM Pool WHERE Type = 1 AND StockName = '" + s + "' ORDER BY Price DSC");
                List<double> OfferPriceLevels = new List<double>();
                while (OfferPoolReader.Read()) {
                    OfferPriceLevels.Add((double)BidPoolReader["Price"]);
                }
                if (OfferPriceLevels.Count == 0) {
                    continue;
                }
                foreach (double BidPrice in BidPriceLevels) {
                    for (int i = 0; i < OfferPriceLevels.Count; i++) {
                        if (OfferPriceLevels[i] <= BidPrice) {
                            MySqlDataReader BidReader = DataBaseHandler.GetData("SELECT * FROM Pool WHERE Type = 0 AND StockName = '" + s + "' AND Price = " + BidPrice);
                            List<BidsAndOffers> bids = new List<BidsAndOffers>();
                            while (BidReader.Read()) {
                                bids.Add(new BidsAndOffers((int)BidReader["Type"], (string)BidReader["TimePlaced"], (double)BidReader["Price"], (int)BidReader["User"], (string)BidReader["StockName"], (int)BidReader["Quantity"], (int)BidReader["TurnsInPool"]));
                            }
                            MySqlDataReader OfferReader = DataBaseHandler.GetData("SELECT * FROM Pool WHERE Type = 1 AND StockName = '" + s + "' AND Price = " + OfferPriceLevels[i]);
                            List<BidsAndOffers> offers = new List<BidsAndOffers>();
                            while (OfferReader.Read()) {
                                offers.Add(new BidsAndOffers((int)OfferReader["Type"], (string)OfferReader["TimePlaced"], (double)OfferReader["Price"], (int)OfferReader["User"], (string)OfferReader["StockName"], (int)OfferReader["Quantity"], (int)OfferReader["TurnsInPool"]));
                            }
                            while (bids.Count != 0 && offers.Count != 0) {
                                BidsAndOffers b = bids[0];
                                LMMRound(ref b, ref offers);

                            }
                        }
                    }
                }
            }
        }

        private static void LMMRound(ref BidsAndOffers b, ref List<BidsAndOffers> offers) {
            for (int i = 0; i < offers.Count; i++) {
                double LMMPercentage = (double) DataBaseHandler.GetData("SELECT LMM FROM Users WHERE ID = " + offers[i].User)["LMM"];
                if (LMMPercentage > 0f) {
                    int LMMAmount = (int)(b.Quantity * LMMPercentage);
                    if (LMMAmount > offers[i].Quantity) {
                        BidsAndOffers offer = offers[i];
                        TradeManager.CreateTrade(ref b, ref offer, offer.Quantity);
                        offers[i] = offer;
                    } else {
                        BidsAndOffers offer = offers[i];
                        TradeManager.CreateTrade(ref b, ref offer, LMMAmount);
                        offers[i] = offer;
                    }
                }
            }
        }

        void ProRataWithLMM() {

        }
    }
    class BidsAndOffers {
        public int Type;
        public string TimePlaced;
        public double Price;
        public int User;
        public string StockName;
        public int Quantity;
        public int TurnsInPool;
        public BidsAndOffers(int type, string timePlace, double price, int user, string stockName, int quantity, int turnsInPool) {
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
