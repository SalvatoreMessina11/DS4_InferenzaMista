"""Embedded Pi configuration/readiness helper; no package installation or model access."""
import argparse
import datetime
import json
import os
from pathlib import Path
import shutil
import tempfile
import time
import urllib.request
import uuid


def provider(context, max_output=16384, model="ds4"):
    if not 2048 <= context <= 1000000:
        raise ValueError('Context deve essere tra 2048 e 1000000')
    result = {
        'baseUrl': 'http://127.0.0.1:8000/v1', 'api': 'openai-completions',
        'apiKey': 'dsv4-local',
        'compat': {
            'supportsStore': False, 'supportsDeveloperRole': False,
            'supportsReasoningEffort': True, 'supportsUsageInStreaming': True,
            'maxTokensField': 'max_tokens', 'supportsStrictMode': False,
            'thinkingFormat': 'deepseek', 'requiresReasoningContentOnAssistantMessages': True,
        },
        'models': [{
            'id': 'deepseek-v4-flash', 'name': 'DeepSeek V4 Flash Q2 Local',
            'reasoning': True,
            'thinkingLevelMap': {'off': None, 'minimal': 'low', 'low': 'low',
                                 'medium': 'medium', 'high': 'high', 'xhigh': 'xhigh'},
            'input': ['text'], 'contextWindow': context,
            'maxTokens': min(max_output, context - 1),
            'cost': dict(input=0, output=0, cacheRead=0, cacheWrite=0),
        }],
    }

    if not 1 <= max_output <= 1000000:
        raise ValueError('Output massimo deve essere tra 1 e 1000000')
    if model == 'qwen':
        if context > 262144:
            raise ValueError('Context Qwen massimo 262144')
        result['compat'] = {'supportsStore': False, 'supportsDeveloperRole': False, 'maxTokensField': 'max_tokens'}
        result['models'][0].update(id='qwen3.8-27b', name='Qwen3.8 27B IQ3_S Local')
        result['models'][0].pop('thinkingLevelMap')
    return result


def configure(path, context, max_output=16384, model='ds4'):
    replacement = provider(context, max_output, model)
    path = Path(path).expanduser()
    data = json.loads(path.read_text(encoding='utf-8')) if path.exists() else {}
    if not isinstance(data, dict) or not isinstance(data.get('providers', {}), dict):
        raise ValueError('Config Pi non valida: root e providers devono essere oggetti JSON')
    # Validate before creating any backup or changing the original.
    if data.get('providers', {}).get(model) == replacement:
        return
    path.parent.mkdir(parents=True, exist_ok=True)
    if path.exists():
        stamp = datetime.datetime.now().strftime('%Y%m%d-%H%M%S-%f') + '-' + uuid.uuid4().hex
        shutil.copy2(path, str(path) + '.backup.' + stamp)
    data.setdefault('providers', {})[model] = replacement
    fd, temp = tempfile.mkstemp(prefix='.models-', suffix='.json', dir=path.parent)
    try:
        with os.fdopen(fd, 'w', encoding='utf-8') as stream:
            json.dump(data, stream, indent=2, ensure_ascii=False)
            stream.write('\n')
            stream.flush()
            os.fsync(stream.fileno())
        os.replace(temp, path)
    finally:
        if os.path.exists(temp):
            os.unlink(temp)


def process_alive(pid):
    try:
        os.kill(pid, 0)
        stat = Path('/proc') / str(pid) / 'stat'
        return not stat.exists() or stat.read_text().rsplit(')', 1)[1].split()[0] != 'Z'
    except (ProcessLookupError, FileNotFoundError):
        return False


def wait_ready(pid_file, timeout=300, interval=1, url='http://127.0.0.1:8000/v1/models',
               alive=process_alive, model='ds4'):
    deadline = time.monotonic() + timeout
    pid_file = Path(pid_file)
    opener = urllib.request.build_opener(urllib.request.ProxyHandler({}))
    while time.monotonic() < deadline:
        if Path(str(pid_file) + '.finished').exists():
            raise RuntimeError('Avvio server terminato; controlla la console server. Pi non avviato.')
        if pid_file.exists():
            value = pid_file.read_text().strip()
            if not value:
                time.sleep(min(interval, max(0, deadline-time.monotonic())))
                continue
            pid = int(value)
            if not alive(pid):
                raise RuntimeError('ds4-server terminato prima della readiness; controlla la console server')
            try:
                with opener.open(url, timeout=min(2, max(.01, deadline-time.monotonic()))) as response:
                    data = json.load(response)
                if any(item.get('id') == ('qwen3.8-27b' if model == 'qwen' else 'deepseek-v4-flash') for item in data.get('data', [])):
                    if not alive(pid):
                        raise RuntimeError('ds4-server terminato durante la readiness')
                    return
            except (OSError, ValueError, AttributeError, TypeError):
                pass
        time.sleep(min(interval, max(0, deadline-time.monotonic())))
    raise TimeoutError('Server non pronto entro il timeout; Pi non avviato. Controlla il worker e la console server.')


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('action', choices=['configure', 'wait'])
    parser.add_argument('--context', type=int, default=100000)
    parser.add_argument('--model', choices=['ds4', 'qwen'], default='ds4')
    parser.add_argument('--max-output', type=int, default=16384)
    parser.add_argument('--config', default='~/.pi/agent/models.json')
    parser.add_argument('--pid-file')
    parser.add_argument('--timeout', type=int, default=300)
    args = parser.parse_args()
    if args.action == 'configure':
        configure(args.config, args.context, args.max_output, args.model)
        print('Provider Pi ds4 configurato; context=' + str(args.context))
    else:
        if not args.pid_file:
            parser.error('--pid-file necessario')
        wait_ready(args.pid_file, args.timeout, model=args.model)
        print('DS4_READY')


if __name__ == '__main__':
    main()
