using System.Security.Cryptography;
using System.Text;

namespace LoboForge.TNOIRC.Services;

public static class ScramSha256Helper
{
    public sealed class ScramSession
    {
        public required string ClientFirstBare { get; init; }
        public required string ClientNonce { get; init; }
        public string? ServerFirst { get; set; }
        public string? ServerNonce { get; set; }
        public byte[]? Salt { get; set; }
        public int Iterations { get; set; }
    }

    public static ScramSession Start(string username)
    {
        var clientNonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(18)).TrimEnd('=');
        var clientFirstBare = $"n={Escape(username)},r={clientNonce}";
        return new ScramSession
        {
            ClientNonce = clientNonce,
            ClientFirstBare = clientFirstBare
        };
    }

    public static string ClientFirstMessage(ScramSession session) => $"n,,{session.ClientFirstBare}";

    public static void ParseServerFirst(ScramSession session, string serverFirstMessage)
    {
        session.ServerFirst = serverFirstMessage;
        var parts = serverFirstMessage.Split(',');
        foreach (var part in parts)
        {
            if (part.StartsWith("r=", StringComparison.Ordinal))
                session.ServerNonce = part[2..];
            else if (part.StartsWith("s=", StringComparison.Ordinal))
                session.Salt = Convert.FromBase64String(part[2..]);
            else if (part.StartsWith("i=", StringComparison.Ordinal))
                session.Iterations = int.Parse(part[2..]);
        }

        if (session.ServerNonce == null || !session.ServerNonce.StartsWith(session.ClientNonce, StringComparison.Ordinal))
            throw new InvalidOperationException("SCRAM server nonce mismatch.");
    }

    public static string BuildClientFinal(ScramSession session, string password)
    {
        var clientFinalWithoutProof = $"c=biws,r={session.ServerNonce}";
        var authMessage = $"{session.ClientFirstBare},{session.ServerFirst},{clientFinalWithoutProof}";

        var saltedPassword = Hi(Normalize(password), session.Salt!, session.Iterations);
        var clientKey = Hmac(saltedPassword, "Client Key");
        var storedKey = Sha256(clientKey);
        var clientProof = Xor(clientKey, Hmac(storedKey, authMessage));

        return $"{clientFinalWithoutProof},p={Convert.ToBase64String(clientProof)}";
    }

    private static byte[] Hi(string password, byte[] salt, int iterations)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(32);
    }

    private static byte[] Hmac(byte[] key, string data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }

    private static byte[] Sha256(byte[] data)
    {
        return SHA256.HashData(data);
    }

    private static byte[] Xor(byte[] a, byte[] b)
    {
        var result = new byte[a.Length];
        for (var i = 0; i < a.Length; i++)
            result[i] = (byte)(a[i] ^ b[i]);
        return result;
    }

    private static string Escape(string value) =>
        value.Replace("=", "=3D", StringComparison.Ordinal).Replace(",", "=2C", StringComparison.Ordinal);

    private static string Normalize(string password) =>
        password.Normalize(NormalizationForm.FormKC);
}
