using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MqttFramework.Code
{
    public sealed class SharedAccessSignatureBuilder
    {
        public static string GetHostNameNamespaceFromConnectionString(string connectionString)
        {
            return GetPartsFromConnectionString(connectionString)["HostName"].Split('.').FirstOrDefault();
        }
        public static string GetSASTokenFromConnectionString(string connectionString, uint hours = 24)
        {
            var parts = GetPartsFromConnectionString(connectionString);
            return GetSASToken(parts["HostName"], parts["SharedAccessKey"], parts.Keys.Contains("SharedAccessKeyName") ? parts["SharedAccessKeyName"] : null, hours);
        }
        public static string GetSASToken(string resourceUri, string key, string keyName = null, uint hours = 24)
        {
            var expiry = GetExpiry(hours);
            string stringToSign = System.Web.HttpUtility.UrlEncode(resourceUri) + "\n" + expiry;
            var signature = SharedAccessSignatureBuilder.ComputeSignature(key, stringToSign);
            var sasToken = keyName == null ?
                String.Format(CultureInfo.InvariantCulture, "SharedAccessSignature sr={0}&sig={1}&se={2}", HttpUtility.UrlEncode(resourceUri), HttpUtility.UrlEncode(signature), expiry) :
                String.Format(CultureInfo.InvariantCulture, "SharedAccessSignature sr={0}&sig={1}&se={2}&skn={3}", HttpUtility.UrlEncode(resourceUri), HttpUtility.UrlEncode(signature), expiry, keyName);
            return sasToken;
        }

        #region Helpers
        public static string ComputeSignature(string key, string stringToSign)
        {
            using (HMACSHA256 hmac = new HMACSHA256(Convert.FromBase64String(key)))
            {
                return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
            }
        }

        public static Dictionary<string, string> GetPartsFromConnectionString(string connectionString)
        {
            return connectionString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Split(new[] { '=' }, 2)).ToDictionary(x => x[0].Trim(), x => x[1].Trim());
        }

        // default expiring = 24 hours
        private static string GetExpiry(uint hours = 24)
        {
            TimeSpan sinceEpoch = DateTime.UtcNow - new DateTime(1970, 1, 1);
            return Convert.ToString((ulong)sinceEpoch.TotalSeconds + 3600 * hours);
        }

        public static DateTime GetDateTimeUtcFromExpiry(ulong expiry)
        {
            return (new DateTime(1970, 1, 1)).AddSeconds(expiry);
        }
        public static bool IsValidExpiry(ulong expiry, ulong toleranceInSeconds = 0)
        {
            return GetDateTimeUtcFromExpiry(expiry) - TimeSpan.FromSeconds(toleranceInSeconds) > DateTime.UtcNow;
        }

        public static string CreateSHA256Key(string secret)
        {
            using (var provider = new SHA256CryptoServiceProvider())
            {
                byte[] keyArray = provider.ComputeHash(UTF8Encoding.UTF8.GetBytes(secret));
                provider.Clear();
                return Convert.ToBase64String(keyArray);
            }
        }

        public static string CreateRNGKey(int keySize = 32)
        {
            byte[] keyArray = new byte[keySize];
            using (var provider = new RNGCryptoServiceProvider())
            {
                provider.GetNonZeroBytes(keyArray);
            }
            return Convert.ToBase64String(keyArray);
        }
        #endregion
    }
}
