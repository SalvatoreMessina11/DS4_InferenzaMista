"""On-demand Unlimited-OCR CLI for a local agent; no persistent GPU server."""
import argparse
import importlib.util
import json
from pathlib import Path
import sys


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('input', nargs='?', type=Path)
    parser.add_argument('--output', type=Path)
    parser.add_argument('--model', default='baidu/Unlimited-OCR')
    parser.add_argument('--check', action='store_true')
    parser.add_argument('--max-tokens', type=int, default=32768)
    args = parser.parse_args()
    missing = [name for name in ('torch', 'transformers', 'fitz', 'PIL', 'einops', 'addict', 'easydict')
               if importlib.util.find_spec(name) is None]
    if args.check:
        print(json.dumps({'ready_dependencies': not missing, 'missing': missing,
                          'model': args.model, 'model_download_verified': False}))
        return int(bool(missing))
    if not args.input or not args.input.is_file() or not args.output:
        parser.error('Specifica un file esistente e --output CARTELLA')
    if missing:
        parser.error('Dipendenze mancanti: ' + ', '.join(missing) + '. Vedi OCR/README.md')
    if not 1 <= args.max_tokens <= 32768:
        parser.error('--max-tokens deve essere tra 1 e 32768')
    import torch
    from transformers import AutoModel, AutoTokenizer
    if not torch.cuda.is_available():
        parser.error('CUDA non disponibile in questo ambiente Python')
    args.output.mkdir(parents=True, exist_ok=True)
    # Official implementation uses custom code from the selected model repo.
    tokenizer = AutoTokenizer.from_pretrained(args.model, trust_remote_code=True)
    model = AutoModel.from_pretrained(args.model, trust_remote_code=True,
        use_safetensors=True, torch_dtype=torch.bfloat16).eval().cuda()
    images = [str(args.input.resolve())]
    if args.input.suffix.lower() == '.pdf':
        import fitz
        pages = args.output / 'pages'
        pages.mkdir(exist_ok=True)
        images = []
        with fitz.open(args.input) as doc:
            for index, page in enumerate(doc):
                target = pages / f'{index + 1:05d}.png'
                page.get_pixmap(matrix=fitz.Matrix(2, 2)).save(target)
                images.append(str(target.resolve()))
    # Process one page at a time to bound visual input memory.
    with torch.inference_mode():
        for index, filename in enumerate(images):
            target = args.output / f'page-{index + 1:05d}'
            target.mkdir(exist_ok=True)
            model.infer(tokenizer, prompt='<image>document parsing.', image_file=filename,
                output_path=str(target), base_size=1024, image_size=640, crop_mode=True,
                max_length=args.max_tokens, no_repeat_ngram_size=35, ngram_window=128,
                save_results=True)
    print(json.dumps({'output': str(args.output.resolve()), 'pages': len(images)}))
    return 0


if __name__ == '__main__':
    sys.exit(main())
