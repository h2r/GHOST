#!/usr/bin/env python3
"""Local preview server for the GHOST project page.

Adds two things over `python3 -m http.server`:
  * HTTP Range support (206 Partial Content) — video streams and seeks
    properly, and Safari can play it at all.
  * Quiet handling of clients hanging up mid-transfer — no more
    BrokenPipeError tracebacks when the browser stops fetching the video.

GitHub Pages does all of this natively in production; this is only for
local preview.

Usage:  python3 serve.py [port]     (default port: 8000)
"""
import os
import re
import sys
from http import HTTPStatus
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer

CHUNK = 64 * 1024
RANGE_RE = re.compile(r"bytes=(\d*)-(\d*)$")


class RangeHandler(SimpleHTTPRequestHandler):
    protocol_version = "HTTP/1.1"

    def send_head(self):
        self.range_length = None
        header = self.headers.get("Range", "").strip()
        path = self.translate_path(self.path)
        m = RANGE_RE.match(header) if header else None
        if not (m and (m.group(1) or m.group(2)) and os.path.isfile(path)):
            return super().send_head()

        try:
            f = open(path, "rb")
        except OSError:
            self.send_error(HTTPStatus.NOT_FOUND, "File not found")
            return None

        stat = os.fstat(f.fileno())
        size = stat.st_size
        if m.group(1):
            start = int(m.group(1))
            end = int(m.group(2)) if m.group(2) else size - 1
        else:  # suffix range: bytes=-N (last N bytes)
            start = max(0, size - int(m.group(2)))
            end = size - 1
        end = min(end, size - 1)

        if start >= size or start > end:
            f.close()
            self.send_response(HTTPStatus.REQUESTED_RANGE_NOT_SATISFIABLE)
            self.send_header("Content-Range", "bytes */%d" % size)
            self.send_header("Content-Length", "0")
            self.end_headers()
            return None

        self.send_response(HTTPStatus.PARTIAL_CONTENT)
        self.send_header("Content-Type", self.guess_type(path))
        self.send_header("Accept-Ranges", "bytes")
        self.send_header("Content-Range", "bytes %d-%d/%d" % (start, end, size))
        self.send_header("Content-Length", str(end - start + 1))
        self.send_header("Last-Modified", self.date_time_string(stat.st_mtime))
        self.end_headers()
        f.seek(start)
        self.range_length = end - start + 1
        return f

    def copyfile(self, source, outputfile):
        if self.range_length is None:
            return super().copyfile(source, outputfile)
        remaining = self.range_length
        while remaining > 0:
            chunk = source.read(min(CHUNK, remaining))
            if not chunk:
                break
            outputfile.write(chunk)
            remaining -= len(chunk)


class QuietServer(ThreadingHTTPServer):
    def handle_error(self, request, client_address):
        exc = sys.exc_info()[1]
        if isinstance(exc, (BrokenPipeError, ConnectionResetError)):
            return  # client hung up mid-transfer — normal for media streaming
        super().handle_error(request, client_address)


if __name__ == "__main__":
    port = int(sys.argv[1]) if len(sys.argv) > 1 else 8000
    os.chdir(os.path.dirname(os.path.abspath(__file__)))
    with QuietServer(("", port), RangeHandler) as srv:
        print("Serving GHOST page at http://localhost:%d  (Ctrl-C to stop)" % port)
        try:
            srv.serve_forever()
        except KeyboardInterrupt:
            print("\nStopped.")
