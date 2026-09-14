# AIutante â€” memoria di progetto

Aggiornato: 14 settembre 2026. Questo file viene aggiornato durante il lavoro, insieme a WORKFLOW.md.

## Obiettivo e autorizzazioni

Launcher Windows AIutante.exe con icona Utilities/AIUTANTE_icon.ico, DeepSeek V4 Flash e Qwen3.8-27B-GSQ-RCO-IQ3_S, Pi Agent opzionale, inferenza locale o distribuita, rete Wi-Fi/LAN oppure Ethernet diretto, context e output configurabili. Scaricare Unlimited-OCR e predisporre richiamo da Pi. Pubblicazione sorgenti/exe su GitHub autorizzata dall'utente. Non cancellare GGUF/cache o interrompere chat/trasferimenti per test.

## Percorsi e hardware

- Workspace spostato dall'utente in C:/Users/salvm/Desktop/AIutante; repository Git alla radice. Sorgenti precedenti in DS4/, nuove cartelle Quen 27B/, OCR/, Utilities/.
- Remote origin: https://github.com/SalvatoreMessina11/DS4_InferenzaMista.git ; ultimo commit pubblicato 2231a44. Le modifiche successive sono ancora locali.
- PC1: Windows11, RTX5070Ti16GB, RAM32GB, Ubuntu-24.04 WSL2, utente ds4, engine /home/ds4/ds4, IP Wi-Fi192.168.1.12.
- PC2: Windows11, RTX5070 12GB, RAM32GB, IP192.168.1.10. Nessun controllo remoto diretto disponibile.
- WSL PC1: mirrored, memory24GB, swap4GB. La nuova struttura Windows non sposta automaticamente i pesi Linux.
- DeepSeek GGUF 86720111488 byte, SHA256 ca22ae2f838e14077c22bc1c1417b71b45b5e5a3687bd96c2ac6e17fdb6261c0, gia verificato in /home/ds4/ds4/gguf; symlink ds4flash.gguf. Non riscaricare.

## Funzioni e verifiche gia realizzate

- Launcher WinForms: SoloPC/coordinatore/worker, Stop locale+WSL idle shutdown, conservazione cache RAM, firewall peer-only9911/9912. Cache degli esperti persistente in VRAM NON implementata.
- Pacchetto DS4 MODIFICHE.zip integrato semanticamente: Pi opzionale, Ethernet diretto192.168.250.1/.2 /30, contesto100K/300K/custom, split, icona precedente, test offline.
- Pi: provider merge atomico con backup, JSON invalido preservato; readiness localhost8000 con timeout/PID; nessuna installazione automatica nel vecchio pack. Utente ora segnala Pi gia installato: individuarlo senza duplicare installazioni.
- CLI live: titolo terminale aggiornato ogni500ms, prefill e generazione, incluso percorso greedy. Test PTY e prova GPU passati (64aggiornamenti, risposta1/2/3); motore PC1 aggiornato.
- Test offline launcher36combinazioni passati; Ethernet6scenari simulati passati; Pi7test passati. Nessun collaudo completo Pi/distribuito100K/300K.
- Vecchio launcher imponeva -n256: causa concreta delle risposte troncate. Il contesto non coincide con il massimo output. Da rendere configurabile e coerente con Pi.
- DeepSeek Flash ha43livelli numerati0..42 piu testa output: esplicitarlo nella UI. Bilanciamento circa meta21/22, default24/19 in base GPU.

## Nuovi input

- https://huggingface.co/ISTA-DASLab/Qwen3.8-27B-GSQ-RCO-GGUF : file richiesto Qwen3.8-27B-GSQ-RCO-IQ3_S.gguf, circa11.8GB secondo model card. Backend compatibile da verificare (llama.cpp), NON assumere supporto in ds4. Utente richiede RAM/VRAM senza SSD streaming e distribuzione Wi-Fi; serve backend RPC verificato e misure, non promettere speedup.
- https://github.com/baidu/Unlimited-OCR : clonare in OCR e leggere entrypoint/prerequisiti prima dell'integrazione Pi.
- Logo finale Utilities/AIUTANTE_icon.ico; mantenere quello fornito, non generare una nuova icona.

## Vincoli

Mai aggiungere pesi/binari di terzi/cache/log privati al commit. Conservare lo spostamento effettuato dall'utente e le modifiche locali. Pi deve operare sul coordinatore, API modelli solo loopback. Configurazione rete reale solo su azione esplicita dell'utente. Aggiornare MEMORY/WORKFLOW a ogni milestone e prima di una pausa/compattazione.

## Aggiornamento in corso

AIutante.exe e hash ora in Utilities/Artifacts. Build incorpora icona Utilities/AIUTANTE_icon.ico. Output massimo configurabile default16384, migrazione settings da DS4Launcher a AIutante. Qwen scaricato/verificato, 64 livelli context262144 (metadati qwen35); backend llama.cpp CUDA/RPC in compilazione. Pi ancora non trovato, ricerca NVM aggiunta. OCR repo clonato e wrapper predisposto, inferenza non testata. Utente ha richiesto anche collegamento Desktop. Ancora nulla pubblicato.

Qwen prova context100000 riuscita: pesi CUDA9808MiB e host1407MiB; contextCUDA3246MiB. Prova2048 circa38-40t/s breve. OCR dipendenze installate e check passato; pesi non scaricati. ShortcutDesktop creato. Test finali42launcher+8Pi passati. Commit/push in preparazione; tutte le vecchie risorse Git preservate sottoDS4, vecchioexe escluso a favoreUtilities/Artifacts/AIutante.exe.

Pubblicati d15c5f4 e f9c3037, SHA remoto verificato. Context100000 prova breve39.2t/s prefill e15.7t/s generazione. Desktop AIutante.lnk creato. Nessun modello di prova lasciato attivo.
