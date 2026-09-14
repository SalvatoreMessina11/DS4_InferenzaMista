# AIutante

Launcher Windows per DeepSeek V4 Flash e Qwen3.8 27B IQ3_S, con chat locale o coordinatore/worker su due PC. Il motore gira in Ubuntu WSL: Windows rimane il sistema operativo del PC.

**Eseguibile:** [Utilities/Artifacts/AIutante.exe](Utilities/Artifacts/AIutante.exe). Icona incorporata fornita dall'utente. Il launcher conserva le precedenti impostazioni DS4 al primo avvio e salva le nuove in `%LOCALAPPDATA%/AIutante/settings.xml`.

## Avvio

1. Seleziona modello e ruolo: **Solo questo PC**, **Coordinatore** (prompt e calcolo) oppure **Worker** (solo calcolo).
2. Seleziona la distribuzione e l'utente Ubuntu che contengono i programmi. **Verifica** controlla motore, modello e GPU.
3. Imposta **Context** e **Output massimo**. Il primo contiene prompt, storia e risposta; il secondo limita ciascuna risposta. Il valore iniziale dell'output e 32768, senza il precedente limite fisso di 256. EOS, interruzione o esaurimento del contesto possono concludere prima la risposta.
4. Per due PC imposta IP locali/peer e configura la rete su entrambi. Avvia il worker e poi il coordinatore. Solo il coordinatore riceve prompt. **Dettagli avvio** spiega e mostra i parametri senza avviare nulla. **Aiuto Pi** descrive la modalita con strumenti; non serve una configurazione manuale separata.
5. **Stop** arresta la distribuzione WSL selezionata, inclusi i processi che contiene. Se altre distribuzioni sono attive, la VM WSL resta necessaria. La chiusura della finestra spegne Ubuntu solo dopo il controllo di terminali e servizi attivi.

## Motori e memoria

| | DeepSeek V4 Flash | Qwen3.8 27B IQ3_S |
|---|---|---|
| Motore | DS4 CUDA | llama.cpp CUDA/RPC |
| Livelli | 43, numerati 0–42 + output | 64 + output |
| Ripartizione | Livelli coordinatore configurabili; meta circa 21/22 | Automatica fra dispositivi secondo memoria; totale livelli GPU configurabile |
| Pesi | VRAM + cache RAM + SSD streaming | Pesi residenti RAM/VRAM, caricamento mlock e lazy mode disattivato |
| Due PC | GGUF necessario su entrambi | GGUF sul coordinatore; worker riceve i tensori via RPC |
| Context | Configurabile; memoria effettiva da verificare | Massimo dichiarato nel GGUF: 262144; memoria effettiva da verificare |

RAM e VRAM rimangono memorie distinte su ogni PC. Distribuire il modello amplia le risorse utilizzabili, ma non garantisce piu token/s: il Wi-Fi e i passaggi sequenziali fra GPU possono rallentare la generazione. Qwen permette di provare prima tutto sulla GPU locale o spostare alcuni livelli in RAM. `mlock` richiede un limite Linux sufficiente: un avviso di fallimento significa che il sistema puo usare swap; non equivale a garanzia di residenza.

Qwen: [installazione e limiti](Quen%2027B/README.md). DeepSeek: [guida del motore](DS4/README.md), [due PC](DS4/DUE-PC.md). Le API per Pi ascoltano solo su `127.0.0.1:8000`. Il worker RPC usa TCP 9912, da consentire solo all'altro PC nella rete fidata; non esporlo su Internet.

## Pi e OCR

Pi e opzionale e va trovato nell'ambiente Ubuntu selezionato. Il launcher cerca anche installazioni utente npm, NVM, pnpm, bun e Node portatile in ~/.local/share/pi-node. Un'installazione esclusivamente Windows non diventa automaticamente un comando Linux. La configurazione preserva altri provider e crea backup univoci.

[Unlimited-OCR](OCR/README.md) e disponibile come comando per immagini/PDF; la cartella contiene un wrapper richiamabile dall'agente. La disponibilita effettiva dipende dall'ambiente Python OCR e dai suoi pesi.

## Sorgenti e verifica

`DS4/` contiene il motore precedente e `launcher/`; `Quen 27B/` contiene il setup Qwen; `OCR/` contiene l'integrazione OCR; `Utilities/` contiene memoria, piano, icona ed eseguibile. I GGUF e i repository esterni clonati non vengono caricati su GitHub.

Build Windows: `powershell -ExecutionPolicy Bypass -File DS4/launcher/build.ps1`.

Stato dettagliato e prove: [WORKFLOW](Utilities/WORKFLOW.md), [MEMORY](Utilities/MEMORY.md), [validazione](DS4/launcher/VALIDATION.md). Il supporto su due PC richiede una prova fisica su entrambi: i test dei comandi non misurano la velocita della pipeline.

Default: Qwen + Pi Agent, Solo questo PC, context100000 e output32768. Qwen offre preset32768/65536/100000/131072/200000/262144 e valore personalizzato entro262144. Server e terminale Pi si aprono entrambi: Pi mostra lo stato di attesa fino a15minuti e conserva eventuali errori nella finestra. Le scelte successive vengono salvate.
