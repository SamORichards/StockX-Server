using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace StockMarketServer {
    class TradeManager {
        public static void CreateTrade(ref BidsAndOffers Bid, ref BidsAndOffers Offer, int Quantity) {
            string sqlFormattedDate = DateTime.Now.Date.ToString("yyyy-MM-dd HH:mm:ss");
            DataBaseHandler.SetData(string.Format("INSERT INTO Trades(Time, BuyerID, SellerID, Price, Quantity) VALUES('{0}', {1}, {2}, {3}, {4})", sqlFormattedDate, Bid.User, Offer.User, Offer.Price, Quantity));
            MySqlDataReader reader = DataBaseHandler.GetData(string.Format("SELECT StockID FROM StocksInCirculation WHERE StockName = '{0} AND OwnerID = {1}'", Offer.StockName, Offer.User));
            List<int> StockIDs = new List<int>();
            while (reader.Read()) {
                StockIDs.Add((int)reader["StockID"]);
            }
            for (int i = 0; i < Quantity; i++) {
                DataBaseHandler.SetData(string.Format("UPDATE StocksInCirculation set OwnerID = {0}, LastTradedPrice = {1} WHERE StockID = {2}", Bid.User, Offer.Price, StockIDs[i]));
            }
            DataBaseHandler.SetData(string.Format("UPDATE Users SET Balance = Balance + {0} WHERE ID = {1}", Offer.Price, Offer.User));
            DataBaseHandler.SetData(string.Format("UPDATE Users SET Balance = Balance - {0} WHERE ID = {1}", Offer.Price, Bid.User));
            DataBaseHandler.SetData(string.Format("UPDATE Stock SET VolumeTraded = VolumeTraded + {0} WHERE StockName = '{1}'", Quantity, Offer.StockName));
            Bid.Quantity -= Quantity;
            Offer.Quantity -= Quantity;
        }

        public static void RemoveFromPool(BidsAndOffers Trade) {
            DataBaseHandler.SetData(string.Format("DELETE FROM Pool WHERE Type = {0} AND TimePlace = '{1}' AND Price = {2} AND User = {3} AND StockName = '{4}' AND Quantity = {5}", Trade.Type, Trade.TimePlaced, Trade.Price, Trade.User, Trade.StockName, Trade.Quantity));
        }
    }
}
