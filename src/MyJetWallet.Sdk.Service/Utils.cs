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

        public static string ToJson(this object obj) => ToJson(obj, false);

        public static string ToJson(this object obj, bool indented)
        {
            if (obj == null)
                return string.Empty;

            var options = new JsonSerializerOptions
            {
                WriteIndented = indented
            };
            
            return JsonSerializer.Serialize(obj, options);
        }
        
        public static T FromJson<T>(this string json) where T: class
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            return JsonSerializer.Deserialize<T>(json);
        }

        public static string ToReadableString(this decimal value) => value.ToString("F10").TrimEnd('0').TrimEnd(',', '.');
    }
}