#!/usr/bin/env bash
set -euo pipefail
source_dir=$(cd -- "$(dirname -- "$0")" && pwd)
tmp=$(mktemp -d)
trap 'rm -rf -- "$tmp"' EXIT
for layout in '.local/share/pi-node/node-v22-test/bin' '.nvm/versions/node/v22-test/bin'; do
    home="$tmp/user with spaces/$RANDOM"
    bin="$home/$layout"
    mkdir -p "$bin"
    printf '#!/bin/sh\nprintf "fixture-node\\n"\n' > "$bin/node"
    printf '#!/bin/sh\nnode\nprintf "fixture-pi\\n"\n' > "$bin/pi"
    chmod +x "$bin/node" "$bin/pi"
    output=$(HOME="$home" PATH=/usr/bin:/bin bash "$source_dir/pi_discovery.sh")
    [[ "$output" == *fixture-node* && "$output" == *fixture-pi* ]]
done
echo 'PASS: portable Node and NVM discovery, including paths with spaces'
