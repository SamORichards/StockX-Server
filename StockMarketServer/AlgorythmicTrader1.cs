using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace StockMarketServer {
    enum BidOffer { bid, offer }
    public enum Segments { RapidlyRising, Rising, Falling, RapidlyFalling }
    public enum Stance { ShortTermLong, ShortTermShort }
    class AlgorithmsTrader1 : AlgoTrader {
        DateTime LastTurn;
        int UserID;
        string TargetStock;
        public List<StockTurn> StockTurns = new List<StockTurn>();
        List<MarketStance> stances = new List<MarketStance>();

        public double ShortTermShortRequirement { get; private set; }
        public double ShortTermLongRequirement { get; private set; }
        public int MinAmount { get; private set; }
        public int MaxAmount { get; private set; }
        public double Aggression { get; private set; }


        //Trader Description
        //Takes a look at last two cycles
        //establishes trend
        //purchases or sell depending on position eg int or short
        public AlgorithmsTrader1(string target, int ClientID, double ShortTermShortRequirement, double ShortTermLongRequirement, int MinAmount, int MaxAmount, double Aggresion) {
            TargetStock = target;
            UserID = ClientID;
            this.ShortTermShortRequirement = ShortTermShortRequirement;
            this.ShortTermLongRequirement = ShortTermLongRequirement;
            this.MinAmount = MinAmount;
            this.MaxAmount = MaxAmount;
            this.Aggression = Aggression;
        }

        public override void RunTurn() {
            base.RunTurn();
            string s = "SELECT Price FROM Trades WHERE Time > '" + LastTurn.ToString("yyyy-MM-dd HH:mm:ss") + "' AND StockName = '" + TargetStock + "'";
            MySqlDataReader reader = DataBaseHandlerAlgo.GetData(s);
            //Console.WriteLine(s);
            List<double> Trades = new List<double>();
            while (reader.Read()) {
                Trades.Add((double)reader["Price"]);
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
                double AverageLast3Turns = (double)TotalLast3Turns / 3f;
                if (AverageLast3Turns <= ShortTermLongRequirement) {//1.6f
                    stances.Add(new MarketStance(Stance.ShortTermLong, MathsHelper.Lerp(MinAmount, MaxAmount, 1f - (AverageLast3Turns / ShortTermLongRequirement) + Aggression), UserID, TargetStock, this));
                } else if (AverageLast3Turns >= ShortTermShortRequirement) {//2.4f
                    stances.Add(new MarketStance(Stance.ShortTermShort, MathsHelper.Lerp(MinAmount, MaxAmount, (AverageLast3Turns / ShortTermLongRequirement) + Aggression), UserID, TargetStock, this));
                }
            }
        }

        class MarketStance {
            public Stance stance;
            int client;
            double SuccessPrice;
            double FailurePrice;
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
                client = ClientID;
                this.TargetStock = TargetStock;
                StartTime = DateTime.Now;
                double CurrentPrice = DataBaseHandlerAlgo.GetCountDouble("SELECT SUM(CurrentPrice) FROM Stock WHERE StockName = '" + this.TargetStock + "'");
                CurrentPrice = Math.Round(CurrentPrice, 2);
                switch (stance) {
                    case Stance.ShortTermLong:
                        this.Quanity = Quanity;
                        SuccessPrice = CurrentPrice + 0.02f;
                        FailurePrice = CurrentPrice - 0.01f;
                        RequiredTime = 5;
                        ShortTermLong(Quanity, CurrentPrice);
                        break;
                    case Stance.ShortTermShort:
                        this.Quanity = Quanity;
                        SuccessPrice = CurrentPrice - 0.02f;
                        FailurePrice = CurrentPrice + 0.01f;
                        RequiredTime = 5;
                        ShortTermShort(Quanity, CurrentPrice);
                        break;
                }
            }

            private void ShortTermShort(int Quanity, double Price) {
                DataBaseHandlerAlgo.SetData(string.Format("INSERT INTO Pool (Type, Price, User, StockName, Quantity) VALUES ({0}, {1}, {2}, '{3}', {4})", (int)BidOffer.offer, Price, client, TargetStock, Quanity));
                Console.WriteLine("Places short with " + Quanity);
            }

            void ShortTermLong(int Quanity, double Price) {
                DataBaseHandlerAlgo.SetData(string.Format("INSERT INTO Pool (Type, Price, User, StockName, Quantity) VALUES ({0}, {1}, {2}, '{3}', {4})", (int)BidOffer.bid, Price, client, TargetStock, Quanity));
                Console.WriteLine("Places Long with " + Quanity);
            }

            public void RunTurn() {
                double CurrentPrice = DataBaseHandlerAlgo.GetCountDouble("SELECT SUM(CurrentPrice) FROM Stock WHERE StockName = '" + TargetStock + "'");
                TimeSpan TimeTaken = DateTime.Now - StartTime;
                CurrentPrice = Math.Round(CurrentPrice, 2);
                switch (stance) {
                    case Stance.ShortTermLong:
                        if ((CurrentPrice >= SuccessPrice || CurrentPrice < FailurePrice) && !OfferPlaced && TimeTaken.TotalMinutes > RequiredTime) {
                            DataBaseHandlerAlgo.SetData(string.Format("INSERT INTO Pool (Type, Price, User, StockName, Quantity) VALUES ({0}, {1}, {2}, '{3}', {4})", (int)BidOffer.offer, CurrentPrice, client, TargetStock, Quanity));
                            OfferPlaced = true;
                            isCompleted = true;
                            Console.WriteLine("Finished long with " + Quanity);
                        }
                        break;
                    case Stance.ShortTermShort:
                        if ((CurrentPrice <= SuccessPrice || CurrentPrice > FailurePrice) && !OfferPlaced && TimeTaken.TotalMinutes > RequiredTime) {
                            DataBaseHandlerAlgo.SetData(string.Format("INSERT INTO Pool (Type, Price, User, StockName, Quantity) VALUES ({0}, {1}, {2}, '{3}', {4})", (int)BidOffer.bid, CurrentPrice, client, TargetStock, Quanity));
                            OfferPlaced = true;
                            isCompleted = true;
                            Console.WriteLine("Finished short with " + Quanity);
                        }
                        break;

                }
            }
        }
    }

    class StockTurn {
        public double OpeningPrice;
        public double LowPrice;
        public double HighPrice;
        public double ClosePrice;
        public double AveragePrice;
        public Segments Trend;
        public StockTurn(List<double> trades) {
            OpeningPrice = trades[0];
            LowPrice = trades.OrderBy((double t) => t).ToList()[0];
            HighPrice = trades.OrderByDescending((double t) => t).ToList()[0];
            ClosePrice = trades[trades.Count - 1];
            double TotalPrice = 0;
            foreach (double t in trades) {
                TotalPrice += t;
            }
            AveragePrice = TotalPrice / (double)trades.Count;
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
}


