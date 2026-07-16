#!/usr/bin/env bash
# Local setup for TNO IRC on Linux (Tor + TLS client cert)
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
KEYS_DIR="${KEYS_DIR:-/home/wrath/Keys}"
CONFIG_DIR="${XDG_CONFIG_HOME:-$HOME/.config}/LoboForge.TNOIRC"
CONFIG_FILE="$CONFIG_DIR/config.json"
NICK="${IRC_NICK:-wrath}"
USER_NAME="${IRC_USER:-wrath}"
TOR_SOCKS="${TOR_SOCKS:-9050}"

echo "==> TNO IRC local setup"
echo "    Repo:     $REPO_ROOT"
echo "    Keys:     $KEYS_DIR"
echo "    Config:   $CONFIG_FILE"
echo

if [[ ! -d "$KEYS_DIR" ]]; then
  echo "==> Creating keys directory: $KEYS_DIR"
  if [[ "$KEYS_DIR" == /home/*/* ]]; then
    sudo mkdir -p "$KEYS_DIR" 2>/dev/null || mkdir -p "$KEYS_DIR"
    sudo chown -R "$(whoami):$(id -gn)" "$(dirname "$KEYS_DIR")" 2>/dev/null || true
  else
    mkdir -p "$KEYS_DIR"
  fi
fi

echo "==> Keys in $KEYS_DIR:"
ls -la "$KEYS_DIR" || true
echo

# Detect certificate file (prefer .pfx, then .crt/.cert + .key)
CERT_PATH=""
CERT_PASSWORD="${IRC_CERT_PASSWORD:-}"

for candidate in irc.pfx irc.p12 client.pfx tno.pfx cert.pfx; do
  if [[ -f "$KEYS_DIR/$candidate" ]]; then
    CERT_PATH="$KEYS_DIR/$candidate"
    echo "==> Found PKCS#12 cert: $CERT_PATH"
    break
  fi
done

if [[ -z "$CERT_PATH" ]]; then
  for candidate in irc.crt irc.cert client.crt cert.crt cert.cert; do
    if [[ -f "$KEYS_DIR/$candidate" ]]; then
      base="${candidate%.*}"
      if [[ -f "$KEYS_DIR/$base.key" ]] || [[ -f "$KEYS_DIR/irc.key" ]] || [[ -f "$KEYS_DIR/key.pem" ]]; then
        CERT_PATH="$KEYS_DIR/$candidate"
        echo "==> Found PEM cert + key: $CERT_PATH"
        break
      fi
    fi
  done
fi

if [[ -z "$CERT_PATH" ]]; then
  echo "WARNING: No usable cert found in $KEYS_DIR"
  echo "Expected one of:"
  echo "  - irc.pfx / irc.p12  (recommended)"
  echo "  - irc.crt + irc.key  (same folder)"
  echo "Continuing without client cert — you can add paths in the Connection window later."
fi

# Verify key pair if PEM
if [[ -n "$CERT_PATH" && "$CERT_PATH" != *.pfx && "$CERT_PATH" != *.p12 ]]; then
  base="$(basename "$CERT_PATH" | sed 's/\.[^.]*$//')"
  key=""
  for k in "$KEYS_DIR/$base.key" "$KEYS_DIR/irc.key" "$KEYS_DIR/key.pem"; do
    [[ -f "$k" ]] && key="$k" && break
  done
  if [[ -n "$key" ]]; then
    echo "==> Matching private key: $key"
    if command -v openssl >/dev/null 2>&1; then
      openssl x509 -in "$CERT_PATH" -noout -subject 2>/dev/null || echo "WARN: could not read cert with openssl"
      openssl rsa -in "$key" -check -noout 2>/dev/null || openssl pkey -in "$key" -check -noout 2>/dev/null || echo "WARN: could not verify private key"
    fi
  else
    echo "ERROR: Cert found but no matching .key file in $KEYS_DIR"
    exit 1
  fi
fi

# Dependencies
echo "==> Checking dependencies..."
missing=()
command -v dotnet >/dev/null 2>&1 || missing+=("dotnet-sdk (8.0)")
command -v tor >/dev/null 2>&1 || missing+=("tor")
command -v node >/dev/null 2>&1 || missing+=("nodejs")
command -v npm >/dev/null 2>&1 || missing+=("npm")

if [[ ${#missing[@]} -gt 0 ]]; then
  echo "Install missing packages (Debian/Ubuntu example):"
  echo "  sudo apt update && sudo apt install -y tor dotnet-sdk-8.0 nodejs npm"
  echo "Missing: ${missing[*]}"
  if [[ " ${missing[*]} " == *" dotnet-sdk"* ]]; then
    echo "Or install .NET 8: https://dotnet.microsoft.com/download"
  fi
fi

# Tor (systemd when available, otherwise start tor directly)
socks_up() {
  if command -v ss >/dev/null 2>&1; then
    ss -tln 2>/dev/null | grep -qE ':9050|:9150'
  elif command -v netstat >/dev/null 2>&1; then
    netstat -tln 2>/dev/null | grep -qE ':9050|:9150'
  else
    python3 -c "import socket; s=socket.socket(); r=s.connect_ex(('127.0.0.1',9050)); s.close(); exit(0 if r==0 else 1)" 2>/dev/null
  fi
}

if socks_up; then
  echo "==> SOCKS proxy already listening (9050 or 9150)"
elif command -v systemctl >/dev/null 2>&1 && systemctl is-active --quiet tor 2>/dev/null; then
  echo "==> Tor service: running"
elif command -v tor >/dev/null 2>&1; then
  echo "==> Starting Tor daemon..."
  tor --RunAsDaemon 1 2>/dev/null || sudo systemctl start tor 2>/dev/null || true
  sleep 2
fi

if socks_up; then
  echo "==> SOCKS proxy listening (9050 or 9150)"
else
  echo "WARN: No SOCKS on 9050/9150 — start Tor or Tor Browser before connecting"
fi

# Electron.NET CLI (requires .NET 6 runtime for the global tool)
export PATH="$PATH:$HOME/.dotnet/tools"
if ! dotnet --list-runtimes 2>/dev/null | grep -q "Microsoft.NETCore.App 6."; then
  echo "==> Installing .NET 6 runtime (required by electronize CLI)..."
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  bash /tmp/dotnet-install.sh --runtime dotnet --version 6.0.36
  export PATH="$PATH:$HOME/.dotnet"
fi

if ! command -v electronize >/dev/null 2>&1; then
  echo "==> Installing ElectronNET.CLI..."
  dotnet tool install ElectronNET.CLI -g
fi

# electronize publish fails when TNOIRC/ contains multiple .sln files
if [[ -f "$REPO_ROOT/TNOIRC/TNOIRC.sln" ]]; then
  mv "$REPO_ROOT/TNOIRC/TNOIRC.sln" "$REPO_ROOT/TNOIRC/TNOIRC.sln.bak"
  echo "==> Renamed TNOIRC/TNOIRC.sln → TNOIRC.sln.bak (avoids electronize publish conflict)"
fi

if [[ ! -d "$REPO_ROOT/TNOIRC/node_modules" ]]; then
  echo "==> Installing npm dependencies for Electron shell..."
  (cd "$REPO_ROOT/TNOIRC" && npm install)
fi

# Build
echo "==> Building solution..."
cd "$REPO_ROOT"
dotnet build LoboForge.TNOIRC.sln -c Release

# Write config
mkdir -p "$CONFIG_DIR"

USE_CERT=false
CERT_JSON="null"
CERT_PASS_JSON="null"
if [[ -n "$CERT_PATH" ]]; then
  USE_CERT=true
  CERT_JSON="\"$(echo "$CERT_PATH" | sed 's/\\/\\\\/g')\""
  CERT_PASS_JSON="\"$CERT_PASSWORD\""
fi

cat > "$CONFIG_FILE" <<EOF
{
  "TorExecutablePath": null,
  "TorSocksPort": $TOR_SOCKS,
  "Servers": [
    {
      "Name": "Libera (local)",
      "Host": "irc.libera.chat",
      "Port": 6697,
      "Nick": "$NICK",
      "User": "$USER_NAME",
      "UseTor": true,
      "UseTls": true,
      "UseSasl": true,
      "SaslUsername": "",
      "SaslPassword": "",
      "ServerPassword": null,
      "AutoReconnect": true,
      "UseClientCert": $USE_CERT,
      "ClientCertPath": $CERT_JSON,
      "ClientCertPassword": $CERT_PASS_JSON,
      "AutoJoinChannels": []
    }
  ],
  "AutoReplies": [],
  "AlertRules": []
}
EOF

echo "==> Wrote $CONFIG_FILE"
echo
echo "==> Done. Start the app:"
echo "    cd $REPO_ROOT/TNOIRC"
echo "    export PATH=\"\$PATH:\$HOME/.dotnet/tools:\$HOME/.dotnet\""
echo "    electronize start"
echo
echo "==> In the app:"
echo "    1. Connection window → profile 'Libera (local)' should already have your cert path"
echo "    2. Enable Tor + TLS + Client Certificate"
echo "    3. Connect"
echo
if [[ -n "$CERT_PATH" ]]; then
  echo "==> Register cert fingerprint with NickServ (first time only):"
  echo "    openssl x509 -in \"$CERT_PATH\" -outform DER | sha512sum"
  echo "    /msg NickServ CERT ADD <fingerprint>"
  echo "    /msg NickServ CERT LIST"
fi
