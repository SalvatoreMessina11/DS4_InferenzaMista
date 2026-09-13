# Launcher Windows

Apri **[dist/DS4-Launcher.exe](../dist/DS4-Launcher.exe)** da Windows su entrambi i PC. Il file e autonomo come launcher: non richiede Codex, Python Windows o GitHub Desktop. Richiede Windows x64 con .NET Framework 4.5+ e una configurazione WSL/DS4 gia completata.

Il modello, Ubuntu, CUDA e il binario Linux non sono incorporati nell'exe. La copia/verifica del GGUF sul PC2 deve terminare prima dell'avvio. Il launcher non installa Linux, non riavvia WSL e non interrompe trasferimenti.

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
