// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("kHqX2g385cRh1HCXUBFez55SUhs83E7ejObhbq9enjwzutWzgzig/pzFF94ffM3Zp8EFvlBMwEAMWGPkFUhoqu5YjDyIHHBEZWhSh1aWUdjkXapcoSQYPjMVrOZzxc4bCcTVSw1044hlCRtVox3kvQjtcMSKn4eiVuRnRFZrYG9M4C7gkWtnZ2djZmW9FtZpAFM3174UxCZabw/kYoNQfGx85HP/uZ/FbmvXJEzHC4LpJ+USFIE6mhY2NTSZLpi9ughx/CxCZXXkZ2lmVuRnbGTkZ2dm9CYzGqu1UqKxIqxQoTvxZ3LkYG8Duokk01bPO8xo6I3VTo2Nwas1Sqko63BLe+ui+qb33ckOrzKebN5Ypdny9nx4BrVXI0o1Eu4qB2RlZ2Zn");
        private static int[] order = new int[] { 12,2,11,3,5,8,12,13,12,12,11,13,12,13,14 };
        private static int key = 102;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
