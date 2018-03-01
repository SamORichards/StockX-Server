namespace StockMarketServer {
	//Base class for Algorythmic Traders
    public class AlgoTrader {
        public int ID;
        public int UserID;
        public virtual void RunTurn() { }
    }
}