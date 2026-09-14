import http.server
import json
from pathlib import Path
import tempfile
import threading
import unittest
from unittest import mock
import pi_support as pi


class PiTests(unittest.TestCase):
    def test_output_and_qwen_provider(self):
        self.assertEqual(pi.provider(100000, 32000)['models'][0]['maxTokens'], 32000)
        self.assertEqual(pi.provider(2048, 32000)['models'][0]['maxTokens'], 2047)
        with self.assertRaises(ValueError): pi.provider(100000, 0)
        with self.assertRaises(ValueError): pi.provider(300000, 100, 'qwen')
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / 'models.json'
            pi.configure(path, 100000, 16000)
            pi.configure(path, 262144, 32000, 'qwen')
            providers = json.loads(path.read_text())['providers']
            self.assertIn('ds4', providers)
            self.assertEqual(providers['qwen']['models'][0]['id'], 'qwen3.8-27b')
            self.assertEqual(providers['qwen']['models'][0]['maxTokens'], 32000)

    def test_merge_backup_context(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp)/'models.json'
            original = {'providers': {'other': {'secret': 'fixture'}}, 'extra': 42}
            path.write_text(json.dumps(original))
            for ctx in (100000, 300000, 131072, 2048):
                pi.configure(path, ctx)
                data = json.loads(path.read_text())
                self.assertEqual(data['providers']['other'], original['providers']['other'])
                self.assertEqual(data['extra'], 42)
                model = data['providers']['ds4']['models'][0]
                self.assertEqual(model['contextWindow'], ctx)
                self.assertLess(model['maxTokens'], ctx)
            self.assertEqual(len(list(Path(tmp).glob('*.backup.*'))), 4)
            before = path.read_bytes()
            pi.configure(path, 2048)
            self.assertEqual(before, path.read_bytes())

    def test_invalid_no_changes(self):
        for content in ('{invalid', '[]', '{"providers": []}'):
            with tempfile.TemporaryDirectory() as tmp:
                path = Path(tmp)/'models.json'
                path.write_text(content)
                with self.assertRaises(ValueError):
                    pi.configure(path, 100000)
                self.assertEqual(path.read_text(), content)
                self.assertEqual(len(list(Path(tmp).iterdir())), 1)

    def test_atomic_failure_preserves_original(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp)/'models.json';path.write_text('{}')
            with mock.patch.object(pi.os, 'replace', side_effect=OSError('fixture')):
                with self.assertRaises(OSError): pi.configure(path, 100000)
            self.assertEqual(path.read_text(), '{}')

    def test_readiness_local_http(self):
        class Handler(http.server.BaseHTTPRequestHandler):
            def do_GET(self):
                self.send_response(200);self.end_headers()
                self.wfile.write(b'{"data":[{"id":"deepseek-v4-flash"}]}')
            def log_message(self, *args): pass
        server = http.server.ThreadingHTTPServer(('127.0.0.1', 0), Handler)
        thread = threading.Thread(target=server.serve_forever, daemon=True);thread.start()
        try:
            with tempfile.TemporaryDirectory() as tmp:
                path=Path(tmp)/'pid';path.write_text('123')
                pi.wait_ready(path, timeout=2, alive=lambda _: True,
                              url='http://127.0.0.1:%d/v1/models' % server.server_port)
                with self.assertRaises(RuntimeError):
                    pi.wait_ready(path, timeout=2, alive=lambda _: False)
        finally:
            server.shutdown();server.server_close();thread.join()

    def test_timeout_missing_pid(self):
        with tempfile.TemporaryDirectory() as tmp:
            with self.assertRaises(TimeoutError):
                pi.wait_ready(Path(tmp)/'missing', timeout=.03, interval=.01)

    def test_server_preflight_failed(self):
        with tempfile.TemporaryDirectory() as tmp:
            path=Path(tmp)/'pid'
            Path(str(path)+'.finished').write_text('failed')
            with self.assertRaises(RuntimeError):pi.wait_ready(path, timeout=.1)

    def test_context_limits(self):
        for value in (0, 2047, 1000001):
            with self.assertRaises(ValueError):pi.provider(value)


if __name__ == '__main__': unittest.main()
