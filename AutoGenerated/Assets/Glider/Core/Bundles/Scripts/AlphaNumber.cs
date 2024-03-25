namespace Glider.Core.Bundles
{
    public static class AlphaNumber
    {
        private static readonly string[] numberAlpha =
        {
            "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U",
            "V", "W", "X", "Y", "Z",
            "AA", "AB", "AC", "AD", "AE", "AF", "AG", "AH", "AI", "AJ", "AK", "AL", "AM", "AN", "AO", "AP", "AQ",
            "AR", "AS", "AT", "AU", "AV", "AW", "AX", "AY", "AZ",
            "BA", "BB", "BC", "BD", "BE", "BF", "BG", "BH", "BI", "BJ", "BK", "BL", "BM", "BN", "BO", "BP", "BQ",
            "BR", "BS", "BT", "BU", "BV", "BW", "BX", "BY", "BZ",
            "BA", "BB", "BC", "BD", "BE", "BF", "BG", "BH", "BI", "BJ", "BK", "BL", "BM", "BN", "BO", "BP", "BQ",
            "BR", "BS", "BT", "BU", "BV", "BW", "BX", "BY", "BZ",
            "BA", "BB", "BC", "BD", "BE", "BF", "BG", "BH", "BI", "BJ", "BK", "BL", "BM", "BN", "BO", "BP", "BQ",
            "BR", "BS", "BT", "BU", "BV", "BW", "BX", "BY", "BZ",
            "BA", "BB", "BC", "BD", "BE", "BF", "BG", "BH", "BI", "BJ", "BK", "BL", "BM", "BN", "BO", "BP", "BQ",
            "BR", "BS", "BT", "BU", "BV", "BW", "BX", "BY", "BZ",
            "BA", "BB", "BC", "BD", "BE", "BF", "BG", "BH", "BI", "BJ", "BK", "BL", "BM", "BN", "BO", "BP", "BQ",
            "BR", "BS", "BT", "BU", "BV", "BW", "BX", "BY", "BZ"
        };

        public static string ToNumberString(this double value)
        {
            return GetNumberString(value);
        }

        public static string GetNumberString(double value)
        {
            string res;
            if (value < 1000)
            {
                res = value.ToString("F1");
            }
            else
            {
                var str = value.ToString("F0");
                if (str.Length > 3)
                {
                    res = str.Substring(0, 3);
                    var dotPos = ((str.Length - 1) % 3) + 1;
                    if (dotPos < 3)
                        res = res.Insert(dotPos, ".");

                    var n = (str.Length - 4) / 3;
                    res += numberAlpha[n];
                }
                else
                {
                    return str;
                }
            }

            return res;
        }
    }
}