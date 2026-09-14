# Due PC NVIDIA: configurazione sperimentale

## Launcher con Pi, Ethernet e context

Il nuovo exe offre Chat DS4 / Pi Agent, rete esistente / Ethernet diretto, context 100K/300K/custom e split personalizzato. Consulta [guida launcher](launcher/README.md) e [validazione](launcher/VALIDATION.md). I comandi shell storici sotto restano esempi con ctx 2048; il launcher usa il valore scelto, default 100000.

PC1 coordinatore Pi via Wi-Fi: scegli rete esistente, Coordinatore, GPU effettiva, Pi Agent e context, imposta gli IP, poi Verifica e Avvia. Serve Pi installato nell'utente Ubuntu del coordinatore.

PC2 worker: scegli rete esistente, Worker, GPU effettiva, **stesso context e split**, poi Verifica e Avvia prima del coordinatore. Il worker riprova il collegamento mentre il coordinatore parte. Non installare Pi sul worker. Pi viene aperto sul coordinatore solo dopo la readiness HTTP localhost:8000.

Ethernet diretto: collega il cavo, scegli Ethernet diretto e Configura rete su entrambi. Seleziona numeri fisici PC1/PC2 opposti, ottenendo 192.168.250.1/.2; puoi comunque assegnare il ruolo coordinatore a uno qualsiasi dei due. Non sono necessarie regole LAN per 8000.

Le metriche live della Chat DS4 stanno nel titolo della console del coordinatore. Il conteggio prefill remoto viene confermato per blocchi; il tempo si aggiorna anche durante l'attesa. Prestazioni su due PC, tool Pi reali e allocazione di contesti 100K/300K restano da collaudare sul sistema completo.

Stato: avvii preparati, non ancora collaudati su due computer. La chat su un PC e gia stata provata. Non confondere i due risultati.

PC1: RTX 5070 Ti 16 GB + 32 GB RAM. PC2: RTX 5070 12 GB + 32 GB RAM (dati forniti dall'utente).
Modello: DeepSeek V4 Flash 0731 Q2, 86720111488 byte.
SHA256: ca22ae2f838e14077c22bc1c1417b71b45b5e5a3687bd96c2ac6e17fdb6261c0.

## Come funziona

Entrambi i PC calcolano parti dello stesso modello. Solo il coordinator accetta prompt e mostra le risposte; il worker resta aperto come processo di calcolo, senza una chat indipendente.
Puoi scegliere quale PC coordina prima di avviare la coppia. La scelta non trasferisce una conversazione gia in corso e non si cambia durante la generazione.
Pipeline TCP predefinita: PC1 esegue i livelli 0-23, PC2 i livelli 24-42 e la testa di output.
Questa divisione e un punto di partenza, non un bilanciamento misurato.
Entrambi usano CUDA e SSD streaming. Le due VRAM non diventano un unico banco da 28 GB.
La generazione di una singola risposta attraversa entrambi i PC in sequenza: nessun raddoppio di velocita garantito.
La configurazione CUDA multi-GPU --gpu-devices riguarda GPU nello stesso sistema, non due PC Wi-Fi.

## Secondo PC

1. Se Windows: installare WSL2 e Ubuntu 24.04, poi verificare nvidia-smi dentro Ubuntu.
2. Copiare/clonare la nostra versione con le due correzioni CUDA, non soltanto l'originale antirez.
3. Compilare con CUDA 13.2: make -j4 ds4 CUDA_ARCH=sm_120.
4. Tenere codice e GGUF nel filesystem Linux. Il launcher usa la propria cartella; per un'altra cartella impostare DS4_ROOT.
5. Copiare lo stesso GGUF dall'SSD del PC1 al PC2, anche tramite disco esterno. Non serve riscaricarlo da Internet. In alternativa: bash download_model.sh ds4f-q2.
6. Controllare SHA256 sul PC2; creare ds4flash.gguf come link al GGUF. Prevedere almeno 100 GiB liberi per modello e strumenti, oltre allo spazio per Windows.
7. Non eseguire alla cieca setup-cuda-wsl.sh sul PC2: contiene il percorso Windows del PC1. Adattare il percorso della copia e l'utente Linux.

## Rete

Stesso Wi-Fi domestico, non rete ospiti con isolamento client. Preferibile Ethernet allo stesso router se disponibile.
WSL2 normalmente usa NAT: essere sullo stesso Wi-Fi non rende automaticamente raggiungibile il processo Linux.
Su Windows 11 compatibile, valutare networkingMode=mirrored nella sezione [wsl2] di .wslconfig su entrambi i PC.
La modifica richiede l'arresto di WSL: farla solo dopo /quit, per non perdere una risposta in corso.
Consentire TCP 9911 verso il coordinator e TCP 9912 verso il worker nei firewall Windows/Hyper-V, limitando l'accesso all'IP dell'altro PC. Se inverti i ruoli, vanno adeguate anche le regole: quelle gia predisposte per PC1 coordinator non bastano automaticamente per PC2 coordinator.
Non disabilitare l'intero firewall. Non inoltrare queste porte dal router verso Internet: il protocollo ds4 non ha autenticazione o cifratura.
Con NAT occorre invece configurare gli inoltri Windows->WSL e verificare anche il collegamento di ritorno al worker. Il percorso mirrored e quello da provare per primo.

## Avvio, dopo la verifica della rete

Chiudere la chat singola con /quit. Stessa versione del codice su entrambi i PC.
Da Ubuntu nella cartella contenente due-pc.sh:

Puoi eseguire `bash due-pc.sh` senza argomenti per scegliere il ruolo dal menu. Impostare lo stesso numero di livelli coordinator su entrambi i PC.

### Chat sul PC1 (5070 Ti), worker sul PC2 (5070)

PC1, IPv4 LAN verificato 192.168.1.12:

    bash due-pc.sh coordinator 192.168.1.12 24

PC2 (passare l'IP del PC1):

    bash due-pc.sh worker 192.168.1.12 24

### Chat sul PC2 (5070), worker sul PC1 (5070 Ti)

PC2, IPv4 LAN comunicato 192.168.1.10:

    bash due-pc.sh coordinator 192.168.1.10 19

PC1:

    bash due-pc.sh worker 192.168.1.10 19

In questo caso PC2 esegue livelli 0-18 e PC1 livelli 19-42 piu output. Le due divisioni sono ipotesi iniziali da misurare, non garanzie di memoria o prestazioni.

Per cambiare ruolo: terminare la chat e il worker in modo ordinato, adeguare le regole firewall, poi riavviare con la coppia di comandi scelta. Chiudere prima i listener diagnostici sulle porte 9911/9912. Non interrompere il trasferimento HTTP 9913 se ancora in corso.

`DS4_DRY_RUN=1 bash due-pc.sh ...` stampa il comando senza caricare il modello o aprire porte.

La chat e soltanto sul coordinator scelto. Non usare curl/nc sulle porte del protocollo mentre ds4 attende il peer.
Prima prova: prompt breve, confronto risposta e token/s con la baseline singolo PC, misura VRAM su entrambi.
Se la combinazione pipeline+SSD incontra errori di caricamento/cache, servono ulteriori correzioni; non e ancora certificata dal nostro test locale.

## GitHub e continuita

Consigliato un fork personale o un repository dedicato, con le correzioni C/CUDA, launcher, questa guida e note di collaudo.
Non caricare il GGUF, binari, dump di memoria, log di conversazioni o copie .before-local-streaming.
Repository personale: https://github.com/SalvatoreMessina11/DS4_InferenzaMista. Il modello resta separato.
Le istruzioni per riprendere su un altro PC devono stare nei file: SETUP-LOCALE.md per il PC1, DUE-PC.md per la prova distribuita, HANDOFF-DS4.md per il passaggio di lavoro.

Fonti:
- https://github.com/antirez/ds4/blob/main/docs/DISTRIBUTED.md
- https://learn.microsoft.com/en-us/windows/wsl/networking
