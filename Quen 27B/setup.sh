#!/usr/bin/env bash
set -euo pipefail
# Run inside Ubuntu, from any checkout path. The worker needs no GGUF.
root=${AIUTANTE_QWEN_HOME:-/home/ds4/aiutante-qwen}
revision=bbdd9f246e9667f9aeb7ad11cca269466f981184
mkdir -p "$root"
for tool in git cmake ninja; do command -v "$tool" >/dev/null || { echo "Installa $tool in Ubuntu prima di continuare"; exit 1; }; done
if [ ! -d "$root/llama.cpp/.git" ]; then git clone https://github.com/ggml-org/llama.cpp.git "$root/llama.cpp"; fi
git -C "$root/llama.cpp" checkout --detach "$revision"
cmake -S "$root/llama.cpp" -B "$root/build" -G Ninja -DGGML_CUDA=ON -DGGML_RPC=ON -DCMAKE_CUDA_COMPILER=/usr/local/cuda/bin/nvcc -DCMAKE_CUDA_ARCHITECTURES=120 -DLLAMA_BUILD_TESTS=OFF
cmake --build "$root/build" -j 4 --target llama-cli llama-server ggml-rpc-server
if [ "${1:-}" = --worker ]; then exit 0; fi
filename=Qwen3.8-27B-GSQ-RCO-IQ3_S.gguf
source_dir=$(cd -- "$(dirname -- "$0")" && pwd)
model="$source_dir/models/$filename"
mkdir -p "$source_dir/models"
if [ ! -f "$model" ]; then
    curl --fail --location --retry 4 --continue-at - -o "$model.part" "https://huggingface.co/ISTA-DASLab/Qwen3.8-27B-GSQ-RCO-GGUF/resolve/main/$filename"
    echo "64b53b64c7aa39f20a7e54bd80582fe595b1d745624ee8a72e92508c0326d810  $model.part" | sha256sum -c -
    mv -- "$model.part" "$model"
fi
echo "64b53b64c7aa39f20a7e54bd80582fe595b1d745624ee8a72e92508c0326d810  $model" | sha256sum -c -
if [ ! -e "$root/model.gguf" ]; then ln -s "$model" "$root/model.gguf"; fi
echo "Backend pronto: $root. Il GGUF resta nella cartella models, senza duplicarlo."
