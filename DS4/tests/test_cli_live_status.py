"""Validate terminal-title status and absence of escapes in redirected output."""
import os
import pty
import select
import subprocess
import sys

binary = sys.argv[1]
plain = subprocess.run([binary], capture_output=True, check=True)
assert plain.stdout == b'' and plain.stderr == b'', plain

def capture(enabled):
    master, slave = pty.openpty()
    proc = subprocess.Popen([binary], stdout=subprocess.PIPE, stderr=slave,
                            env=dict(os.environ, DS4_LIVE_STATUS=str(enabled)))
    os.close(slave)
    chunks=[]
    try:
        while True:
            if select.select([master], [], [], 5)[0]:
                try: data=os.read(master, 65536)
                except OSError: break
                if not data: break
                chunks.append(data)
            elif proc.poll() is not None: break
        stdout,_=proc.communicate(timeout=5)
        assert proc.returncode == 0 and stdout == b''
    finally:
        os.close(master)
        if proc.poll() is None: proc.kill();proc.wait()
    return b''.join(chunks)

status=capture(1)
assert status.count(b'Prefill 16/64') >= 2, status
assert b'Generazione 3/32' in status and b'Generazione terminata: 3' in status, status
assert b'token/s medi' in status, status
assert capture(0) == b''
print('PASS: live timer, chunk-wait heartbeat, monotonic count, decode rate, final state, redirected output, opt-out')
