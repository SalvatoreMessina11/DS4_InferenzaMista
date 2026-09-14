#!/usr/bin/env bash
# Run inside Ubuntu 24.04 on WSL; installs toolkit components, not Linux GPU drivers.
set -euo pipefail
source /etc/os-release
[[ "$ID" == ubuntu && "$VERSION_ID" == 24.04 ]] || { echo 'Richiesto Ubuntu 24.04'; exit 1; }
work=$(mktemp -d /tmp/ds4-cuda.XXXXXX)
sudo apt-get update
sudo apt-get install -y build-essential curl ca-certificates
curl -fL --retry 3 -o "$work/cuda-keyring.deb" https://developer.download.nvidia.com/compute/cuda/repos/ubuntu2404/x86_64/cuda-keyring_1.1-1_all.deb
sudo dpkg -i "$work/cuda-keyring.deb"
sudo apt-get update
sudo apt-get install -y --no-install-recommends cuda-nvcc-13-2 cuda-cudart-dev-13-2 libcublas-dev-13-2
/usr/local/cuda-13.2/bin/nvcc --version
