# 🔐 TNO IRC Client

Welcome to the **TNO IRC Client** — a fully custom-built, hacker-themed, secure-by-default IRC client built with **Blazor** and **.NET**.  
This project was created to **bring modern UI and security practices** to the IRC world while remaining lean, extensible, and blazing fast.

---

## ✨ Features

- 🧠 **Full Blazor Frontend**: Interactive desktop-like UI rendered in the browser with real-time updates.
- 🔒 **TLS & Tor Support**: Seamless integration with **SSL/TLS** and **Tor** (via SOCKS5) for anonymous, encrypted connections.
- 🔐 **Client Certificate Authentication**: SASL EXTERNAL support using pinned client certificates.
- 🧾 **NickServ Registration & WHOIS Tools**: Register, verify, and manage identities with complete IRC protocol transparency.
- 📜 **Raw Command Mode**: Send and inspect raw IRC commands and responses for power users and debugging.
- 🧰 **Modular Command Dispatcher**: Easily extend the protocol handler with your own custom commands.
- 🪟 **Window Manager**: Tabbed windows for each channel, private message, or system console.
- 💻 **Cross-platform**: Runs on any platform supported by .NET and WebAssembly.
- 🧙‍♂️ **Hacker Theme**: Dark, matrix-style design — because IRC should look badass.

---

## 📸 Screenshots

> _"You're in a dark room... connected to an IRC server... over Tor... with cert-based auth... this is not your grandpa’s IRC client."_  
![Demo](https://www.loboforge.com/TNO.IRC.png)

---

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- (Optional) [Tor Browser](https://www.torproject.org/) or Tor daemon running (SOCKS5 on `127.0.0.1:9150`)
- Valid IRC client certificate (see below)

---

### 🔧 Setup & Run

```bash
git clone https://github.com/yourname/tno-irc-client.git
cd tno-irc-client
dotnet run
```

Browse to: [https://localhost:5001](https://localhost:5001)

---

### 🔐 Certificate Auth Instructions

1. Generate your certificate:

```bash
openssl req -x509 -newkey rsa:4096 -keyout irc-client.key -out irc-client.crt -days 365 -nodes -subj "/CN=YourNick"
openssl pkcs12 -export -out irc-client.pfx -inkey irc-client.key -in irc-client.crt
```

2. Add your fingerprint to NickServ:

Connect without Tor first and send:

```
/msg NickServ CERT ADD <your sha512 fingerprint>
```

3. Reconnect via Tor using `SASL EXTERNAL` and certificate.

---

### 🧠 WHOIS Verification

Use the built-in `/whois` command to verify your identity and certificate status:

```
/whois YourNick
```

Look for `is logged in as` and `has client certificate fingerprint`.

---

## 🤝 Contributing

Pull requests are welcome! Please submit features or improvements that align with the secure and minimalist philosophy of this project.

---

## 🧾 License

This project is licensed under the **Attribution-NonCommercial-NoDerivatives 4.0 International (CC BY-NC-ND 4.0)**.  
You're free to use it, share it, and study it — but always credit the author, don’t resell it, and don’t pass off modified versions as your own.

See: [https://creativecommons.org/licenses/by-nc-nd/4.0/](https://creativecommons.org/licenses/by-nc-nd/4.0/)

---

## 👤 Author

**LoboForge**  
Crafted with passion and paranoia.

