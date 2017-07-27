using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace StockMarketServer {
    class TradeManager {
        public static void CreateTrade(ref BidsAndOffers Bid, ref BidsAndOffers Offer, int Quantity) {
            int StockAvailable = DataBaseHandler.GetCount(string.Format("SELECT COUNT(StockID) FROM StocksInCirculation WHERE StockName = '{0}' AND OwnerID = {1}", Offer.StockName, Offer.User));
            if (Quantity > StockAvailable) {
                Quantity = StockAvailable;
            }
            if (Quantity != 0) {
                string sqlFormattedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                DataBaseHandler.SetData(string.Format("INSERT INTO Trades(Time, StockName, BuyerID, SellerID, Price, Quantity) VALUES('{0}', '{1}', {2}, {3}, {4}, {5})", sqlFormattedDate, Bid.StockName, Bid.User, Offer.User, Offer.Price, Quantity));
                DataBaseHandler.SetData(string.Format("UPDATE StocksInCirculation set OwnerID = {0}, LastTradedPrice = {1} WHERE StockName = '{2}' AND OwnerID = {3} LIMIT {4}", Bid.User, Offer.Price, Offer.StockName, Offer.User, Quantity));
                DataBaseHandler.SetData(string.Format("UPDATE Stock SET VolumeTraded = VolumeTraded + {0} WHERE StockName = '{1}'", Quantity, Offer.StockName));
                DataBaseHandler.SetData(string.Format("UPDATE Users SET Balance = Balance + {0} WHERE ID = {1}", Offer.Price * Quantity, Offer.User));
                DataBaseHandler.SetData(string.Format("UPDATE Users SET Balance = Balance - {0} WHERE ID = {1}", Offer.Price * Quantity, Bid.User));
            }
            Bid.Quantity -= Quantity;
            if (Quantity != 0) {
                Offer.Quantity -= Quantity;
            } else {
                Offer.Quantity = 0;
            }
        }

        public static void RemoveFromPool(BidsAndOffers Trade) {
            DataBaseHandler.SetData(string.Format("DELETE FROM Pool WHERE Type = {0} AND TimePlaced = '{1}' AND Price = {2} AND User = {3} AND StockName = '{4}' AND Quantity = {5}", Trade.Type, Trade.TimePlaced, Trade.Price, Trade.User, Trade.StockName, Trade.Quantity));
        }
    }
}
