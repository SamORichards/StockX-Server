using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace StockMarketServer {
    class StockTicker {
        public static void RunTicker() {
            List<Stock> stocks = new List<Stock>();
            MySqlDataReader reader = DataBaseHandler.GetData("SELECT StockName, CurrentPrice FROM Pool");
            while (reader.Read()) {
                stocks.Add(new Stock((string)reader["StockName"], (double)reader["CurrentPrice"]));
            }
            foreach (Stock s in stocks) {
                int NumberOfBids = DataBaseHandler.GetCountint("SELECT SUM(Quantity) FROM Pool WHERE Type = 0 AND StockName = " + s.StockName);
                int NumberOfOffers = DataBaseHandler.GetCountint("SELECT SUM(Quantity) FROM Pool WHERE Type = 1 AND StockName = " + s.StockName);
                long StocksInCirculation = DataBaseHandler.GetCount("SELECT COUNT(*) FROM StocksInCirculation WHERE StockName = " + s.StockName);
                UpdateStockPrice(s.StockName, s.StartingPrice, NumberOfBids, NumberOfOffers, StocksInCirculation);
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
            Console.WriteLine("{0}: The Number Of Bids is {1}, and the Number Of Offers is: {2}", StockName, numOfBuyers, numOfOffers);
            double ChangeInPrice = ((double)(numOfBuyers - numOfOffers) / (double)totalStocksInCirculation);
            startPrice += (ChangeInPrice * 10);
            Console.WriteLine("New Price for " + StockName + " is " + startPrice);
            return startPrice;
        }
    }
}
