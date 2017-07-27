using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockMarketServer {
    class TimingManager {
        static Stopwatch MainTimer = new Stopwatch();
        static void Main(string[] args) {
            DataBaseHandler.StartServer();
            MainTimer.Start();
            int i = 0;
            while (true) {
                if (MainTimer.Elapsed.Seconds > 1) {
                    //Run sever tick
                    Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  Tick Started");
                    StockTicker.RunTicker();
                    Pool.RunPool();
                    Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  Tick Completed");
                    MainTimer.Stop();
                    MainTimer.Reset();
                    MainTimer.Start();
                    i++;
                    if (i > 20) {
                        Console.Clear();
                        i = 0;
;                    }
                }
            }
        }
    }
}
