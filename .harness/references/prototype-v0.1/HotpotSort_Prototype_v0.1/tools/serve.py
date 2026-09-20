"""Optional local HTTP server; double-clicking Web/standalone.html needs no server."""
from pathlib import Path
from http.server import ThreadingHTTPServer, SimpleHTTPRequestHandler
from functools import partial
import argparse
ROOT=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser();parser.add_argument('--port',type=int,default=8080);args=parser.parse_args()
server=ThreadingHTTPServer(('127.0.0.1',args.port),partial(SimpleHTTPRequestHandler,directory=str(ROOT/'Web')))
print(f'Open http://127.0.0.1:{args.port}  (Ctrl+C to stop)')
try:server.serve_forever()
except KeyboardInterrupt:pass
finally:server.server_close()
