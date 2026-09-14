# Qwen3.8 27B IQ3_S

File richiesto: `Qwen3.8-27B-GSQ-RCO-IQ3_S.gguf`, 11771546784 byte.

SHA256: `64b53b64c7aa39f20a7e54bd80582fe595b1d745624ee8a72e92508c0326d810`.

Metadati verificati nel file: architettura `qwen35`, 64 livelli, context nativo 262144. Il nome commerciale del file rimane Qwen3.8. Fonte: https://huggingface.co/ISTA-DASLab/Qwen3.8-27B-GSQ-RCO-GGUF

Backend fissato a llama.cpp `bbdd9f246e9667f9aeb7ad11cca269466f981184`. Lo script `setup.sh` compila CUDA per le RTX 5070/5070 Ti e RPC. In Ubuntu installa prima git, cmake, ninja-build, libcurl4-openssl-dev e CUDA Toolkit. Esegui `bash setup.sh` per il PC con il modello, oppure `bash setup.sh --worker` per il worker senza scaricare pesi. La cartella motore predefinita e `/home/ds4/aiutante-qwen`; per cambiarla imposta `AIUTANTE_QWEN_HOME` e riporta lo stesso percorso nel launcher.

Il setup conserva il GGUF in `models/` e crea un collegamento Linux `model.gguf`, senza copiarlo. Il download supporta ripresa e controllo SHA256. Il modello sul PC1 e gia scaricato e verificato.

Il launcher usa RAM/VRAM senza lettura lazy dei pesi; con `-1` nei livelli GPU il backend sceglie secondo memoria libera. Un numero esplicito permette di spostare il resto dei livelli in RAM. Context e massimo output restano espliciti: se la memoria non basta, ridurli. I valori elevati non sono una promessa di poterli eseguire interamente sulla scheda.

Il worker espone la GPU su TCP 9912 all'IP selezionato; il coordinatore usa anche la GPU locale. Non attiviamo cache RPC su disco. RPC e sperimentale e va limitato alla rete fidata con firewall peer-only. Fonte tecnica: https://github.com/ggml-org/llama.cpp/blob/master/tools/rpc/README.md

Sono inclusi solo i pesi testuali richiesti: vision richiede il projector separato, MTP richiede la variante dedicata. Non vengono abilitate funzioni per cui mancano i pesi.
