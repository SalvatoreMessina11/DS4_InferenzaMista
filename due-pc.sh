#!/usr/bin/env bash
# Experimental: the CUDA+SSD pipeline must still be validated on both PCs.
set -euo pipefail
role=${1:-}
address=${2:-}
root=${DS4_ROOT:-$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)}
if [[ "$role" != coordinator && "$role" != worker ]] || [[ -z "$address" ]]; then
    echo 'Uso: bash due-pc.sh coordinator IP_LOCALE_PC1'
    echo '     bash due-pc.sh worker IP_PC1'
    exit 2
fi
cd "$root"
if pgrep -x ds4 >/dev/null || pgrep -x ds4-server >/dev/null; then
    echo 'Chiudi prima la chat/server DS4 di questo PC con /quit.'
    exit 1
fi
test -x ./ds4 || { echo "Binario mancante in $root"; exit 1; }
test -s ds4flash.gguf || { echo 'Modello mancante: scarica ds4f-q2 o copia il GGUF verificato.'; exit 1; }
export DS4_CUDA_NO_DIRECT_IO=1
common=(--cuda --ssd-streaming --ctx 2048 --prefill-chunk 64 --nothink)
if [[ "$role" == coordinator ]]; then
    # Initial split, to be adjusted only after measuring both devices.
    exec ./ds4 "${common[@]}" --role coordinator --layers 0:23 --listen "$address" 9911 -n 256
else
    exec ./ds4 "${common[@]}" --role worker --layers 24:output --listen 0.0.0.0 9912 --coordinator "$address" 9911
fi
