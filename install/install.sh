#!/bin/bash

RED='\033[0;31m'
BLUE='\033[0;34m'
GREEN='\033[0;32m'
MAGENTA='\033[0;35m'
RESET='\033[0m'

info() { printf "${MAGENTA}$@${RESET}\n"; }
status() { printf "${BLUE}$@${RESET}\n"; }
success() { printf "${GREEN}$@${RESET}\n"; }
error() { printf "${RED}$@${RESET}\n"; }

# Function to get the latest release from Github and parse their JSON.
fetch_gh_release_latest() {
  local api="https://api.github.com/repos/eladriagon/apagee/releases/latest"
  local url json tag

  command -v jq >/dev/null 2>&1 || { echo "jq is required" >&2; return 3; }

  # Build curl args; keep your headers, add resiliency.
  local -a curl_args=(
    -fsSL
    --connect-timeout 10
    --max-time 30
    --retry 3
    --retry-delay 1
    --retry-all-errors
    -H "Accept: application/vnd.github+json"
    -H "X-GitHub-Api-Version: 2022-11-28"
  )
  # Optional: use GITHUB_TOKEN to dodge low rate limits
  if [[ -n "$GITHUB_TOKEN" ]]; then
    curl_args+=(-H "Authorization: Bearer $GITHUB_TOKEN")
  fi

  local json
  if ! json="$(curl "${curl_args[@]}" "$api")"; then
    # Curl already printed something sane; just exit with a failure code.
    return 2
  fi

  # Extract both tag and matching Linux asset URL
  local tag url
  tag="$(jq -r '.tag_name // empty' <<<"$json")"
  url="$(jq -r '
    .assets
    | (. // [])
    | map(select((.name|test("linux-x64"; "i")) and .state=="uploaded"))
    | first
    | .browser_download_url // empty
  ' <<<"$json")"

  # Export for external use (e.g., in other parts of your script)
  export LATEST_TAG_RAW="$tag"
  export LATEST_TAG_STRIPPED="${tag#v}"

  # if URL is empty, return 1 (no uploaded linux-x64 asset found)
  if [[ -z "$url" ]]; then
    return 1
  fi

  [[ -z "$url" ]] && return 1

  printf -v _GH_URL '%s' "$url"  # store locally accessible variable
  return 0
}

info ""
info "  Apagee Installation Script"
info "            v1.0"
info "  --------------------------"
info ""

if [ "$EUID" -ne 0 ]; then
    error "Script must be run as root."
    exit 1
fi

status "Installing dependencies..."

apt-get update -q > /dev/null 2>&1
apt-get install git curl wget tar libcap2-bin jq | grep "upgraded"

if ! id -u apagee &>/dev/null; then
  status "Creating user and group..."

  getent group apagee >/dev/null || groupadd apagee
  useradd -m -g apagee apagee
  status "User/group 'apagee:apagee' created."
fi

pushd "$(eval echo ~apagee)" >/dev/null

status "Now working in: $(pwd)"

status "Fetching the latest release..."

if ! gh_latest_linux_x64_url; then
  error "Fatal: No uploaded linux-x64 asset found. (error code $?) A build may be in progress. Try again in a few minutes." >&2
  return
fi

status "Downloading $LATEST_TAG_RAW..."

# TODO: Automate this...

wget -qO apagee.tar.gz "$_GH_URL" > /dev/null

WAS_RUNNING=false
if systemctl status apagee.service &>/dev/null; then
    status "Stopping existing service..."
    
    systemctl stop apagee.service > /dev/null
    WAS_RUNNING=true
fi

status "Extracting..."

mkdir -p apagee
tar -xzf apagee.tar.gz -C apagee --overwrite --warning=no-unknown-keyword
rm apagee.tar.gz > /dev/null

status "Setting permissions..."

chown -R apagee:apagee apagee
chmod +x apagee/apagee # Should already have +x from the tarball, but just in case.
setcap 'cap_net_bind_service=+ep' "$(pwd)/apagee/apagee" # Required to listen on 80/443.

# TODO: chmod -R 700 apagee .keys ?

if [ "$WAS_RUNNING" = true ]; then
    status "Updating service..."
else
    status "Creating new service..."
fi

cat > /etc/systemd/system/apagee.service <<EOF
[Unit]
Description=Apagee Web Server
After=network.target

[Service]
Type=simple
User=apagee
Group=apagee
AmbientCapabilities=CAP_NET_BIND_SERVICE
WorkingDirectory=$(pwd)/apagee
ExecStart=$(pwd)/apagee/apagee
Restart=on-failure

[Install]
WantedBy=multi-user.target
EOF

status "Registering service..."

systemctl daemon-reload
systemctl enable apagee.service

# If the config.json file already existed or if the service was previously running...
if [ -f "$(pwd)/apagee/config.json" ] || [ "$WAS_RUNNING" = true ]; then
    status "Starting service..."
    systemctl start apagee.service > /dev/null
    success " > Update complete!\nApagee should now be running if no errors are present.\nCheck status with:\nsudo systemctl status apagee.service"
else
    success " > Installation complete!\nNext steps:\n   • Configure your instance using the provided config.example.json\n   • Start the service with: sudo systemctl start apagee.service\n   • Check status with: sudo systemctl status apagee.service"
fi

status "Leaving $(pwd)..."
popd >/dev/null
success "Done!"
exit 0