# Passaggio di lavoro: DS4 CUDA SSD su Windows/WSL

Leggere AGENT.md, SECONDO-PC.md e DUE-PC.md prima di intervenire. SETUP-LOCALE.md contiene ulteriori note solo nella copia privata del PC1.
Obiettivo utente: utilizzare DeepSeek V4 Flash su due PC domestici via Wi-Fi, PC1 5070 Ti 16GB/32GB RAM e PC2 5070 12GB/32GB RAM.

Base upstream bd66c402070042bf0a79ad6ece8242de4c93680c.
Due modifiche essenziali gia applicate a ds4.c e ds4_cuda.cu: caricamento esperti CUDA anche per prefill da un token; esclusione del percorso MMQ IQ2 full-expert quando SSD streaming e attivo.
Su PC1 esiste una copia Windows e una copia operativa Linux /home/ds4/ds4: sincronizzare esplicitamente le modifiche, compilare nella copia Linux.
Una prova reale breve ha risposto correttamente a 2+2: prefill 1.80 t/s, decode 0.80 t/s, circa 13.3 GiB VRAM totale.
Questo non costituisce una verifica numerica completa delle correzioni. Nessun test Metal o distribuito eseguito.
Configurazione singolo PC: --cuda --ssd-streaming --ctx 2048 --prefill-chunk 64 --nothink; DS4_CUDA_NO_DIRECT_IO=1.
Prefill=1 dopo la correzione funziona ma e molto lento. Non sceglierlo come default.
Il modello completo e verificato con SHA256 riportato in DUE-PC.md. Non riscaricarlo sul PC1.

Prossimo passo: identificare OS, IP, disco libero e WSL del PC2; predisporre rete bidirezionale; trasferire codice e pesi; provare due-pc.sh, che e solo preparato e non ancora collaudato.
Non eseguire due processi modello sullo stesso PC mentre la chat e attiva.
Non pubblicare log o pesi su GitHub. Repository personale: https://github.com/SalvatoreMessina11/DS4_InferenzaMista.
