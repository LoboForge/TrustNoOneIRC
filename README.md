# 🔐 TNO IRC Client

Welcome to the **TNO IRC Client** — a fully custom-built, hacker-themed, secure-by-default IRC client built on **Blazor** and **Electron.NET**.  
This project brings a modern UI, hardened opsec, and the classic power of IRC to the desktop — **no browser required**.

---

## ✨ Features

- 🧠 **Blazor + Electron.NET**: Runs as a standalone desktop app — cross-platform and offline-capable.
- 🔒 **Tor Support**: Automatically connects over Tor using built-in SOCKS5 proxy support.
- 🪪 **Client Certificate Auth (SASL EXTERNAL)**: Authenticate using pinned client certificates.
- 🧭 **NickServ & WHOIS Tools**: Inspect identities, verify fingerprints, and automate registration.
- ⚙️ **Raw IRC Mode**: View and inject raw protocol messages like a pro.
- 🧰 **Plugin-Ready Command Dispatcher**: Extend with your own logic using dependency-free bots.
- 🪟 **Multi-Window Tabbed UI**: Each channel, PM, or server console lives in its own tab.
- 🧙‍♂️ **Stylized Hacker Theme**: Matrix-green, smoked glass, and bold lines.  
- 💻 **Cross-Platform**: Fully packaged builds for **Windows** and **Linux (AppImage + Snap)**.
- 📋 **Tor-routed paste & image share**: Upload text/images via HTTPS over Tor — **no DCC** (see below).

---

## 🚫 Why There Is No DCC

**DCC (Direct Client-to-Client) is intentionally not supported** and will not be added.

DCC file and chat transfers are **peer-to-peer**. During a transfer, IRC clients exchange IP addresses and connect directly — bypassing the server and **defeating Tor**. That leaks your real network identity to the remote peer even when IRC itself is tunneled.

TNO IRC is designed for **opsec-first** use:

| Approach | IP exposure | Supported |
|----------|-------------|-----------|
| DCC SEND / CHAT | Direct P2P — **leaks IP** | ❌ Never |
| IRC over Tor + TLS | Hidden via SOCKS | ✅ |
| HTTPS paste/image link over Tor | Hidden via SOCKS | ✅ |

**Instead of DCC**, use the built-in **Paste / Image Share** tool (dock icon) or slash commands:

- Upload **text** → [paste.rs](https://paste.rs) via Tor
- Upload **images** → [0x0.st](https://0x0.st) via Tor
- Post the returned **URL** in channel — recipients fetch over their own connection

Uploads require a running Tor SOCKS proxy (`127.0.0.1:9050` or `9150`). If Tor is not available, uploads are **blocked** rather than falling back to a direct connection that would leak your IP.

### Share commands

```
/paste              Open the share window
/paste some text    Upload text and post the link to the current channel
/share              Same as /paste
```

---

## 📸 Screenshot

![Demo UI](https://www.loboforge.com/TNO.IRC.png)

_"You're in a dark room... connected to an IRC server... over Tor... with cert-based auth... this is not your grandpa’s IRC client."_

---

## 🧪 Try It Now

### 🪟 Windows  
📦 [[Download Windows Build](https://www.loboforge.com/projects/tnoirc)]

### 🐧 Linux  
📦 [[Download AppImage](https://www.loboforge.com/projects/tnoirc)]
📦 [Download Snap Package](https://www.loboforge.com/projects/tnoirc)]

> AppImage: Most common for Linux users  
> Snap: Works great on Ubuntu and Snap-enabled distros

---

## 🚀 Development Setup

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Electron.NET CLI](https://github.com/ElectronNET/Electron.NET)  
  Install via:
  ```bash
  dotnet tool install ElectronNET.CLI -g
  ```

- (Optional) Tor running locally (SOCKS5 at `127.0.0.1:9150`)
- (Optional) A PFX client certificate if using SASL EXTERNAL

---

### 🐧 Linux quick setup (Tor + client cert)

If your IRC certificates live in `/home/wrath/Keys` (or another folder), use the setup script:

```bash
git clone https://github.com/LoboForge/TrustNoOneIRC.git
cd TrustNoOneIRC
./scripts/setup-local-linux.sh
```

The script checks dependencies, builds the solution, and writes `~/.config/LoboForge.TNOIRC/config.json` with a **Libera (local)** profile using your cert.

**Expected key layout** (any one of these in your keys folder):

| Layout | Files |
|--------|--------|
| PKCS#12 (recommended) | `irc.pfx` or `irc.p12` |
| PEM pair | `irc.crt` + `irc.key` (same folder) |

Optional environment variables before running the script:

```bash
export KEYS_DIR=/home/wrath/Keys      # default
export IRC_NICK=wrath                 # default
export IRC_USER=wrath                 # default
export IRC_CERT_PASSWORD=             # only if your .pfx is password-protected
export TOR_SOCKS=9050                   # 9050 = system Tor, 9150 = Tor Browser
./scripts/setup-local-linux.sh
```

**Start the app** after setup:

```bash
cd TNOIRC
export PATH="$PATH:$HOME/.dotnet/tools"
electronize start
```

First connect checklist:

1. Tor running (`sudo systemctl start tor` or Tor Browser open)
2. Connection window → profile **Libera (local)** → Connect
3. New cert? Register with NickServ: `openssl x509 -in /home/wrath/Keys/irc.crt -outform DER | sha512sum` then `/msg NickServ CERT ADD <fingerprint>`

---

### 🔧 Run Locally (manual)

```bash
git clone https://github.com/LoboForge/TrustNoOneIRC.git
cd TrustNoOneIRC/TNOIRC
dotnet tool install ElectronNET.CLI -g
export PATH="$PATH:$HOME/.dotnet/tools"
electronize start
```

This will launch the full app in Electron.

---

### 📦 Build Desktop App

#### Windows:
```bash
electronize build /target win
```

#### Linux (AppImage + Snap):
```bash
electronize build /target linux
```

Built files will appear under `bin/Desktop/`.

> You can distribute the `.AppImage` and `.snap` directly. No need to zip them.

---

## 🔐 Certificate Authentication (SASL EXTERNAL)

There is **no SSH key authentication** in this client. IRC certificate login uses X509 client certificates over TLS with SASL EXTERNAL.

```bash
openssl req -x509 -newkey rsa:4096 -keyout irc.key -out irc.crt -days 365 -nodes -subj "/CN=YourNick"
openssl pkcs12 -export -out irc.pfx -inkey irc.key -in irc.crt
```

Supported certificate formats in the connection profile:

- `.pfx` / `.p12` (recommended) — password optional
- `.crt` / `.cert` / `.pem` — must have a matching `.key` file in the same directory

Then connect with **TLS + SASL + Client Certificate** enabled, and register your fingerprint with:

```
/msg NickServ CERT ADD <your sha512 fingerprint>
```

---

## 🧅 Tor on Linux

The client uses SOCKS5 on `127.0.0.1:9050` (system Tor) or `127.0.0.1:9150` (Tor Browser). It auto-detects an already-running proxy.

If Tor is not running, install and start it:

```bash
sudo apt install tor
sudo systemctl start tor
```

Or start Tor Browser before connecting. The bundled `tor/torrc` is used when the app launches its own Tor process.

---

## 🧠 Tips

- Use `/whois YourNick` to verify that your cert was accepted.
- Use **Paste / Image Share** (dock) to send files without DCC — links only, Tor-routed uploads.
- All bots implement `IBot` and can respond to events or PMs — check the `BotScripts` folder for samples.

---

## 🤝 Contributing

Pull requests are welcome — especially for new bots, modules, or themes.

---

## 🧾 License

Licensed under **CC BY-NC-ND 4.0**  
Use it, fork it, but don't sell it or claim modified versions as your own.

https://creativecommons.org/licenses/by-nc-nd/4.0/

---

## 👤 Author

**LoboForge**  
Built with caffeine and paranoia.  
https://www.loboforge.com
