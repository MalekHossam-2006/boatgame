"""Command-line client for Assets/Editor/ClaudeBridge.

Usage:
  python ClaudeTools/bridge.py ping
  python ClaudeTools/bridge.py hierarchy
  python ClaudeTools/bridge.py screenshot path=ClaudeTools/out/a.png w=1280 h=720
  python ClaudeTools/bridge.py invoke type=MyTool method=Build [--body "text" | --body-file f]
  python ClaudeTools/bridge.py compile          # refresh, wait for recompile + reload, print errors
  python ClaudeTools/bridge.py wait             # wait until editor responds and is idle
"""
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path

PROJECT = Path(__file__).resolve().parent.parent
TOKEN_FILE = PROJECT / "UserSettings" / "ClaudeBridge.token"
BASE = "http://127.0.0.1:17890/"


def call(cmd, args=None, body="", timeout=130):
    token = TOKEN_FILE.read_text().strip()
    url = BASE + cmd + ("?" + urllib.parse.urlencode(args) if args else "")
    req = urllib.request.Request(url, data=body.encode("utf-8"), method="POST",
                                 headers={"X-Token": token})
    try:
        with urllib.request.urlopen(req, timeout=timeout) as r:
            return r.status, r.read().decode("utf-8")
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode("utf-8")


def wait_ready(max_wait=300):
    """Poll until bridge answers and editor is not compiling/updating."""
    t0 = time.time()
    while time.time() - t0 < max_wait:
        try:
            code, txt = call("ping", timeout=10)
            if code == 200 and '"compiling":false' in txt and '"updating":false' in txt:
                return txt
        except (urllib.error.URLError, ConnectionError, TimeoutError, OSError):
            pass  # domain reload in progress or editor busy
        time.sleep(1.0)
    raise SystemExit("bridge not ready after %ds" % max_wait)


def main(argv):
    if not argv:
        print(__doc__)
        return 1
    cmd, rest = argv[0], argv[1:]
    body = ""
    if "--body" in rest:
        i = rest.index("--body")
        body = rest[i + 1]
        rest = rest[:i] + rest[i + 2:]
    if "--body-file" in rest:
        i = rest.index("--body-file")
        body = Path(rest[i + 1]).read_text(encoding="utf-8")
        rest = rest[:i] + rest[i + 2:]
    args = dict(a.split("=", 1) for a in rest)

    if cmd == "wait":
        print(wait_ready())
        return 0
    if cmd == "compile":
        wait_ready()
        call("refresh")
        time.sleep(2)
        print(wait_ready())
        code, errs = call("compile_errors")
        print("compile errors:", errs)
        return 0 if errs.strip() == "none" else 2

    code, txt = call(cmd, args, body)
    print(txt)
    return 0 if code == 200 else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
