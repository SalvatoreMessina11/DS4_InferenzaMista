set -e
command -v python3 >/dev/null || { echo 'Python3 necessario in Ubuntu'; exit 1; }
# Non-interactive login shells do not generally load NVM or npm user bins.
if ! command -v pi >/dev/null; then
    for bin in "$HOME/.local/bin" "$HOME/.npm-global/bin" "$HOME/.local/share/pnpm" "$HOME/.bun/bin"; do
        if [ -x "$bin/pi" ]; then export PATH="$bin:$PATH"; break; fi
    done
fi
if ! command -v pi >/dev/null; then
    for bin in $(find "$HOME/.nvm/versions/node" -mindepth 2 -maxdepth 2 -type d -name bin 2>/dev/null | sort -Vr); do
        if [ -x "$bin/pi" ]; then export PATH="$bin:$PATH"; break; fi
    done
fi
if ! command -v pi >/dev/null; then
    printf 'Pi non trovato per utente Ubuntu %s (HOME=%s).\n' "$(id -un)" "$HOME"
    echo 'Se Pi e installato per un altro utente Ubuntu, selezionalo nel launcher.'
    echo 'Un Pi installato solo in Windows non e disponibile come comando Linux. Nessuna installazione automatica effettuata.'
    exit 1
fi
printf 'Pi: %s\n' "$(command -v pi)"
pi --version
