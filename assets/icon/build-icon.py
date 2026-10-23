"""Genera los PNG intermedios y icon.ico a partir de icon.svg e icon-small.svg.

Uso (desde la raíz del repo):  python assets/icon/build-icon.py
Requisitos: Python 3, Pillow (pip install pillow) y Microsoft Edge o Google Chrome
(se usan en modo headless solo para rasterizar el SVG).
"""
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

from PIL import Image

HERE = Path(__file__).resolve().parent
SIZES = [16, 24, 32, 48, 64, 128, 256]
SMALL_MAX = 32          # hasta este tamaño se usa icon-small.svg
RENDER = 1024           # tamaño de rasterizado antes de reducir con Lanczos

BROWSERS = [
    r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
    r"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
    r"C:\Program Files\Google\Chrome\Application\chrome.exe",
    r"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
]


def find_browser() -> str:
    for candidate in BROWSERS:
        if Path(candidate).exists():
            return candidate
    for name in ("msedge", "chrome", "google-chrome", "chromium"):
        found = shutil.which(name)
        if found:
            return found
    sys.exit("No se encontró Edge ni Chrome para rasterizar el SVG.")


def rasterize(browser: str, svg: Path, out: Path) -> Image.Image:
    with tempfile.TemporaryDirectory() as tmp:
        page = Path(tmp) / "page.html"
        page.write_text(
            f'<html><body style="margin:0;background:transparent">'
            f'<img src="{svg.as_uri()}" width="{RENDER}" height="{RENDER}" style="display:block">'
            f"</body></html>",
            encoding="utf-8",
        )
        subprocess.run(
            [browser, "--headless", "--disable-gpu", "--hide-scrollbars",
             "--default-background-color=00000000",
             f"--window-size={RENDER},{RENDER}",
             f"--screenshot={out}", page.as_uri()],
            check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
        )
    return Image.open(out).convert("RGBA").crop((0, 0, RENDER, RENDER))


def main() -> None:
    browser = find_browser()
    png_dir = HERE / "png"
    png_dir.mkdir(exist_ok=True)

    masters = {}
    for key, svg in (("large", HERE / "icon.svg"), ("small", HERE / "icon-small.svg")):
        masters[key] = rasterize(browser, svg, png_dir / f"_master-{key}.png")

    frames = []
    for size in SIZES:
        master = masters["small" if size <= SMALL_MAX else "large"]
        frame = master.resize((size, size), Image.LANCZOS)
        frame.save(png_dir / f"icon-{size}.png")
        frames.append(frame)

    for key in masters:
        (png_dir / f"_master-{key}.png").unlink()

    # Pillow guarda cada tamaño a partir de la imagen más grande; se pasan los frames ya preparados.
    frames[-1].save(HERE / "icon.ico", format="ICO", sizes=[(s, s) for s in SIZES],
                    append_images=frames[:-1])
    print("Generado:", HERE / "icon.ico")


if __name__ == "__main__":
    main()
