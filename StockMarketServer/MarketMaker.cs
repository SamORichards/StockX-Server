using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace StockMarketServer {
	enum JobType { Bid, Offer }
	class MarketMaker {
		static List<Job> Queue = new List<Job>();
		public static int QueueSize { private set { } get { return Queue.Count; } }
		static int clientID = 2;
		static int TurnCounter = 0;
		static ThreadDataBaseHandler threadDataBaseHandler = new ThreadDataBaseHandler();

		//This is called by pool and it simply creates a new instance of Job class
		//which takes the bid / offer which has been taken from the DB to be completed
		//by the market maker as it had been in the pool too long.
		//This instance of Job is then added to Queue
		public static void AddJob(BidsAndOffers bidAndOffer) {
			Queue.Add(new Job((JobType)Convert.ToInt32(bidAndOffer.Type), bidAndOffer));
		}


		/// <summary>
		/// This is called from Timing Manager every tick and is runs through its
		/// queue of Jobs to see if it can complete them by satisfying the bid / offer
		/// </summary>
		public static void RunTurn() {
			TurnCounter++;
			if (TurnCounter == 10) {
				CheckStockLevels();
				TurnCounter = 0;
			}
			Console.WriteLine("Items in Queue is " + Queue.Count);
			//If the Queue is too long it remove the Jobs its been trying to complete
			//for the longest period of time
			while (Queue.Count > 100) {
				Queue = Queue.OrderByDescending((w) => w.TurnsInPool).ToList();
				Queue.RemoveAt(0);
			}
			for (int i = 0; i < Queue.Count; i++) {
				Queue[i].TurnsInPool++;
				//Remove completed jobs
				if (Queue[i].Quanity <= 0) {
					Queue.RemoveAt(i);
					continue;
				}
				switch (Queue[i].jobType) {
					case JobType.Bid:
						RunBid(Queue[i]);
						break;
					case JobType.Offer:
						RunOfffer(Queue[i]);
						break;
				}
			}
			Queue.RemoveAll((Job j) => j.Quanity == 0);
			Queue.RemoveAll((Job j) => j.TurnsInPool == 50);
		}

		private static void CheckStockLevels() {
			MySqlDataReader reader = DataBaseHandler.GetData("SELECT DISTINCT StockName FROM Inventories WHERE UserID = " + clientID);
			List<string> StockNames = new List<string>();
			while (reader.Read()) {
				StockNames.Add((string)reader["StockName"]);
			}
			foreach (string stock in StockNames) {
				int NumberOfStocksOwned = DataBaseHandler.GetCount("SELECT SUM(Quantity) FROM Inventories WHERE UserID = " + clientID + " AND StockName = '" + stock + "'");
				int AlreadySelling = DataBaseHandler.GetCount("SELECT COUNT(Quantity) FROM Pool WHERE Type = 1 AND User = " + clientID + " AND StockName = '" + stock + "'");
				int StocksAvailable = NumberOfStocksOwned - AlreadySelling;
				if (StocksAvailable > 250) {
					double Price = DataBaseHandler.GetCountDouble("SELECT SUM(CurrentPrice) FROM Stock WHERE StockName = '" + stock + "'");
					DataBaseHandler.SetData(string.Format("INSERT INTO Pool (Type, Price, User, StockName, Quantity) VALUES ({0}, {1}, {2}, '{3}', {4})", (int)BidOffer.offer, Math.Round(Price, 2), clientID, stock, StocksAvailable - 250));
				}
				if (StocksAvailable < 150) {
					double Price = DataBaseHandler.GetCountDouble("SELECT SUM(CurrentPrice) FROM Stock WHERE StockName = '" + stock + "'");
					DataBaseHandler.SetData(string.Format("INSERT INTO Pool (Type, Price, User, StockName, Quantity) VALUES ({0}, {1}, {2}, '{3}', {4})", (int)BidOffer.bid, Math.Round(Price, 2), clientID, stock, 150 - StocksAvailable));
				}
			}
		}

		static void RunBid(Job job) {
			//Grab the total stocks the market maker owns of this stock
			int NumberOfStocksOwned = DataBaseHandler.GetCount("SELECT SUM(Quantity) FROM Inventories WHERE UserID = " + clientID + " AND StockName = '" + job.bidAndOffer.StockName + "'");
			//Count how many of these the market maker is already selling
			int AlreadySelling = DataBaseHandler.GetCount("SELECT COUNT(Quantity) FROM Pool WHERE Type = 1 AND User = " + clientID + " AND StockName = '" + job.bidAndOffer.StockName + "'");
			//Calculate the total stock available for the market maker to sell
			int StocksAvailable = NumberOfStocksOwned - AlreadySelling;
			//If it has more stock available than needed to complete the job
			if (job.Quanity <= StocksAvailable) {
				BidsAndOffers offer = new BidsAndOffers(true, DateTime.Now, job.bidAndOffer.Price, clientID, job.bidAndOffer.StockName, job.Quanity, 0);
				//Complete the trade by selling the market makers stock to this user
				TradeManager.CreateTrade(ref offer, ref job.bidAndOffer, job.Quanity, threadDataBaseHandler);
				//Remove job as it now complete
				Queue.Remove(job);
			} else {
				//Market maker does not have enough stock to currently complete this trade so it sells to job as much as it can and puts in a bid into the market to buy more
				//of the stock so that it can fully complete the job
				double Price = DataBaseHandler.GetCountDouble("SELECT SUM(CurrentPrice) FROM Stock WHERE StockName = '" + job.bidAndOffer.StockName + "'");
				DataBaseHandler.SetData(string.Format("INSERT INTO Pool (Type, Price, User, StockName, Quantity) VALUES ({0}, {1}, {2}, '{3}', {4})", (int)BidOffer.bid, Math.Round(Price, 2) + 1.5, clientID, job.bidAndOffer.StockName, job.Quanity - StocksAvailable));
				//Console.WriteLine(job.Quanity - StocksAvailable);
				BidsAndOffers offer = new BidsAndOffers(true, DateTime.Now, job.bidAndOffer.Price, clientID, job.bidAndOffer.StockName, StocksAvailable, 0);
				TradeManager.CreateTrade(ref offer, ref job.bidAndOffer, job.Quanity - StocksAvailable, threadDataBaseHandler);
			}
		}

		static void RunOfffer(Job job) {
			//Purchase the stocks that this job is selling to complete the job
			BidsAndOffers bid = new BidsAndOffers(false, DateTime.Now, job.bidAndOffer.Price, clientID, job.bidAndOffer.StockName, job.Quanity, 0);
			TradeManager.CreateTrade(ref job.bidAndOffer, ref bid, job.Quanity, threadDataBaseHandler);
			//Remove job from queue as it is now complete
			Queue.Remove(job);
		}

	}

	class Job {
		public JobType jobType;
		public BidsAndOffers bidAndOffer;
		public int TurnsInPool = 0;
		public int Quanity {
			get { return bidAndOffer.Quantity; }
		}
		public Job(JobType jobType, BidsAndOffers bidAndOffer) {
			this.jobType = jobType;
			this.bidAndOffer = bidAndOffer;
		}
	}
}
