using System.Text;
using System.Security.Cryptography;
using SpaPrerenderer.Services.Interfaces;

namespace SpaPrerenderer.Services;

public class CryptoService : ICryptoService
{
    public string ComputeStringHash(string data)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        var sb = new StringBuilder();
        for (int i = 0; i < hash.Length; i++) sb.Append(hash[i].ToString("X2"));
        return sb.ToString().ToLower();
    }
}