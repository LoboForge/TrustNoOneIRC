public class IrcConnectionOptions
{
    public bool UseTor { get; set; } = false;
    public bool UseTls { get; set; } = false;
    public bool UseSasl { get; set; } = false;
    public string? SaslUsername { get; set; }
    public string? SaslPassword { get; set; }
    public string? ClientCertPath { get; set; }
    public string? ClientCertPassword { get; set; }
}
