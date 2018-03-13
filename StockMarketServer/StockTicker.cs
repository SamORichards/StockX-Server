using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace StockMarketServer {
	class StockTicker {
		static int LastDay = 0;
		static List<ThreadDataBaseHandler> DataBaseHandlers = new List<ThreadDataBaseHandler>();
		public static void RunTicker(bool PriceHistoryCleaner) {
			if (PriceHistoryCleaner) {
				Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ": Pricing Cleaned");
			}
			//If this is a new day we need to update the daily varaibles in the DB which are used to calculate daily movements of the stocks
			if (DateTime.Now.Day != LastDay) {
				LastDay = DateTime.Now.Day;
				DataBaseHandler.SetData("UPDATE Stock SET OpeningPriceToday = CurrentPrice, HighToday = CurrentPrice, LowToday = CurrentPrice, VolumeTraded = 0");
			}
			//Grab all the stocks from the DB
			List<Stock> stocks = new List<Stock>();
			MySqlDataReader reader = DataBaseHandler.GetData("SELECT StockName, CurrentPrice FROM Stock");
			while (reader.Read()) {
				stocks.Add(new Stock((string)reader["StockName"], (double)reader["CurrentPrice"]));
			}
			int i = 0;
			foreach (Stock s in stocks) {
				//Make sure we have anough ThreadDataBaseHandlers for each of the stocks
				while (i > DataBaseHandlers.Count - 1) {
					DataBaseHandlers.Add(new ThreadDataBaseHandler());
				}
				//Get the total number of bids added to the pool since last turn for this stock
				int NumberOfBids = DataBaseHandler.GetCount("SELECT SUM(Quantity) FROM Pool WHERE Type = 0 AND StockName = '" + s.StockName + "'  AND TurnsInPool = 0");
				//Get the total number of offers added to the pool since last turn for this stock
				int NumberOfOffers = DataBaseHandler.GetCount("SELECT SUM(Quantity) FROM Pool WHERE Type = 1 AND StockName = '" + s.StockName + "'  AND TurnsInPool = 0");
				//Get the total number of stocks in circulation for this stock
				long StocksInCirculation = DataBaseHandler.GetCount("SELECT SUM(Quantity) FROM Inventories WHERE StockName = '" + s.StockName + "'");
				double NewPrice = UpdateStockPrice(s.StockName, s.StartingPrice, NumberOfBids, NumberOfOffers, StocksInCirculation);
				//Check if price has changed, if not there is no need to send the information to the DB
				if (NewPrice != s.StartingPrice) {
					DataBaseHandler.SetData("UPDATE Stock SET CurrentPrice = " + NewPrice + " WHERE StockName = '" + s.StockName + "'");
				}
				//Update daily movements variables
				DataBaseHandler.SetData("UPDATE Stock SET LowToday = CurrentPrice WHERE CurrentPrice < LowToday");
				DataBaseHandler.SetData("UPDATE Stock SET HighToday = CurrentPrice WHERE CurrentPrice > HighToday");
				//Insert the price into the PricingHistory for the graphs the users can see in the trading application
				DataBaseHandler.SetData("INSERT INTO PricingHistory (Price, StockName) VALUES (" + NewPrice + ", '" + s.StockName + "')");
				if (PriceHistoryCleaner) {
					//PricingThinner reduces the frequency of data for a stock the older the data is in order to keep the DB at a reasonable size
					//Grab an instance of the threadDataBaseHandler
					ThreadDataBaseHandler threadDataBaseHandler = DataBaseHandlers[i];
					//Pass the this Task.Factory to be assign a thread
					Task.Factory.StartNew(() => PricingThinner(s.StockName, threadDataBaseHandler));
					//Restart the timmer for Timing Manager
					TimingManager.PricingTimer.Restart();
				}
				i++;
			}
		}

		/// <summary>
		/// Reduces the frequency of data for a stock the older the data is in order to keep the DB at a reasonable size
		/// </summary>
		static void PricingThinner(string StockName, ThreadDataBaseHandler threadDataBaseHandler) {
			MySqlDataReader reader = threadDataBaseHandler.GetData(string.Format("SELECT Time From PricingHistory WHERE StockName = '{0}' AND Time < '{1}' ORDER BY Time ASC", StockName, DateTime.Now.AddSeconds(-10).ToString("yyyy-MM-dd HH:mm:ss")));
			List<DateTime> LastHour = new List<DateTime>();
			List<DateTime> Last12Hours = new List<DateTime>();
			List<DateTime> LastDay = new List<DateTime>();
			List<DateTime> LastWeek = new List<DateTime>();
			List<DateTime> LastMonth = new List<DateTime>();
			List<DateTime> Longer = new List<DateTime>();

			while (reader.Read()) {
				DateTime time = (DateTime)reader["Time"];
				if (time > DateTime.Now.AddHours(-1)) {
					LastHour.Add(time);
				} else if (time > DateTime.Now.AddHours(-12)) {
					Last12Hours.Add(time);
				} else if (time > DateTime.Now.AddDays(-1)) {
					LastDay.Add(time);
				} else if (time > DateTime.Now.AddDays(-7)) {
					LastWeek.Add(time);
				} else if (time > DateTime.Now.AddMonths(-1)) {
					LastMonth.Add(time);
				} else {
					Longer.Add(time);
				}
			}
			//For each time period there is a set gap which entries need to be apart by.
			//If the gap is not the right size then the entry is removed from the DB
			List<DateTime> ToBeDeleted = new List<DateTime>();
			DateTime last = DateTime.MinValue;
			for (int i = 0; i < LastHour.Count; i++) {
				if (i == 0) {
					last = LastHour[i];
					continue;
				}
				if ((LastHour[i] - last).Seconds < 10) {
					ToBeDeleted.Add(LastHour[i]);
				} else {
					last = LastHour[i];
				}
			}
			last = DateTime.MinValue;
			for (int i = 0; i < Last12Hours.Count; i++) {
				if (i == 0) {
					last = Last12Hours[i];
					continue;
				}
				if ((Last12Hours[i] - last).Minutes < 2) {
					ToBeDeleted.Add(Last12Hours[i]);
				} else {
					last = Last12Hours[i];
				}
			}
			last = DateTime.MinValue;
			for (int i = 0; i < LastDay.Count; i++) {
				if (i == 0) {
					last = LastDay[i];
					continue;
				}
				if ((LastDay[i] - last).Minutes < 10) {
					ToBeDeleted.Add(LastDay[i]);
				} else {
					last = LastDay[i];
				}
			}
			last = DateTime.MinValue;
			for (int i = 0; i < LastWeek.Count; i++) {
				if (i == 0) {
					last = LastWeek[i];
					continue;
				}
				if ((LastWeek[i] - last).Hours < 1) {
					ToBeDeleted.Add(LastWeek[i]);
				} else {
					last = LastWeek[i];
				}
			}
			last = DateTime.MinValue;
			for (int i = 0; i < LastMonth.Count; i++) {
				if (i == 0) {
					last = LastMonth[i];
					continue;
				}
				if ((LastMonth[i] - last).Hours < 6) {
					ToBeDeleted.Add(LastMonth[i]);
				} else {
					last = LastMonth[i];
				}
			}
			last = DateTime.MinValue;
			for (int i = 0; i < Longer.Count; i++) {
				if (i == 0) {
					last = Longer[i];
					continue;
				}
				if ((Longer[i] - last).Days < 7) {
					ToBeDeleted.Add(Longer[i]);
				} else {
					last = Longer[i];
				}
			}
			for (int i = 0; i < ToBeDeleted.Count; i++) {
				threadDataBaseHandler.SetData("DELETE FROM PricingHistory WHERE StockName = '" + StockName + "' AND Time = '" + ToBeDeleted[i].ToString("yyyy-MM-dd HH:mm:ss") + "'");
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
			//Change in price is calculated by the formula discused in the algorithm part of my design section
			double ChangeInPrice = (((double)(numOfBuyers - numOfOffers) / (double)totalStocksInCirculation)) * startPrice;
			startPrice += ChangeInPrice;
			//Console.WriteLine("New Price for " + StockName + " is " + startPrice);
			return startPrice;
		}
	}
}
