# Unlimited-OCR per AIutante

Repository ufficiale clonato in `Unlimited-OCR/`, commit `d49ff64afffc1f47ab563dc1c589bc2f78808fa4`. La copia locale e i pesi non vengono inclusi in Git.

Per ripristinare la copia: `git clone https://github.com/baidu/Unlimited-OCR.git OCR/Unlimited-OCR`.

Il comando per Pi e `python3 OCR/aiutante_ocr.py documento.pdf --output risultato-ocr`. Accetta PDF e immagini, salva una cartella per pagina e termina liberando la GPU. `--check` controlla le dipendenze senza caricare pesi. Va eseguito nell'ambiente Python OCR, non necessariamente nello stesso ambiente di Pi.

Prerequisiti ufficiali Transformers: Python 3.12, torch 2.10.0, torchvision 0.25.0, transformers 4.57.1, Pillow 12.1.1, matplotlib 3.10.8, einops 0.8.2, addict 2.4.0, easydict 1.13, pymupdf 1.27.2.2, psutil 7.2.2. Installarli in un venv dedicato. Il primo uso scarica `baidu/Unlimited-OCR` ed esegue il codice custom del modello; `--model` permette una copia locale.

Stato PC1: dipendenze installate in `/home/ds4/aiutante-ocr/.venv`, controllo superato; wrapper in `/home/ds4/aiutante-ocr/aiutante_ocr.py`. `bash OCR/setup.sh` riproduce questo ambiente. I pesi OCR restano da scaricare al primo uso; inferenza OCR e convivenza con il LLM ancora da collaudare. Non avviare OCR automaticamente quando il modello occupa tutta la VRAM. Non arrestare il LLM senza richiesta dell'utente.

Fonte: https://github.com/baidu/Unlimited-OCR
