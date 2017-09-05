using System;

namespace StockMarketServer {
    public class MathsHelper {
        public static int Lerp(int Old, int New, double Percentage) {
            int Result = (int)(Old + ((New - Old) * Percentage));
            if (Result < 0) {
                throw new Exception("Quantity less than zero");
            }
            return Result;
        }
    }
}