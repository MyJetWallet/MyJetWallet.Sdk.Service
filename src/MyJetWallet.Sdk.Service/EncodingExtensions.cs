using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MyJetWallet.Sdk.Service;

public static class EncodingExtensions
{
    public static string EncryptStringAes(this string str, string keyStr)
    {
        if(string.IsNullOrEmpty(str))
            return str;
            
        var key = Encoding.UTF8.GetBytes(keyStr);
        var data = Encoding.UTF8.GetBytes(str);

        var result = Encode(data, key);

        return result.ToHexString();
    }

    public static string DecryptStringAes(this string str, string keyStr)
    {
        if(string.IsNullOrEmpty(str))
            return str;
            
        var key = Encoding.UTF8.GetBytes(keyStr);
        var data = str.HexStringToByteArray();

        return Encoding.UTF8.GetString(Decode(data, key));
    }
    
    public static string EncryptStringRsa(this string str, string pubKey)
    {
        if(string.IsNullOrEmpty(str) || string.IsNullOrEmpty(pubKey))
            return str;
            
        pubKey = pubKey.Replace("-----BEGIN PUBLIC KEY-----", "");
        pubKey = pubKey.Replace("-----END PUBLIC KEY-----", "");
        pubKey = pubKey.Replace("-----BEGIN RSA PUBLIC KEY-----", "");
        pubKey = pubKey.Replace("-----END RSA PUBLIC KEY-----", "");
        pubKey = pubKey.Replace("\n", "");    
            
        using var rsa = new RSACryptoServiceProvider();
        var key = Convert.FromBase64String(pubKey);
        rsa.ImportRSAPublicKey(key, out _);
        var decryptedBytes = Encoding.UTF8.GetBytes(str);
        var encryptedBytes = rsa.Encrypt(decryptedBytes, false);
        return Convert.ToBase64String(encryptedBytes);
    }

    public static string DecryptStringRsa(this string base64Str, string privKey)
    {
        privKey = privKey.Replace("-----BEGIN PRIVATE KEY-----", "");
        privKey = privKey.Replace("-----END PRIVATE KEY-----", "");
        privKey = privKey.Replace("-----BEGIN RSA PRIVATE KEY-----", "");
        privKey = privKey.Replace("-----END RSA PRIVATE KEY-----", "");
        privKey = privKey.Replace("\n", "");       
            
        var key = Convert.FromBase64String(privKey);
        var rsa = new RSACryptoServiceProvider();
        rsa.ImportRSAPrivateKey(key, out _);
        var encryptedBytes = Convert.FromBase64String(base64Str);
        var decryptedBytes = rsa.Decrypt(encryptedBytes, false);
        return Encoding.UTF8.GetString(decryptedBytes);
    }
    
    public static string EncodeToHexString(byte[] key, string data)
    {
      byte[] numArray = Encode(Encoding.UTF8.GetBytes(data), key);
      StringBuilder stringBuilder = new StringBuilder(numArray.Length * 2);
      foreach (byte num in numArray)
        stringBuilder.AppendFormat("{0:x2}", (object) num);
      return stringBuilder.ToString().ToUpper();
    }

    public static string DecodeHexString(this string data, byte[] key)
    {
      int length = data.Length;
      byte[] combined = new byte[length / 2];
      for (int startIndex = 0; startIndex < length; startIndex += 2)
        combined[startIndex / 2] = Convert.ToByte(data.ToLower().Substring(startIndex, 2), 16);
      return Encoding.Default.GetString(Decode(combined, key));
    }

    private static byte[] Encode(byte[] data, byte[] key)
    {
      SHA512CryptoServiceProvider cryptoServiceProvider = new SHA512CryptoServiceProvider();
      byte[] dst = new byte[24];
      byte[] buffer = key;
      Buffer.BlockCopy((Array) cryptoServiceProvider.ComputeHash(buffer), 0, (Array) dst, 0, 24);
      using (Aes aes = Aes.Create())
      {
        if (aes == null)
          throw new ArgumentException("Parameter must not be null.", "aes");
        aes.Key = dst;
        using (ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
        {
          using (MemoryStream memoryStream1 = new MemoryStream())
          {
            using (CryptoStream destination = new CryptoStream((Stream) memoryStream1, encryptor, CryptoStreamMode.Write))
            {
              using (MemoryStream memoryStream2 = new MemoryStream(data))
                memoryStream2.CopyTo((Stream) destination);
            }
            byte[] array = memoryStream1.ToArray();
            byte[] destinationArray = new byte[aes.IV.Length + array.Length];
            Array.ConstrainedCopy((Array) aes.IV, 0, (Array) destinationArray, 0, aes.IV.Length);
            Array.ConstrainedCopy((Array) array, 0, (Array) destinationArray, aes.IV.Length, array.Length);
            return destinationArray;
          }
        }
      }
    }

    private static byte[] Decode(byte[] combined, byte[] key)
    {
      byte[] numArray1 = new byte[combined.Length];
      SHA512CryptoServiceProvider cryptoServiceProvider = new SHA512CryptoServiceProvider();
      byte[] dst = new byte[24];
      byte[] buffer = key;
      Buffer.BlockCopy((Array) cryptoServiceProvider.ComputeHash(buffer), 0, (Array) dst, 0, 24);
      using (Aes aes = Aes.Create())
      {
        if (aes == null)
          throw new ArgumentException("Parameter must not be null.", "aes");
        aes.Key = dst;
        byte[] destinationArray = new byte[aes.IV.Length];
        byte[] numArray2 = new byte[numArray1.Length - destinationArray.Length];
        Array.ConstrainedCopy((Array) combined, 0, (Array) destinationArray, 0, destinationArray.Length);
        Array.ConstrainedCopy((Array) combined, destinationArray.Length, (Array) numArray2, 0, numArray2.Length);
        aes.IV = destinationArray;
        using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
        {
          using (MemoryStream memoryStream1 = new MemoryStream())
          {
            using (CryptoStream destination = new CryptoStream((Stream) memoryStream1, decryptor, CryptoStreamMode.Write))
            {
              using (MemoryStream memoryStream2 = new MemoryStream(numArray2))
                memoryStream2.CopyTo((Stream) destination);
            }
            return memoryStream1.ToArray();
          }
        }
      }
    }
}

