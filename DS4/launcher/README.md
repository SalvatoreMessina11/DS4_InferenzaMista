# Launcher Windows

Apri **[dist/DS4-Launcher.exe](../dist/DS4-Launcher.exe)** da Windows su entrambi i PC. Il file e autonomo come launcher: non richiede Codex, Python Windows o GitHub Desktop. Richiede Windows x64 con .NET Framework 4.5+ e una configurazione WSL/DS4 gia completata.

Il modello, Ubuntu, CUDA, Pi e il binario Linux non sono incorporati nell'exe. La copia/verifica del GGUF sul PC2 deve terminare prima dell'avvio. Il launcher non installa Linux o Pi e non cambia automaticamente la rete. Stop arresta la distribuzione solo dopo l'avviso sui processi e trasferimenti che verranno interrotti.

## Frontend, context e comandi

Scegli **Chat DS4** oppure **Pi Agent** sul coordinatore o in locale. Sul worker Pi viene disabilitato automaticamente. Il campo context e numerico: preset 100000, 300000 oppure Personalizzato, da 2048 a 1000000. Il nuovo default e 100000 anche per le impostazioni precedenti senza questo campo. Imposta lo stesso valore sui due PC; il launcher non sincronizza le impostazioni attraverso la rete. Sopra 300K compare una nota sulla memoria. Questi contesti non sono certificati sull'hardware corrente; per confronti col vecchio avvio imposta 2048.

Lo split resta automatico 24/19 per le due GPU; seleziona Split personalizzato per scegliere 1–42 livelli sul coordinatore, con lo stesso numero sul worker. **Comando** mostra il comando effettivo e le variabili d'ambiente senza avviare processi o modificare la rete.

**Pi Agent:** Verifica controlla `command -v pi`, `pi --version`, Python3 e ds4-server. Se manca Pi, mostra un errore; Chat DS4 resta disponibile. **Configura Pi** aggiorna solo il provider ds4 in `~/.pi/agent/models.json`, salvando prima un backup timestamped e preservando gli altri provider. Un JSON invalido resta intatto e blocca l'operazione. Anche Avvia effettua questo merge.

Avvia apre ds4-server in una console, legato a **127.0.0.1:8000**, poi verifica `/v1/models` ogni secondo per un massimo di cinque minuti. Solo dopo aver trovato il modello atteso apre Pi in Windows Terminal, con fallback a console interattiva. Un errore del server o timeout impedisce l'apertura di Pi; Stop resta disponibile durante l'attesa. Non vengono sondate le porte di protocollo 9911/9912. La configurazione Pi usa lo stesso context, output massimo min(16384, context/2), KV su `$HOME/.cache/ds4-kv` con budget 8192 MB. I tool Pi lavorano nella cartella DS4 del coordinatore.

## Avanzamento live

La Chat DS4 (locale o coordinatore) aggiorna il titolo della console ogni 500 ms: fase, token elaborati/emessi, token/s **medi dall'inizio della fase**, tempo trascorso. Nel prefill distribuito il conteggio resta quello dei blocchi confermati; durante un blocco il tempo continua ad avanzare e la media si aggiorna. Non e una stima di token remoti non ancora completati. Il worker non genera una seconda risposta. Le statistiche finali restano disponibili; gli indicatori nel titolo non vengono scritti su stdout e sono disabilitati con stderr reindirizzato oppure `DS4_LIVE_STATUS=0`. Il terminale deve supportare titoli OSC; eventuali titoli fissi imposti da Windows Terminal possono nasconderli. Pi utilizza la propria TUI, non gli indicatori della CLI ds4.

## Wi-Fi ed Ethernet

Per la rete esistente scegli Wi-Fi/LAN, inserisci IP locale/peer e usa Configura rete. Per un cavo diretto scegli Ethernet diretto e Configura rete: seleziona la scheda fisica dedicata e PC1/PC2 indipendentemente dal ruolo DS4. Il setup usa `192.168.250.1/30` e `.2/30`, senza gateway, e mostra il link realmente negoziato. Ripeti sull'altro PC. Il setup rifiuta schede con IP/gateway preesistenti e reti sovrapposte; non modifica Wi-Fi o route predefinita. Le regole Windows/Hyper-V dedicate consentono solo 9911/9912 fra i due peer, mai HTTP 8000. Rete WSL mirrored resta necessaria in modalita distribuita.

Tornando alla rete Wi-Fi reinserisci gli IP LAN nei campi: cambiare selettore non ripristina DHCP sulla scheda Ethernet. Per ripristinarla usa le impostazioni IPv4 Windows della sola scheda dedicata e rimuovi solo le regole `DS4Ethernet-<indice>-9911/9912` e corrispondenti `-WSL`, se non piu necessarie. Non cambiare la scheda Wi-Fi.

## Chat su un solo PC

Scegli **Solo questo PC - chat locale**, poi **Verifica** e **Avvia**. Non servono IP, secondo computer, rete mirrored o regole firewall. La GPU viene scelta dal motore CUDA locale; il campo GPU del launcher serve solo a ripartire i livelli nella modalita distribuita. Restano necessari WSL, CUDA, binario e GGUF. Termina prima eventuali processi DS4 precedenti.

## Arresto

**Stop**, accanto ad Avvia, termina la distribuzione Ubuntu locale usata dalla sessione (`wsl --terminate NOME`). Ferma quindi modello, collegamento DS4 e tutti gli altri processi di quella distribuzione, inclusi eventuali trasferimenti: termina la risposta in corso senza salvarla. Chiude anche la console aperta da questa istanza del launcher. Se non rimangono distribuzioni attive, esegue `wsl --shutdown` e verifica che VmmemWSL scompaia. Se un'altra distribuzione e attiva, la preserva e segnala che VmmemWSL resta necessario. Il computer remoto resta acceso: premi Stop anche li per liberarne la memoria.

L'opzione **Chiudi Ubuntu all'uscita** e attiva per default e viene salvata. La X arresta Ubuntu solo dopo la chiusura della console modello e se i controlli non rilevano processi DS4, terminali Linux, compilazioni, trasferimenti comuni o servizi TCP in ascolto. Se li rileva, avvisa e lascia Ubuntu acceso; questi controlli non sono un inventario di ogni possibile lavoro in background. Se vuoi l'arresto esplicito usa Stop. Chiudere un launcher dopo Stop non riavvia Ubuntu per controllarlo.

Verifica locale: compilazione launcher e motore CUDA passate; self-test avvio singolo/distribuito passato. Eseguita realmente la funzione di arresto WSL del launcher sul PC1: VmmemWSL assente al termine. Il test non copre l'arresto coordinato del PC remoto. I contatori CUDA sono ora opt-in; il motore aggiornato e installato sul PC1 e richiede ricompilazione sul PC2.

## RAM, VRAM e SSD

**Conserva in RAM i pesi letti** e attivo per default, anche per chi aggiorna una vecchia configurazione. Imposta `DS4_CUDA_NO_DIRECT_IO=1` e `DS4_CUDA_KEEP_MODEL_PAGES=1`: le letture possono beneficiare della cache Linux e il backend non chiede di scartare le pagine dopo la copia alla GPU. Deselezionandolo viene rimossa esplicitamente la seconda variabile, per un confronto con il comportamento precedente.

La cache RAM cresce con le letture, e recuperabile da Linux e condivide il limite WSL con processi e buffer. `memory=24GB` e il limite di tutta la VM, non una riserva di 24 GB per i pesi. Il launcher mostra `free -h` in Verifica; non cambia i limiti WSL. La modifica richiede un nuovo avvio del modello.

I messaggi CUDA `loading model tensors ... GiB` sono disattivati per default nel motore: i caricamenti tardivi non devono mescolarsi con la risposta. Errori e statistiche finali prefill/generazione restano visibili. Per diagnostica si possono riattivare con `DS4_CUDA_MODEL_LOAD_PROGRESS=1`. Dopo un aggiornamento del codice CUDA, sul secondo PC esegui in Ubuntu `make -j4 ds4 CUDA_ARCH=sm_120` nella cartella del motore, a sessione chiusa; aggiornare solo l'exe Windows non aggiorna il binario Linux.

Non precarica indiscriminatamente gli 81 GiB e non blocca tutta la RAM: i primi accessi richiedono ancora l'SSD. La VRAM resta gestita dal motore, con spazio necessario per contesto e buffer; questa opzione non implementa una cache persistente CUDA degli esperti piu frequenti. Nessuna garanzia di memoria piena o di aumento di token/s. Confrontare lo stesso prompt e contesto, distinguendo prefill e generazione e indicando se la cache era gia calda.

Per una singola chat distribuita, il token attraversa i due PC in sequenza. Il beneficio dipende dai trasferimenti SSD evitati rispetto ai tempi aggiunti dalla rete e dalla seconda GPU; due PC possono essere piu lenti di uno.

## Impostazioni

| Campo | PC1 | PC2 |
|---|---|---|
| GPU | RTX 5070 Ti 16 GB | RTX 5070 12 GB |
| IP locale attuale | 192.168.1.12 | 192.168.1.10 |
| IP altro PC | 192.168.1.10 | 192.168.1.12 |
| Distribuzione | Ubuntu-24.04 | Ubuntu-24.04, verificare |
| Utente Ubuntu | ds4 | quello creato sul PC2 |
| Cartella Linux | /home/ds4/ds4 | cartella del clone nella home Linux |

Scegli **Coordinatore** sul PC dove vuoi scrivere; sull'altro scegli **Worker**. Seleziona la GPU corretta su entrambi: il launcher assegna 24 livelli alla 5070 Ti e 19 alla 5070, collocando i livelli iniziali sul coordinatore. Questa suddivisione deve ancora essere collaudata con inferenza su due GPU.

1. **Verifica** controlla cartella, modello presente, rete mirrored, IP locale e GPU. Non calcola nuovamente SHA256: la verifica del file va completata separatamente.
2. **Configura rete**, alla prima configurazione o quando cambiano gli IP, richiede UAC e crea le regole Windows/Hyper-V per TCP 9911 e 9912 limitate all'altro PC. Non cambia il profilo di rete, non disabilita il firewall e non tocca la porta 9913.
3. **Avvia** apre la console DS4 del ruolo scelto, oppure server e Pi dopo la readiness. Nel coordinatore scrivi i prompt; il worker mostra soltanto attivita e diagnostica. La Chat DS4 mostra velocita live nel titolo e statistiche finali.

Le impostazioni sono salvate per utente Windows in `%LOCALAPPDATA%\DS4Launcher\settings.xml`. Il launcher cerca il binario nella home dell'utente Ubuntu se lasci vuota la cartella. Non copiare le impostazioni del PC1 sul PC2.

Per invertire i ruoli termina ordinatamente entrambi i processi, poi cambia ruolo su entrambi e riavvia. Non e previsto il trasferimento di una chat attiva. Prima della prima prova vanno chiusi i listener diagnostici 9911/9912; il launcher rileva la porta occupata e non li termina da solo.

## Aggiornamento

Nel repository del secondo PC:

```sh
git pull --ff-only
make -j4 ds4 ds4-server CUDA_ARCH=sm_120
```

Se il repository e nella home Ubuntu, aprilo da Esplora file tramite `\\wsl.localhost\Ubuntu-24.04\home\NOME_UTENTE\DS4_InferenzaMista\dist` e fai doppio clic su `DS4-Launcher.exe`. Puoi anche copiare soltanto l'exe sul Desktop Windows.

L'eseguibile non e firmato digitalmente: Windows puo mostrarne l'autore come sconosciuto.

L'icona e gli script Ethernet/Pi sono incorporati: non occorre copiare file di supporto accanto all'exe. Le nuove metriche richiedono il motore ricompilato, non soltanto l'exe. Test e comandi completi: [VALIDATION.md](VALIDATION.md).

## Build e verifiche

Sorgente C# in DS4Launcher.cs, compilazione su Windows:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File launcher\build.ps1
```

Controllati: compilazione x64, comandi dei quattro abbinamenti ruolo/GPU, rifiuto IP non validi, passaggio stringhe Windows->WSL, verifica reale WSL/GPU sul PC1, blocco con listener diagnostico presente e layout della finestra.
`DS4-Launcher.exe --self-test` esegue i controlli di sviluppo sulla configurazione PC1 e produce un report; non e una prova di inferenza distribuita. Pipeline reale e avvio sul PC2 ancora da collaudare. Pulsante firewall da verificare alla prima configurazione sul PC2.

Correzione firewall mirrored: le regole Hyper-V esplicite usano il suffisso -WSL per evitare collisioni con quelle Windows ereditate. Configurazione e ripetizione verificate sul PC1 per TCP 9911/9912, con accesso limitato al PC2.

Aggiornamento modalita singola e cache RAM (13 settembre 2026): self-test passato per avvio locale senza IP/rete, quattro abbinamenti distribuiti e cache attiva/disattiva. Prova reale sul PC1, prompt `Quanto fa 2 + 2?`, ctx 2048, prefill chunk 64, massimo 8 token: risposta `4`, uscita 0, circa 37 s incluso caricamento, prefill 0.57 t/s e generazione 0.50 t/s. Picco cache file Linux 22.36 GiB e VRAM totale 10785 MiB. La risposta e troppo breve per un benchmark affidabile; non e un confronto A/B e non dimostra un miglioramento di velocita. Nessuna nuova prova a due PC.
