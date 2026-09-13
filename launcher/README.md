# Launcher Windows

Apri **[dist/DS4-Launcher.exe](../dist/DS4-Launcher.exe)** da Windows su entrambi i PC. Il file e autonomo come launcher: non richiede Codex, Python Windows o GitHub Desktop. Richiede Windows x64 con .NET Framework 4.5+ e una configurazione WSL/DS4 gia completata.

Il modello, Ubuntu, CUDA e il binario Linux non sono incorporati nell'exe. La copia/verifica del GGUF sul PC2 deve terminare prima dell'avvio. Il launcher non installa Linux, non riavvia WSL e non interrompe trasferimenti.

## Chat su un solo PC

Scegli **Solo questo PC - chat locale**, poi **Verifica** e **Avvia**. Non servono IP, secondo computer, rete mirrored o regole firewall. La GPU viene scelta dal motore CUDA locale; il campo GPU del launcher serve solo a ripartire i livelli nella modalita distribuita. Restano necessari WSL, CUDA, binario e GGUF. Termina prima eventuali processi DS4 precedenti.

## RAM, VRAM e SSD

**Conserva in RAM i pesi letti** e attivo per default, anche per chi aggiorna una vecchia configurazione. Imposta `DS4_CUDA_NO_DIRECT_IO=1` e `DS4_CUDA_KEEP_MODEL_PAGES=1`: le letture possono beneficiare della cache Linux e il backend non chiede di scartare le pagine dopo la copia alla GPU. Deselezionandolo viene rimossa esplicitamente la seconda variabile, per un confronto con il comportamento precedente.

La cache RAM cresce con le letture, e recuperabile da Linux e condivide il limite WSL con processi e buffer. `memory=24GB` e il limite di tutta la VM, non una riserva di 24 GB per i pesi. Il launcher mostra `free -h` in Verifica; non cambia i limiti WSL. La modifica richiede un nuovo avvio del modello.

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
3. **Avvia** apre la console DS4 del ruolo scelto. Nel coordinatore scrivi i prompt; il worker mostra soltanto attivita e diagnostica. Velocita prefill/generazione riportate da DS4 al termine della risposta.

Le impostazioni sono salvate per utente Windows in `%LOCALAPPDATA%\DS4Launcher\settings.xml`. Il launcher cerca il binario nella home dell'utente Ubuntu se lasci vuota la cartella. Non copiare le impostazioni del PC1 sul PC2.

Per invertire i ruoli termina ordinatamente entrambi i processi, poi cambia ruolo su entrambi e riavvia. Non e previsto il trasferimento di una chat attiva. Prima della prima prova vanno chiusi i listener diagnostici 9911/9912; il launcher rileva la porta occupata e non li termina da solo.

## Aggiornamento

Nel repository del secondo PC:

```sh
git pull --ff-only
```

Se il repository e nella home Ubuntu, aprilo da Esplora file tramite `\\wsl.localhost\Ubuntu-24.04\home\NOME_UTENTE\DS4_InferenzaMista\dist` e fai doppio clic su `DS4-Launcher.exe`. Puoi anche copiare soltanto l'exe sul Desktop Windows.

L'eseguibile non e firmato digitalmente: Windows puo mostrarne l'autore come sconosciuto.

## Build e verifiche

Sorgente C# in DS4Launcher.cs, compilazione su Windows:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File launcher\build.ps1
```

Controllati: compilazione x64, comandi dei quattro abbinamenti ruolo/GPU, rifiuto IP non validi, passaggio stringhe Windows->WSL, verifica reale WSL/GPU sul PC1, blocco con listener diagnostico presente e layout della finestra.
`DS4-Launcher.exe --self-test` esegue i controlli di sviluppo sulla configurazione PC1 e produce un report; non e una prova di inferenza distribuita. Pipeline reale e avvio sul PC2 ancora da collaudare. Pulsante firewall da verificare alla prima configurazione sul PC2.

Correzione firewall mirrored: le regole Hyper-V esplicite usano il suffisso -WSL per evitare collisioni con quelle Windows ereditate. Configurazione e ripetizione verificate sul PC1 per TCP 9911/9912, con accesso limitato al PC2.

Aggiornamento modalita singola e cache RAM (13 settembre 2026): self-test passato per avvio locale senza IP/rete, quattro abbinamenti distribuiti e cache attiva/disattiva. Prova reale sul PC1, prompt `Quanto fa 2 + 2?`, ctx 2048, prefill chunk 64, massimo 8 token: risposta `4`, uscita 0, circa 37 s incluso caricamento, prefill 0.57 t/s e generazione 0.50 t/s. Picco cache file Linux 22.36 GiB e VRAM totale 10785 MiB. La risposta e troppo breve per un benchmark affidabile; non e un confronto A/B e non dimostra un miglioramento di velocita. Nessuna nuova prova a due PC.
