using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Epyks_Serwer
{
    public static class ChecksumProvider
    {
        public static string CalculateSHA256(string text, string salt)
        {
            byte[] _text = Encoding.UTF8.GetBytes(text);
            byte[] _salt = Encoding.UTF8.GetBytes(salt);
            SHA256Managed crypt = new SHA256Managed();
            StringBuilder hash = new StringBuilder();
            byte[] crypto = crypt.ComputeHash(_text.Concat(_salt).ToArray(), 0, _text.Length + _salt.Length);
            foreach (byte theByte in crypto)
            {
                hash.Append(theByte.ToString("x2"));
            }
            return hash.ToString();
        }
    }
}
