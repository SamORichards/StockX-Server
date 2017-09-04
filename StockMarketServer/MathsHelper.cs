using System;

namespace StockMarketServer {
    public class MathsHelper {
        public static int Lerp(int Old, int New, double Percentage) {
            return (int)(Old + ((New - Old) * Percentage));
        }
    }
}