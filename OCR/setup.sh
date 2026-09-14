#!/usr/bin/env bash
set -euo pipefail
root=${AIUTANTE_OCR_HOME:-/home/ds4/aiutante-ocr}
source_dir=$(cd -- "$(dirname -- "$0")" && pwd)
mkdir -p "$root"
python3 -m venv "$root/.venv"
"$root/.venv/bin/python" -m pip install --upgrade pip
"$root/.venv/bin/python" -m pip install torch==2.10.0 torchvision==0.25.0 transformers==4.57.1 Pillow==12.1.1 matplotlib==3.10.8 einops==0.8.2 addict==2.4.0 easydict==1.13 pymupdf==1.27.2.2 psutil==7.2.2
cp -- "$source_dir/aiutante_ocr.py" "$root/aiutante_ocr.py"
"$root/.venv/bin/python" "$root/aiutante_ocr.py" --check
echo 'Dipendenze pronte. I pesi OCR verranno scaricati al primo utilizzo.'
