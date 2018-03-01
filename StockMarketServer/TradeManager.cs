using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace StockMarketServer {
	class TradeManager {
		public static void CreateTrade(ref BidsAndOffers Bid, ref BidsAndOffers Offer, int Quantity, ThreadDataBaseHandler threadDataBaseHandler) {
			//Get the total quantity that the user can sell
			int StockAvailable = threadDataBaseHandler.GetCount(string.Format("SELECT COUNT(Quantity) FROM Inventories WHERE StockName = '{0}' AND UserID = {1}", Offer.StockName, Offer.User));
			if (Quantity > StockAvailable) {
				//If the quantity if the sale is greater than that the user can offer limit to the max amount the user could sell
				Quantity = StockAvailable;
			}
			//If the Quantity is not greater than one we can skip trade creation
			if (Quantity > 0) {
				//Checking to see if the bid user has an entry in the iventory table
				if (threadDataBaseHandler.GetCount("SELECT COUNT(UserID) FROM Inventories WHERE UserID = " + Bid.User + " AND StockName = '" + Bid.StockName + "'") == 0) {
					//If not then an entry for the bid user for this stock is inserted into inventories
					threadDataBaseHandler.SetData(string.Format("INSERT INTO Inventories(UserID, StockName, Quantity, LastTradedPrice) VALUES({0}, '{1}', {2}, {3})", Bid.User, Bid.StockName, 0, 0));
				}
				//Update the database reflect the new trade
				threadDataBaseHandler.SetData(string.Format("INSERT INTO Trades(StockName, BuyerID, SellerID, Price, Quantity) VALUES('{0}', {1}, {2}, {3}, {4})", Bid.StockName, Bid.User, Offer.User, Offer.Price, Quantity));
				threadDataBaseHandler.SetData(string.Format("UPDATE Inventories set LastTradedPrice = {0}, Quantity = Quantity - {1} WHERE StockName = '{2}' AND UserID = {3}", Offer.Price, Quantity, Offer.StockName, Offer.User));
				threadDataBaseHandler.SetData(string.Format("UPDATE Inventories set LastTradedPrice = {0}, Quantity = Quantity + {1} WHERE StockName = '{2}' AND UserID = {3}", Offer.Price, Quantity, Offer.StockName, Bid.User));
				threadDataBaseHandler.SetData(string.Format("UPDATE Stock SET VolumeTraded = VolumeTraded + {0} WHERE StockName = '{1}'", Quantity, Offer.StockName));
				threadDataBaseHandler.SetData(string.Format("UPDATE Users SET Balance = Balance + {0} WHERE ID = {1}", Offer.Price * Quantity, Offer.User));
				threadDataBaseHandler.SetData(string.Format("UPDATE Users SET Balance = Balance - {0} WHERE ID = {1}", Offer.Price * Quantity, Bid.User));
			} else if (Quantity < 0) { throw new Exception("Error with trade, quantity is less than 0"); }
			//Checking if trade went ahead
			if (Quantity != 0) {
				//If it did then we are updating are local version of the bids and offfers in the server pool
				//This allows the matchmaking algorythim to continue without having to grab the new trade data
				//from the database
				Bid.Quantity -= Quantity;
				Offer.Quantity -= Quantity;
				//Update database to reflect new trade
				threadDataBaseHandler.SetData(string.Format("Update Pool set Quantity = {0} WHERE Type = {1} AND TimePlaced = '{2}' AND Price = {3} AND User = {4} AND StockName = '{5}'", Bid.Quantity, Bid.Type, Bid.TimePlaced.ToString("yyyy-MM-dd HH:mm:ss"), Bid.Price, Bid.User, Bid.StockName));
				threadDataBaseHandler.SetData(string.Format("Update Pool set Quantity = {0} WHERE Type = {1} AND TimePlaced = '{2}' AND Price = {3} AND User = {4} AND StockName = '{5}'", Offer.Quantity, Offer.Type, Offer.TimePlaced.ToString("yyyy-MM-dd HH:mm:ss"), Offer.Price, Offer.User, Offer.StockName));
			} else {
				//If they couldn't supply any stock to sell then set the offer quantity to be zero
				Offer.Quantity = 0;
			}
			//Close the connection to the DB so that DB connection thread can be open for the next thread
			threadDataBaseHandler.CloseCon();
		}

		public static void RemoveFromPool(BidsAndOffers Trade, ThreadDataBaseHandler threadDataBaseHandler) {
			//When a bid or offer is ready to be removed it passed to this method which removes it from DB
			string command = string.Format("DELETE FROM Pool WHERE Type = {0} AND TimePlaced = '{1}' AND Price = {2} AND User = {3} AND StockName = '{4}'", Trade.Type, Trade.TimePlaced.ToString("yyyy-MM-dd HH:mm:ss"), Trade.Price, Trade.User, Trade.StockName);
			threadDataBaseHandler.SetData(command);
			threadDataBaseHandler.CloseCon();
		}
	}
}
