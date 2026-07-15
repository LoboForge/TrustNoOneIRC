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
  echo "ERROR: Keys directory not found: $KEYS_DIR"
  echo "Create it or set KEYS_DIR=/path/to/keys"
  exit 1
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

# Tor
if command -v systemctl >/dev/null 2>&1; then
  if systemctl is-active --quiet tor 2>/dev/null; then
    echo "==> Tor service: running"
  else
    echo "==> Starting Tor..."
    sudo systemctl enable --now tor 2>/dev/null || echo "WARN: could not start tor — run: sudo systemctl start tor"
  fi
fi

if ss -tln 2>/dev/null | grep -qE ':9050|:9150'; then
  echo "==> SOCKS proxy listening (9050 or 9150)"
else
  echo "WARN: No SOCKS on 9050/9150 — start Tor or Tor Browser before connecting"
fi

# Electron.NET CLI
if ! command -v electronize >/dev/null 2>&1; then
  echo "==> Installing ElectronNET.CLI..."
  dotnet tool install ElectronNET.CLI -g
  export PATH="$PATH:$HOME/.dotnet/tools"
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
echo "    export PATH=\"\$PATH:\$HOME/.dotnet/tools\""
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
