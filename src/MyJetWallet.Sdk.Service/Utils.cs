using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MyJetWallet.Sdk.Service
{
    public static class Utils
    {
        public static byte[] EncodeToSha1(this string str)
        {
            using (SHA1 shA1 = SHA1.Create())
                return shA1.ComputeHash(Encoding.ASCII.GetBytes(str));
        }

        public static string ToJson(object obj)
        {
            if (obj == null)
                return string.Empty;

            return JsonSerializer.Serialize(obj);
        }
    }
}