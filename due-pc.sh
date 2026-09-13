#!/usr/bin/env bash
# Experimental: the CUDA+SSD pipeline must still be validated on both PCs.
set -euo pipefail
role=${1:-}
address=${2:-}
coordinator_layers=${3:-24}
root=${DS4_ROOT:-$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)}
if [[ -z "$role" ]]; then
    echo 'Entrambe le GPU calcolano. Solo il coordinator accetta prompt.'
    echo '1) Questo PC: chat e calcolo (coordinator)'
    echo '2) Questo PC: solo calcolo (worker)'
    read -r -p 'Scegli 1 o 2: ' choice
    case "$choice" in
        1) role=coordinator; read -r -p 'IPv4 LAN di questo PC: ' address ;;
        2) role=worker; read -r -p 'IPv4 LAN del PC con la chat: ' address ;;
        *) echo 'Scelta non valida'; exit 2 ;;
    esac
    read -r -p 'Livelli sul coordinator (24 se chat su 5070 Ti; 19 se chat su 5070): ' coordinator_layers
fi
if [[ "$role" != coordinator && "$role" != worker ]] || [[ -z "$address" ]]; then
    echo 'Uso: bash due-pc.sh coordinator IP_LOCALE [NUM_LIVELLI_COORDINATOR]'
    echo '     bash due-pc.sh worker IP_COORDINATOR [NUM_LIVELLI_COORDINATOR]'
    exit 2
fi
if [[ ! "$coordinator_layers" =~ ^([1-9]|[1-3][0-9]|4[0-2])$ ]]; then
    echo 'Il coordinator deve avere da 1 a 42 livelli, uguali nella configurazione dei due PC.'
    exit 2
fi
export DS4_CUDA_NO_DIRECT_IO=1
common=(--cuda --ssd-streaming --ctx 2048 --prefill-chunk 64 --nothink)
if [[ "$role" == coordinator ]]; then
    command=(./ds4 "${common[@]}" --role coordinator --layers "0:$((coordinator_layers-1))" --listen "$address" 9911 -n 256)
    echo 'COORDINATOR: scrivi i prompt in questa finestra. Attendi il worker.'
else
    command=(./ds4 "${common[@]}" --role worker --layers "$coordinator_layers:output" --listen 0.0.0.0 9912 --coordinator "$address" 9911)
    echo 'WORKER: questa finestra esegue solo calcolo; invia i prompt dall’altro PC.'
fi
if [[ "${DS4_DRY_RUN:-0}" == 1 ]]; then
    printf '%q ' "${command[@]}"
    printf '\n'
    exit 0
fi
cd "$root"
if pgrep -x ds4 >/dev/null || pgrep -x ds4-server >/dev/null; then
    echo 'Chiudi prima la chat/server DS4 di questo PC con /quit.'
    exit 1
fi
test -x ./ds4 || { echo "Binario mancante in $root"; exit 1; }
test -s ds4flash.gguf || { echo 'Modello mancante: scarica ds4f-q2 o copia il GGUF verificato.'; exit 1; }
exec "${command[@]}"
