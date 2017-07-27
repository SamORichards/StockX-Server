using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockMarketServer {
    class Pool {
        public static void RunPool() {

            DataBaseHandler.SetData("DELETE FROM Pool WHERE TurnsInPool = 50");
            MatchMaker.RunMatchMaker();
            DataBaseHandler.SetData("UPDATE Pool SET TurnsInPool = TurnsInPool + 1");
        }
    }
}
