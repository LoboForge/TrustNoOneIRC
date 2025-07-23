namespace TNO.mIRC.Models
{
    public class IrcServerProfile
    {
        public string Name { get; set; } = "Libera";
        public string Host { get; set; } = "irc.libera.chat";
        public int Port { get; set; } = 6697;
        public string Nick { get; set; } = "LoboForge";
        public string User { get; set; } = "mIRCUser";
        public bool UseTor { get; set; } = false;
        public bool UseTls { get; set; } = true;
        public bool UseSasl { get; set; } = false;
        public string SaslUsername { get; set; } = "";
        public string SaslPassword { get; set; } = "";
        public string? ClientCertPath { get; set; }
        public string? ClientCertPassword { get; set; }
        public bool UseClientCert { get; set; }
    }


}
