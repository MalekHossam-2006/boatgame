"""Export docs/GDD.md to docs/GDD.pdf.

Converts the Markdown to a print-styled HTML page and prints it with headless Chrome or Edge.
Requires: pip install markdown

Usage (from the repository root):
    python docs/tools/build_gdd.py
"""
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

import markdown

DOCS = Path(__file__).resolve().parent.parent
SOURCE = DOCS / "GDD.md"
OUTPUT = DOCS / "GDD.pdf"

BROWSERS = [
    r"C:\Program Files\Google\Chrome\Application\chrome.exe",
    r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
    r"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
    "google-chrome", "chromium", "msedge",
]

CSS = """
@page { size: A4; margin: 16mm 15mm; }
body { font-family: "Segoe UI", Arial, sans-serif; font-size: 10.5pt; line-height: 1.45; color: #13232e; }
h1 { font-size: 24pt; color: #0b2233; border-bottom: 4px solid #ffc94a; padding-bottom: 6px; margin-top: 0; }
h2 { font-size: 15pt; color: #0b2233; border-bottom: 1px solid #c9d6de; padding-bottom: 3px;
     margin-top: 22px; break-after: avoid; }
h3 { font-size: 12pt; color: #2a7aa6; margin-bottom: 4px; break-after: avoid; }
h1 + p strong { color: #2a7aa6; }
table { border-collapse: collapse; width: 100%; margin: 8px 0 12px; break-inside: avoid; }
th:empty { display: none; }
th, td { border: 1px solid #c9d6de; padding: 5px 7px; vertical-align: top; text-align: left; }
th { background: #0b2233; color: #fff; }
tr:nth-child(even) td { background: #f4f8fa; }
blockquote { margin: 10px 0; padding: 6px 12px; background: #fff7e0; border-left: 4px solid #ffc94a; }
code { background: #eef3f6; padding: 0 3px; border-radius: 3px; font-size: 9.5pt; }
pre { background: #eef3f6; padding: 8px; border-radius: 4px; }
img { max-width: 100%; }
p > img { display: block; margin: 6px auto; max-height: 330px; }
td img { max-height: 210px; display: block; margin: auto; }
hr { border: 0; border-top: 1px solid #c9d6de; margin: 18px 0; }
em { color: #8a5a00; }
"""


def find_browser():
    for b in BROWSERS:
        path = shutil.which(b) or (b if Path(b).exists() else None)
        if path:
            return path
    sys.exit("Chrome or Edge not found")


def main():
    body = markdown.markdown(SOURCE.read_text(encoding="utf-8"), extensions=["tables", "sane_lists"])
    html = (f'<!doctype html><html><head><meta charset="utf-8"><base href="{DOCS.as_uri()}/">'
            f"<title>Boat Game - GDD</title><style>{CSS}</style></head><body>{body}</body></html>")
    with tempfile.TemporaryDirectory() as tmp:
        page = Path(tmp) / "gdd.html"
        page.write_text(html, encoding="utf-8")
        subprocess.run([find_browser(), "--headless=new", "--disable-gpu", "--no-pdf-header-footer",
                        f"--print-to-pdf={OUTPUT}", page.as_uri()], check=True,
                       stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    print("wrote", OUTPUT)


if __name__ == "__main__":
    main()
