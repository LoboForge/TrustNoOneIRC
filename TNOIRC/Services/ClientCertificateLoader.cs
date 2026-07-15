namespace LoboForge.TNOIRC.Services;

using System.Security.Cryptography.X509Certificates;

public static class ClientCertificateLoader
{
    public static X509Certificate2 Load(string path, string? password)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Client certificate file not found", path);

        var extension = Path.GetExtension(path).ToLowerInvariant();
        var flags = X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet;

        return extension switch
        {
            ".pfx" or ".p12" => new X509Certificate2(path, password, flags),
            ".pem" or ".crt" or ".cert" => LoadPemCertificate(path, flags),
            _ => throw new NotSupportedException(
                $"Unsupported certificate format '{extension}'. Use .pfx/.p12, or PEM (.pem/.crt/.cert) with a matching .key file.")
        };
    }

    private static X509Certificate2 LoadPemCertificate(string certPath, X509KeyStorageFlags flags)
    {
        var pem = File.ReadAllText(certPath);
        if (pem.Contains("PRIVATE KEY", StringComparison.Ordinal))
            return X509Certificate2.CreateFromPem(pem);

        var keyPath = FindPrivateKeyPath(certPath);
        if (keyPath == null)
        {
            throw new InvalidOperationException(
                $"Certificate '{certPath}' does not include a private key. " +
                "Place a matching .key file alongside it, or use a .pfx/.p12 bundle.");
        }

        return X509Certificate2.CreateFromPemFile(certPath, keyPath);
    }

    private static string? FindPrivateKeyPath(string certPath)
    {
        var directory = Path.GetDirectoryName(certPath) ?? ".";
        var baseName = Path.GetFileNameWithoutExtension(certPath);

        foreach (var candidate in new[]
        {
            Path.Combine(directory, $"{baseName}.key"),
            Path.Combine(directory, "irc.key"),
            Path.Combine(directory, "key.pem"),
            Path.Combine(directory, "private.key")
        })
        {
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }
}
