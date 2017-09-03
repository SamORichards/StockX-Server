using System;

namespace StockMarketServer {
    public class MathsHelper {
        public static int Lerp(int Old, int New, float Percentage) {
            return (int)(Old + ((New - Old) * Percentage));
        }
    }
}