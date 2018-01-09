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

        public static void AddJob(BidsAndOffers bidAndOffer) {
            Queue.Add(new Job(JobType.Bid, bidAndOffer));
        }

        public static void RunTurn() {
            TurnCounter++;
            if (TurnCounter == 10) {
                CheckStockLevels();
                TurnCounter = 0;
            }
            Console.WriteLine("Items in Queue is " + Queue.Count);
            for (int i = 0; i < Queue.Count; i++) {
                Queue[i].TurnsInPool++;
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
            int NumberOfStocksOwned = DataBaseHandler.GetCount("SELECT SUM(Quantity) FROM Inventories WHERE UserID = " + clientID + " AND StockName = '" + job.bidAndOffer.StockName + "'");
            int AlreadySelling = DataBaseHandler.GetCount("SELECT COUNT(Quantity) FROM Pool WHERE Type = 1 AND User = " + clientID + " AND StockName = '" + job.bidAndOffer.StockName + "'");
            int StocksAvailable = NumberOfStocksOwned - AlreadySelling;
            if (job.Quanity <= StocksAvailable) {
                BidsAndOffers offer = new BidsAndOffers(true, DateTime.Now, job.bidAndOffer.Price, clientID, job.bidAndOffer.StockName, job.Quanity, 0);
                TradeManager.CreateTrade(ref offer, ref job.bidAndOffer, job.Quanity, threadDataBaseHandler);
                Queue.Remove(job);
            } else {
                //TODO: Count how many times takes and maybe cancel transaction if can't be completed
                double Price = DataBaseHandler.GetCountDouble("SELECT SUM(CurrentPrice) FROM Stock WHERE StockName = '" + job.bidAndOffer.StockName + "'");
                DataBaseHandler.SetData(string.Format("INSERT INTO Pool (Type, Price, User, StockName, Quantity) VALUES ({0}, {1}, {2}, '{3}', {4})", (int)BidOffer.bid, Math.Round(Price, 2) + 1.5, clientID, job.bidAndOffer.StockName, job.Quanity - StocksAvailable));
                //Console.WriteLine(job.Quanity - StocksAvailable);
                BidsAndOffers offer = new BidsAndOffers(true, DateTime.Now, job.bidAndOffer.Price, clientID, job.bidAndOffer.StockName, StocksAvailable, 0);
                TradeManager.CreateTrade(ref offer, ref job.bidAndOffer, job.Quanity - StocksAvailable, threadDataBaseHandler);
            }
        }

        static void RunOfffer(Job job) {
            BidsAndOffers bid = new BidsAndOffers(false, DateTime.Now, job.bidAndOffer.Price, clientID, job.bidAndOffer.StockName, job.Quanity, 0);
            TradeManager.CreateTrade(ref job.bidAndOffer, ref bid, job.Quanity, threadDataBaseHandler);
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
