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

apt update -q > /dev/null
apt --quiet=2 install git curl wget tar libcap2-bin > /dev/null

if ! id -u apagee &>/dev/null; then
  status "Creating user and group..."

  getent group apagee >/dev/null || groupadd apagee
  useradd -m -g apagee apagee
  status "User/group 'apagee:apagee' created."
fi

pushd "$(eval echo ~apagee)" >/dev/null

status "Now working in: $(pwd)"
status "Downloading..."

# TODO: Automate this...

wget -qO apagee.tar.gz "https://github.com/Eladriagon/apagee/releases/download/v1.0.2-Pre/apagee-linux-x64.tar.gz" > /dev/null

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