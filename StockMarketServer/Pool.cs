using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockMarketServer {
    class Pool {
        public static void RunPool() {
            MatchMaker.RunMatchMaker();
            DataBaseHandler.SetData("UPDATE Pool SET TurnsInPool = TurnsInPool + 1");
        }
    }
}
