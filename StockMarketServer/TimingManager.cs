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
            MainTimer.Start();
            while (true) {
                if (MainTimer.Elapsed.Seconds > 10) {
                    //Run sever tick
                    StockTicker.RunTicker();

                    MainTimer.Stop();
                    MainTimer.Reset();
                    MainTimer.Start();
                }
            }
        }
    }
}
