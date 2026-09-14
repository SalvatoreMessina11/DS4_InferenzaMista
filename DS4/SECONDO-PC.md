# Preparare il secondo PC Windows 11

Il repository contiene il programma e le modifiche; il modello da 81 GiB si scarica separatamente. Non caricarlo su GitHub.

1. Da PowerShell amministratore, se WSL non e gia installato:

       wsl --install -d Ubuntu-24.04

   Riavviare se richiesto e completare utente/password di Ubuntu.

2. Nel terminale **Ubuntu**, verificare `nvidia-smi`, poi eseguire:

       sudo apt-get update
       sudo apt-get install -y git
       git clone https://github.com/SalvatoreMessina11/DS4_InferenzaMista.git
       cd DS4_InferenzaMista
       bash installa-cuda-ubuntu.sh
       make -j4 ds4 CUDA_ARCH=sm_120

3. Scegliere una delle due possibilita per i pesi:

   - Download diretto ufficiale: `bash download_model.sh ds4f-q2`.
   - Copia da PC1: trasferire il GGUF in `gguf/` della copia Ubuntu, controllare SHA256 e creare il link `ds4flash.gguf` verso il file. Nome e hash in DUE-PC.md. Un SSD esterno evita un nuovo download Internet.

   Il file esistente sul PC1 e dentro Ubuntu: `/home/ds4/ds4/gguf/`. Da Esplora file Windows: `\\wsl.localhost\Ubuntu-24.04\home\ds4\ds4\gguf`.

4. Per il collaudo distribuito leggere DUE-PC.md. Servono gli IPv4 LAN reali, collegamento bidirezionale tra WSL e regole firewall ristrette ai due PC. Non aprire porte Internet sul router.

Non avviare una seconda istanza mentre e aperta la chat. La configurazione con la RTX 5070 12 GB e due PC non e ancora stata verificata.
