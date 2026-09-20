"""Loopback-only config editor. No model/API calls and no arbitrary command endpoint."""
from __future__ import annotations
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
import json
from pathlib import Path
import secrets
from urllib.parse import urlsplit
from .common import HarnessError, require, dump, safe_path, json_read
from . import models

ASSETS = Path(__file__).resolve().parent / "web"


def status_payload(root):
    """Backend-owned UI names plus the exact config, file, and dispatch bindings."""
    from . import engine
    st = models.status(root)
    for row in st["rows"]:
        role = row["role"]
        row["backend_binding"] = {
            "config_key": f"agents.{role}",
            "agent_file": f".codex/agents/{models.ROLES[role]}",
            "dispatch_steps": [step for step, spec in engine.STEPS.items() if spec["role"] == role],
        }
    return st


def server(root, port=8765):
    root = Path(root).resolve()
    token = secrets.token_urlsafe(32)
    class Handler(BaseHTTPRequestHandler):
        server_version = "HarnessDashboard/1.1"
        def log_message(self, fmt, *args):
            pass
        def respond(self, code, body, mime="application/json; charset=utf-8"):
            b = body if isinstance(body, bytes) else body.encode("utf-8")
            self.send_response(code)
            self.send_header("Content-Type", mime)
            self.send_header("Content-Length", str(len(b)))
            self.send_header("Cache-Control", "no-store")
            self.send_header("X-Content-Type-Options", "nosniff")
            self.send_header("Referrer-Policy", "no-referrer")
            self.send_header("Content-Security-Policy", "default-src 'none'; script-src 'self'; style-src 'self'; connect-src 'self'; img-src 'self'; base-uri 'none'; frame-ancestors 'none'")
            self.end_headers(); self.wfile.write(b)
        def guard(self, api=False, post=False):
            address = f"127.0.0.1:{self.server.server_port}"
            require(self.headers.get("Host") == address, "Invalid Host; use the printed loopback URL")
            if api:
                supplied = self.headers.get("X-Harness-Token", "")
                require(secrets.compare_digest(supplied, token), "Missing/invalid dashboard token")
            if post:
                require(self.headers.get("Origin") == "http://" + address, "Invalid Origin")
                require(self.headers.get("Content-Type", "").split(";")[0] == "application/json", "JSON required")
        def do_GET(self):
            try:
                path = urlsplit(self.path).path
                self.guard(api=path.startswith("/api/"))
                if path == "/api/status":
                    st = status_payload(root)
                    st["transactions"] = []
                    for p in safe_path(root, ".harness/model-transactions").glob("*/journal.json"):
                        j = json_read(p)
                        st["transactions"].append({"id": j["id"], "at": j["at"], "status": j["status"]})
                    st["transactions"].sort(key=lambda x: x["at"], reverse=True)
                    self.respond(200, dump(st)); return
                mapping = {"/": ("index.html", "text/html; charset=utf-8"),
                           "/app.js": ("app.js", "text/javascript; charset=utf-8"),
                           "/style.css": ("style.css", "text/css; charset=utf-8")}
                if path not in mapping:
                    self.respond(404, dump({"error": "Not found"})); return
                name, mime = mapping[path]
                self.respond(200, (ASSETS / name).read_bytes(), mime)
            except (HarnessError, ValueError, OSError, KeyError) as e:
                self.respond(400, dump({"error": str(e)}))
        def do_POST(self):
            try:
                self.guard(api=True, post=True)
                size = int(self.headers.get("Content-Length", "0"))
                require(0 < size <= 262144, "Invalid request size")
                data = json.loads(self.rfile.read(size).decode("utf-8"))
                require(isinstance(data, dict), "Object required")
                path = urlsplit(self.path).path
                if path in ("/api/preview", "/api/apply"):
                    text = models.serialize(data["config"])
                    reconcile = data.get("reconcile") is True
                    if path == "/api/preview":
                        out = models.preview(root, text, reconcile=reconcile)
                        result = {"etag": out["etag"], "diff": out["diff"]}
                    else:
                        require(data.get("confirmed") is True, "Explicit Apply confirmation required")
                        result = models.apply(root, text, data["etag"], reconcile=reconcile)
                elif path == "/api/rollback":
                    require(data.get("confirmed") is True, "Explicit rollback confirmation required")
                    result = models.rollback(root, data["transaction"])
                else:
                    self.respond(404, dump({"error": "Not found"})); return
                self.respond(200, dump(result))
            except (HarnessError, ValueError, OSError, KeyError, TypeError) as e:
                self.respond(400, dump({"error": str(e)}))
    httpd = ThreadingHTTPServer(("127.0.0.1", port), Handler)
    httpd.daemon_threads = True
    return httpd, token


def serve(root, port=8765):
    httpd, token = server(root, port)
    print(f"Dashboard: http://127.0.0.1:{httpd.server_port}/#token={token}", flush=True)
    print("Only this workspace. Ctrl+C stops. No agents or model API calls are started.", flush=True)
    try:
        httpd.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        httpd.server_close()
