# AIutante - aggiornamento 14 settembre 2026

Eseguibile attuale: `Utilities/Artifacts/AIutante.exe` (dalla radice repository), hash nel file adiacente `AIutante.sha256`. Logo utente incorporato. Collegamento Desktop AIutante.lnk creato sul PC1; riproducibile con Utilities/Crea-collegamento.ps1.

- Build .NET Framework x64 riuscita. Test offline: 42 combinazioni, incluse Qwen locale/coordinatore/worker, memoria, output personalizzato e persistenza. Screenshot verificato.
- Test Pi: 8 passati; output distinto dal context, provider Qwen e DS4 preservati insieme, backup univoci anche per aggiornamenti rapidi.
- Qwen GGUF scaricato e SHA256 verificato. Backend CUDA/RPC compilato. Prova locale context2048: risposta `2 + 2 fa 4.`, 9 token generati; 62.4-71.6 token/s prefill, 38.3-40.1 token/s generazione. VRAM totale occupata 12828 MiB, GPU RTX5070Ti. Prova breve, non benchmark generale.
- Qwen context100000: caricamento e risposta breve riusciti. Ripartizione misurata CUDA pesi9808 MiB / context3246 MiB / compute778 MiB; host pesi1407 MiB. Non e una prova con prompt di100000 token.
- OCR repository ufficiale clonato; ambiente /home/ds4/aiutante-ocr/.venv installato, controllo dipendenze passato. Wrapper immagini/PDF e istruzioni nel prompt Pi. Pesi OCR non scaricati, inferenza OCR non collaudata.
- Pi non trovato nei percorsi controllati Windows/Ubuntu. Ricerca ampliata a installazioni utente/NVM; non dichiariamo risolta un'installazione che non e stata individuata. Se installato altrove, selezionare l'utente Ubuntu corretto.
- Pipeline fisica su due PC e contesti lunghi pieni non collaudati. Qwen supporta il GGUF testuale richiesto; vision/MTP richiedono pesi diversi e non sono abilitati.

## Verifiche precedenti conservate (prima del cambio nome e struttura)

# Launcher unificato â€” consegna e verifiche, 14 settembre 2026

## Risultato

Versione precedente: `dist/DS4-Launcher.exe`, Windows x64, 79872 byte. Icona incorporata nell'eseguibile e nella finestra; risorse Ethernet.ps1 e pi_support.py incluse. Nessun modello, credenziale privata o cache inclusi.

SHA256:

```text
2af47c2fbf0b60a481e04535a44abe5e5f20dd90cde3a10cefd872cc537f0423
```

La UI comprende ruolo, frontend, tipo rete, GPU, context con preset e controllo numerico, split opzionale, IP, distro/utente/cartella, cache RAM, chiusura Ubuntu, Verifica / Configura rete / Configura Pi / Comando / Avvia / Stop. Worker disabilita Pi; Solo PC disabilita rete/IP. La finestra scorre sulle risoluzioni piu piccole. Screenshot di sviluppo generato e ispezionato; non e necessario accanto all'exe.

## Modifiche

- `launcher/DS4Launcher.cs`: UI, context, server/client opzionali, persistenza compatibile, Stop durante la readiness e avviso prima di interrompere trasferimenti.
- `launcher/LauncherAdvanced.cs`: generazione comandi, integrazione Ethernet e Pi, risorse, readiness, dry-run e test offline.
- `launcher/Ethernet.ps1`, `EthernetSetup.cs`, `test-ethernet.ps1`: portati semanticamente dalla patch del pacchetto DS4 MODIFICHE.zip, basata su 2231a44, preservando gli aggiornamenti successivi. IP dedicati /30 senza gateway, firewall ai soli peer, test simulati.
- `launcher/pi_support.py`, `test_pi_support.py`: provider Pi con backup/merge atomico, validazione JSON, readiness HTTP/PID e test temporanei.
- `launcher/DS4-Launcher-icon.ico`, `build.ps1`, exe e hash: icona dal pacchetto, build autonoma con risorse.
- `ds4_cli.c`, `tests/test_cli_live_status.c`, `tests/test_cli_live_status.py`: timer live nel titolo della console, conteggi monotoni e test PTY. Coperti chat interattiva, prompt singolo campionato, percorso greedy e coordinatore via sessione.
- `README.md`, `DUE-PC.md`, `docs/CLIENTS.md`, `launcher/README.md`: utilizzo, aggiornamento, prerequisiti e limiti.

## Comandi effettivi (context 100000, split 24)

In ogni caso, con cache RAM selezionata:

```bash
export DS4_CUDA_NO_DIRECT_IO=1
export DS4_CUDA_KEEP_MODEL_PAGES=1
```

Chat locale:

```bash
./ds4 -m ds4flash.gguf --cuda --ssd-streaming --ctx 100000 --prefill-chunk 64 --nothink -n 256
```

Pi locale:

```bash
./ds4-server -m ds4flash.gguf --cuda --ssd-streaming --ctx 100000 --prefill-chunk 64 --host 127.0.0.1 --port 8000 --kv-disk-dir "$HOME/.cache/ds4-kv" --kv-disk-space-mb 8192
```

Pi coordinatore Wi-Fi, PC1:

```bash
./ds4-server -m ds4flash.gguf --cuda --ssd-streaming --ctx 100000 --prefill-chunk 64 --role coordinator --layers 0:23 --listen 192.168.1.12 9911 --host 127.0.0.1 --port 8000 --kv-disk-dir "$HOME/.cache/ds4-kv" --kv-disk-space-mb 8192
```

Worker:

```bash
./ds4 -m ds4flash.gguf --cuda --ssd-streaming --ctx 100000 --prefill-chunk 64 --nothink --role worker --layers 24:output --listen 0.0.0.0 9912 --coordinator 192.168.1.12 9911
```

Pi viene aperto solo dopo `/v1/models` pronto e processo server vivo:

```bash
pi --model ds4/deepseek-v4-flash
```

Con Ethernet usare l'IP dedicato del coordinatore. Context personalizzato viene sostituito esattamente nei comandi e in Pi `contextWindow`; la scelta viene salvata. Nessun comando aggiunge `--gpu-vram` o `--gpu-devices`.

## Verifiche eseguite

- Build C# senza warning; icona e risorse presenti. Hash verificato.
- 36 combinazioni: tre context (100000, 300000, 131072), due reti, tre ruoli e due frontend. Verificati comando effettivo, esclusione Pi sul worker, localhost HTTP, assenza opzioni GPU incompatibili e isolamento locale.
- Roundtrip XML, impostazioni precedenti senza campi nuovi, split personalizzato, cache RAM attiva/disattiva. Nessuna scrittura alle impostazioni reali nei test.
- Sette test Python: merge/backup dei provider, context/output, JSON invalido senza perdita, errore scrittura atomica, readiness su HTTP locale simulato, timeout, crash/preflight fallito prima della readiness e limiti context.
- Sei test Ethernet senza cmdlet reali: PC1/PC2/esistente exit 0; conflitto/IP occupato/cavo scollegato exit 1, come previsto. Nessuna modifica della rete reale.
- Test PTY del timer C: aggiornamenti durante attesa senza nuovi blocchi, conteggio non regressivo, generazione, fine fase, nessuna scrittura stdout, nessun escape con stderr reindirizzato, opt-out.
- Compilazione CUDA del motore locale; prova reale RTX 5070 Ti a ctx 2048: risposta `1`, `2`, `3`, 64 aggiornamenti nel titolo, nessun indicatore nello stdout e nessun contatore di caricamento GiB nel log. Ultimo titolo: 5 token, 1.03 token/s medi. Risposta breve: non e un benchmark comparativo.
- Motore `ds4` con indicatori installato su PC1; `ds4-server` rilinkato con le correzioni CUDA. Nessuna modifica ai GGUF o cancellazione delle cache.

Comandi di test:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File launcher\build.ps1
dist\DS4-Launcher.exe --self-test-offline
powershell -NoProfile -ExecutionPolicy Bypass -File launcher\test-ethernet.ps1 -Case pc1
```

In Ubuntu:

```bash
python3 launcher/test_pi_support.py
gcc -O2 -D_GNU_SOURCE -ffunction-sections -fdata-sections -Wl,--gc-sections -o /tmp/ds4-test-live-status tests/test_cli_live_status.c -pthread -lm
python3 tests/test_cli_live_status.py /tmp/ds4-test-live-status
```

## Verifiche ancora necessarie

Pi non risulta disponibile con `command -v pi` nell'utente Ubuntu `ds4` del PC1. Non e stato installato automaticamente, come richiesto dal pacchetto. Il test reale `Usa il tool bash per eseguire pwd e riportami l'output` resta da eseguire dopo l'installazione scelta dall'utente. La readiness e stata testata con un endpoint locale simulato, non con una sessione Pi completa.

Restano da provare sui due PC fisici: applicazione Ethernet/UAC, velocita effettivamente negoziata, rete mirrored, handshake e inferenza Pi distribuita, context 100K/300K e uso di memoria. Gli indicatori del coordinatore sono alimentati dai callback esistenti; non e stata misurata qui una nuova pipeline remota. Non si promettono aumenti di velocita ne allocazione garantita dei contesti grandi.

Il prefill distribuito conferma token per blocchi. Il timer mostra tempo e media anche quando il blocco non e ancora concluso: non inventa token completati. I titoli OSC dipendono dal terminale; Pi ha una propria TUI. Lo Stop interrompe la distribuzione locale dopo avviso e non spegne il PC remoto.

## Aggiornamento PC2

Chiudi la sessione del modello, poi nel clone Ubuntu:

```bash
git pull --ff-only
make -j4 ds4 ds4-server CUDA_ARCH=sm_120
```

Apri l'exe nuovo da Windows. Il solo aggiornamento dell'exe non ricompila il motore Linux. Non serve trasferire nuovamente il GGUF.

## Correzione Pi e interfaccia

Pi individuato e verificato versione0.85.1 in /home/ds4/.local/share/pi-node/node-v22.23.2-linux-x64/bin/pi. Discovery aggiunge anche Node al PATH. Test percorsi portable/NVM con spazi passato.

Terminale Pi aperto subito tramite la stessa console gestita del server, senza passaggio Windows Terminal. Readiness eseguita nella finestra Pi con stato ogni5secondi e timeout900secondi; errore visibile, non finestra silenziosamente assente. Non si dichiara pronto prima del controllo API.

Default Qwen+Pi/soloPC/output32768; migrazione impostazioni con versionamento e preservazione delle scelte future. Preset Qwen fino262144,300K escluso. Pulsanti Aiuto Pi e Dettagli avvio con spiegazioni; configurazione provider automatica. Build e42casi offline,8testPython,discovery passati.

Prova reale finale Pi0.85.1+Qwen context8192 riuscita: toolCall read, toolResult isError=false, rispostaAIUTANTE_PI_READ_OK. ProcessoPi exit0. Test senza sessioni persistenti, configurazione temporanea; server di prova chiuso al termine. La prova riguarda Qwen locale, non DeepSeek o PC2.
