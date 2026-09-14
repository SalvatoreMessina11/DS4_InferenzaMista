# AIutante â€” piano e stato del lavoro

Aggiornato: 14 settembre 2026. Stato generale: IN CORSO, non ancora pubblicato.

## 1. Continuita e struttura

- [x] Rilevare nuova root e cartelle DS4, Quen27B, OCR, Utilities.
- [x] Creare MEMORY.md e WORKFLOW.md.
- [ ] Aggiornare ignore radice e percorsi build/test dopo lo spostamento.
- [ ] Preservare e verificare tutte le modifiche precedenti nel commit della nuova struttura.

## 2. Launcher e problemi segnalati

- [x] Rinominare prodotto/eseguibile AIutante e incorporare Utilities/AIUTANTE_icon.ico.
- [x] Campo Output massimo esplicito, eliminare -n256 fisso; default utile, salvato e coerente con Pi.
- [ ] Chiarire context vs output, motivo di stop per limite raggiunto.
- [ ] Rilevare Pi su Windows/WSL/NVM e spiegare l'utente/ambiente corretto.
- [x] Rinominare Comando in Anteprima avvio e spiegare la funzione.
- [x] Mostrare totale livelli e ripartizione coordinatore/worker.
- [x] Indicatori live prefill/generazione separati dal testo implementati e testati.

## 3. Qwen

- [x] Leggere model card del modello esatto richiesto.
- [ ] Verificare architettura, context supportato, metadati GGUF e backend compatibile.
- [x] Scaricare file IQ3_S con ripresa e verifica hash senza duplicati.
- [ ] Predisporre backend RAM/VRAM senza SSD streaming, controllo livelli GPU e memoria/context.
- [ ] Integrare locale/server Pi e RPC su duePC con firewall peer-only; misurare locale, segnalare prove fisiche residue.

## 4. Unlimited-OCR

- [x] Clonare repository ufficiale in OCR.
- [ ] Leggere codice/prerequisiti e predisporre comando richiamabile da Pi, con diagnostica dipendenze/GPU e output separato.
- [ ] Verificare invocazione senza interrompere modelli attivi; documentare installazioni/modelli OCR eventualmente ancora necessari.

## 5. Verifica e pubblicazione

- [ ] Test launcher: modelli/ruoli/reti/context/output/persistenza/Pi/icone.
- [ ] Test configurazione Pi e readiness, simulazioni rete, test indicatori live.
- [ ] Build finale Utilities/Artifacts o dist canonico, hash, screenshot.
- [ ] Documentazione comandi e limiti, aggiornamento MEMORY/WORKFLOW.
- [ ] Commit/push GitHub autorizzati, verificare SHA remoto e link download AIutante.exe.

## Regole operative

Aggiornare le caselle soltanto con evidenza. Separare supporto implementato, simulazione e prova hardware. I test non devono avviare un secondo modello se uno e gia attivo, spegnere WSL o cambiare la rete automaticamente. Verificare spazio libero prima dei download pesanti. Non chiamare completato il supporto Qwen/OCR prima dei controlli effettivi.

## Ultima sessione

- Qwen verificato SHA256 64b53b64c7aa39f20a7e54bd80582fe595b1d745624ee8a72e92508c0326d810, 11771546784 byte; architettura qwen35, 64 livelli, context262144.
- Backend llama.cpp bbdd9f246e9667f9aeb7ad11cca269466f981184, compilazione CUDA/RPC in corso in /home/ds4/aiutante-qwen/build.log.
- AIutante.exe con icona richiesta compilato; 42 combinazioni offline passate, comprese Qwen e persistenza output.
- OCR clonato, wrapper immagini/PDF creato; dipendenze e prova inferenza restano da completare.
- Ricerca Pi ampliata a NVM/npm-global/pnpm/bun; nessuna installazione Pi trovata finora nei percorsi controllati.
- Aggiungere collegamento Desktop AIutante.lnk al termine (richiesta esplicita utente).
- Commit/push NON ancora effettuati.

## Esito verifiche finali PC1

- [x] AIutante.exe e icona finale; collegamento Desktop AIutante.lnk creato.
- [x] 42 combinazioni launcher offline e 8 test Pi passati; backup Pi univoci.
- [x] Qwen CUDA/RPC compilato; prove HTTP locali context2048 e context100000 riuscite, risposta 2 + 2 fa 4.
- [x] Context100000: CUDA modello9808 MiB, context3246 MiB, compute778 MiB; host modello1407 MiB. Inferenza RAM/VRAM verificata.
- [x] Ambiente OCR /home/ds4/aiutante-ocr/.venv installato; controllo dipendenze superato, wrapper collegato alle istruzioni Pi.
- [ ] Pesi/inferenza OCR reale e convivenza OCR/LLM: non collaudati; download pesi al primo uso.
- [ ] Pi installazione effettiva: non trovata nei percorsi controllati; diagnostica e ricerca migliorate.
- [ ] Prova fisica RPC su PC2 e contesti lunghi pieni: richiedono prova sui due PC, nessuna prestazione distribuita dichiarata.
- [x] Commit/push: d15c5f4 e f9c3037 pubblicati e SHA remoto verificato; file e permessi Linux precedenti preservati.

Context100000, prompt breve: prefill39.2t/s, generazione15.7t/s. Non confondere con38-40t/s ottenuti a2048. Collegamento desktop presente. Restano prove fisiche PC2, Pi effettivo e OCR con pesi.
