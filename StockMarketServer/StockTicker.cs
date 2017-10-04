using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace StockMarketServer {
    class StockTicker {
        static int LastDay = 0;
        public static void RunTicker(bool SavePrices) {
            if (SavePrices) {
                Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ": Pricing Saved");
            }
            if (DateTime.Now.Day != LastDay) {
                LastDay = DateTime.Now.Day;
                DataBaseHandler.SetData("UPDATE Stock SET OpeningPriceToday = CurrentPrice, HighToday = CurrentPrice, LowToday = CurrentPrice, VolumeTraded = 0");
            }
            List<Stock> stocks = new List<Stock>();
            MySqlDataReader reader = DataBaseHandler.GetData("SELECT StockName, CurrentPrice FROM Stock");
            while (reader.Read()) {
                stocks.Add(new Stock((string)reader["StockName"], (double)reader["CurrentPrice"]));
            }
            foreach (Stock s in stocks) {
                int NumberOfBids = DataBaseHandler.GetCount("SELECT SUM(Quantity) FROM Pool WHERE Type = 0 AND StockName = '" + s.StockName + "'  AND TurnsInPool = 0");
                int NumberOfOffers = DataBaseHandler.GetCount("SELECT SUM(Quantity) FROM Pool WHERE Type = 1 AND StockName = '" + s.StockName + "'  AND TurnsInPool = 0");
                long StocksInCirculation = DataBaseHandler.GetCount("SELECT SUM(Quantity) FROM Inventories WHERE StockName = '" + s.StockName + "'");
                double NewPrice = UpdateStockPrice(s.StockName, s.StartingPrice, NumberOfBids, NumberOfOffers, StocksInCirculation);
                if (NewPrice != s.StartingPrice) {
                    if (NewPrice < 0) { NewPrice = 0; }
                    DataBaseHandler.SetData("UPDATE Stock SET CurrentPrice = " + NewPrice + " WHERE StockName = '" + s.StockName + "'");
                }
                DataBaseHandler.SetData("UPDATE Stock SET LowToday = CurrentPrice WHERE CurrentPrice < LowToday");
                DataBaseHandler.SetData("UPDATE Stock SET HighToday = CurrentPrice WHERE CurrentPrice > HighToday");
                if (SavePrices) {
                    DataBaseHandler.SetData("INSERT INTO PricingHistory (Price, StockName) VALUES (" + NewPrice + ", '" + s.StockName + "')");
                    TimingManager.PricingTimer.Restart();
                }
            }
        }
        class Stock {
            public string StockName;
            public double StartingPrice;
            public Stock(string name, double price) {
                StockName = name;
                StartingPrice = price;
            }
        }

        private static double UpdateStockPrice(string StockName, double startPrice, int numOfBuyers, int numOfOffers, long totalStocksInCirculation) {
            //Console.WriteLine("{0}: The Number Of Bids is {1}, and the Number Of Offers is: {2}", StockName, numOfBuyers, numOfOffers);
            double ChangeInPrice = (((double)(numOfBuyers - numOfOffers) / (double)totalStocksInCirculation) / 100) * startPrice;
            startPrice += ChangeInPrice;
            //Console.WriteLine("New Price for " + StockName + " is " + startPrice);
            return startPrice;
        }
    }
}
