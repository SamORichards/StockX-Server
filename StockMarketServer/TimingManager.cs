using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StockMarketServer {
    class TimingManager {
        static Stopwatch MainTimer = new Stopwatch();
        public static Stopwatch PricingTimer = new Stopwatch();
        public static Stopwatch TraderTimer = new Stopwatch();
        static void Main(string[] args) {
            DataBaseHandler.StartServer();
            MainTimer.Start();
            PricingTimer.Start();
            TraderTimer.Start();
            int i = 0;
            while (true) {
                if (MainTimer.Elapsed.Seconds > 1) {
                    //Run sever tick
                    if (TraderTimer.Elapsed.Seconds > 30) {
                        Task.Factory.StartNew(() => AlgoTraderManager.RunTrader());
                        Console.WriteLine("Traders Run");
                        TraderTimer.Reset();
                    }
                    Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  Tick Started");
                    StockTicker.RunTicker(PricingTimer.Elapsed.Seconds >= 15);
                    Pool.RunPool();
                    Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  Tick Completed");
                    MainTimer.Stop();
                    MainTimer.Reset();
                    MainTimer.Start();
                    i++;
                    if (i > 20) {
                        Console.Clear();
                        i = 0;
                        ;
                    }
                }
            }
        }
    }
}
