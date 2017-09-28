using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace StockMarketServer {
    class TradeManager {
        public static void CreateTrade(ref BidsAndOffers Bid, ref BidsAndOffers Offer, int Quantity) {
            int StockAvailable = DataBaseHandler.GetCount(string.Format("SELECT COUNT(Quantity) FROM Inventories WHERE StockName = '{0}' AND UserID = {1}", Offer.StockName, Offer.User));
            if (Quantity > StockAvailable) {
                Quantity = StockAvailable;
            }
            if (Quantity > 0) {
                if (DataBaseHandler.GetCount("SELECT COUNT(UserID) FROM Inventories WHERE UserID = " + Bid.User + " AND StockName = '" + Bid.StockName + "'") == 0) {
                    DataBaseHandler.SetData(string.Format("INSERT INTO Inventories(UserID, StockName, Quantity, LastTradedPrice) VALUES({0}, '{1}', {2}, {3})", Bid.User, Bid.StockName, 0, 0));
                }
                DataBaseHandler.SetData(string.Format("INSERT INTO Trades(StockName, BuyerID, SellerID, Price, Quantity) VALUES('{0}', {1}, {2}, {3}, {4})", Bid.StockName, Bid.User, Offer.User, Offer.Price, Quantity));
                DataBaseHandler.SetData(string.Format("UPDATE Inventories set LastTradedPrice = {0}, Quantity = Quantity - {1} WHERE StockName = '{2}' AND UserID = {3}", Offer.Price, Quantity, Offer.StockName, Offer.User));
                DataBaseHandler.SetData(string.Format("UPDATE Inventories set LastTradedPrice = {0}, Quantity = Quantity + {1} WHERE StockName = '{2}' AND UserID = {3}", Offer.Price, Quantity, Offer.StockName, Bid.User));
                DataBaseHandler.SetData(string.Format("UPDATE Stock SET VolumeTraded = VolumeTraded + {0} WHERE StockName = '{1}'", Quantity, Offer.StockName));
                DataBaseHandler.SetData(string.Format("UPDATE Users SET Balance = Balance + {0} WHERE ID = {1}", Offer.Price * Quantity, Offer.User));
                DataBaseHandler.SetData(string.Format("UPDATE Users SET Balance = Balance - {0} WHERE ID = {1}", Offer.Price * Quantity, Bid.User));
            } else { if (Quantity < 0) { throw new Exception("Fuck dis?"); } }
            if (Quantity != 0) {
                Bid.Quantity -= Quantity;
                if (Quantity != 0) {
                    Offer.Quantity -= Quantity;
                }
                DataBaseHandler.SetData(string.Format("Update Pool set Quantity = {0} WHERE Type = {1} AND TimePlaced = '{2}' AND Price = {3} AND User = {4} AND StockName = '{5}'", Bid.Quantity, Bid.Type, Bid.TimePlaced.ToString("yyyy-MM-dd HH:mm:ss"), Bid.Price, Bid.User, Bid.StockName));
                DataBaseHandler.SetData(string.Format("Update Pool set Quantity = {0} WHERE Type = {1} AND TimePlaced = '{2}' AND Price = {3} AND User = {4} AND StockName = '{5}'", Offer.Quantity, Offer.Type, Offer.TimePlaced.ToString("yyyy-MM-dd HH:mm:ss"), Offer.Price, Offer.User, Offer.StockName));
            } else {
                Offer.Quantity = 0;
            }
        }

        public static void RemoveFromPool(BidsAndOffers Trade) {
            string command = string.Format("DELETE FROM Pool WHERE Type = {0} AND TimePlaced = '{1}' AND Price = {2} AND User = {3} AND StockName = '{4}'", Trade.Type, Trade.TimePlaced.ToString("yyyy-MM-dd HH:mm:ss"), Trade.Price, Trade.User, Trade.StockName);
            DataBaseHandler.SetData(command);
        }
    }
}
