"""Ürün fotoğrafı hattı: arka planı ayır, 4:5 marka zeminine yumuşak gölgeyle yerleştir, iki boyutta webp yaz.

Girdi  brand_assets/products/<slug>-<n>.jpg
Çıktı  Corvano.Web/wwwroot/img/products/<slug>-<n>.webp (1280x1600) ve <slug>-<n>-thumb.webp (640x800)
--dark ile zemin #14261e olur ve dosya adlarına -dark eklenir.
"""
import argparse
from pathlib import Path

from PIL import Image, ImageFilter
from rembg import new_session, remove

ROOT = Path(__file__).resolve().parents[2]
LIGHT_GROUND = (0xF4, 0xF1, 0xEA)
DARK_GROUND = (0x14, 0x26, 0x1E)
SIZE = (1280, 1600)
THUMB = (640, 800)
FILL = 0.82  # konu tuvalin en fazla %82'sini kaplar
MODEL = "isnet-general-use"  # u2net ceket üstündeki ince zincirleri siliyor
ALPHA_FLOOR = 96  # altındaki yarı saydam ceket/zemin kalıntısı atılır
LABEL_FLOOR = 176  # parçalar bu eşikte ayrılır; ceket kenarı ile zincir arasındaki soluk köprü kopar
MIN_PART = 0.01  # kenara değmeyen parça yoksa, alanın %1'inden küçükler leke sayılır


def clean_alpha(cutout: Image.Image) -> Image.Image:
    """Soluk kalıntıyı ve kadraj kenarına değen parçaları (ceket kenarı, raf) atar; ürün kadrajın içinde kalır."""
    alpha = cutout.getchannel("A").point(lambda a: 0 if a < ALPHA_FLOOR else a)
    step = 4
    # küçültmede "bloktaki herhangi bir piksel" kuralı: ince zincir halkaları kaybolmaz
    small = alpha.point(lambda a: 255 if a >= LABEL_FLOOR else 0).reduce(step).point(lambda a: 255 if a else 0)
    small = small.filter(ImageFilter.MaxFilter(3))
    w, h = small.size
    margin = max(2, round(0.02 * max(w, h)))  # kenara birkaç piksel kala biten ceket kenarı da "değiyor" sayılır
    pixels = small.load()
    seen = [[False] * w for _ in range(h)]
    parts = []
    for sy in range(h):
        for sx in range(w):
            if seen[sy][sx] or pixels[sx, sy] == 0:
                continue
            stack, cells, touches = [(sx, sy)], [], False
            seen[sy][sx] = True
            while stack:
                x, y = stack.pop()
                cells.append((x, y))
                touches |= x < margin or y < margin or x >= w - margin or y >= h - margin
                for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                    if 0 <= nx < w and 0 <= ny < h and not seen[ny][nx] and pixels[nx, ny] > 0:
                        seen[ny][nx] = True
                        stack.append((nx, ny))
            parts.append((touches, cells))

    keep = [cells for touches, cells in parts if not touches]
    if sum(len(cells) for cells in keep) < MIN_PART * w * h:
        keep = [cells for _, cells in parts if len(cells) >= MIN_PART * w * h]
    mask_small = Image.new("L", small.size, 0)
    mask_pixels = mask_small.load()
    for cells in keep:
        for x, y in cells:
            mask_pixels[x, y] = 255
    mask = mask_small.resize(alpha.size, Image.NEAREST).filter(ImageFilter.MaxFilter(4 * step + 1))
    cleaned = cutout.copy()
    cleaned.putalpha(Image.composite(alpha, Image.new("L", alpha.size, 0), mask))
    return cleaned


def compose(cutout: Image.Image, dark: bool) -> Image.Image:
    """Kesilmiş (RGBA) konuyu 4:5 tuvale ortalar, altına marka tonunda yumuşak gölge düşer."""
    subject = cutout.crop(cutout.getbbox() or (0, 0, *cutout.size))
    scale = min(SIZE[0] * FILL / subject.width, SIZE[1] * FILL / subject.height)
    subject = subject.resize((round(subject.width * scale), round(subject.height * scale)), Image.LANCZOS)

    canvas = Image.new("RGBA", SIZE, DARK_GROUND if dark else LIGHT_GROUND)
    x = (SIZE[0] - subject.width) // 2
    y = (SIZE[1] - subject.height) // 2

    shadow_tone = (0, 0, 0) if dark else DARK_GROUND
    shadow_alpha = subject.getchannel("A").point(lambda a: a * (0.55 if dark else 0.30))
    shadow = Image.new("RGBA", SIZE, (*shadow_tone, 0))
    shadow_layer = Image.new("RGBA", subject.size, (*shadow_tone, 255))
    shadow_layer.putalpha(shadow_alpha)
    shadow.alpha_composite(shadow_layer, (x + 8, y + 24))
    shadow = shadow.filter(ImageFilter.GaussianBlur(32))

    canvas.alpha_composite(shadow)
    canvas.alpha_composite(subject, (x, y))
    return canvas.convert("RGB")


def process_file(source: Path, out_dir: Path, session, dark: bool = False) -> list[Path]:
    with Image.open(source) as photo:
        cutout = remove(photo.convert("RGB"), session=session)
    image = compose(clean_alpha(cutout), dark)

    suffix = "-dark" if dark else ""
    large = out_dir / f"{source.stem}{suffix}.webp"
    thumb = out_dir / f"{source.stem}{suffix}-thumb.webp"
    image.save(large, "WEBP", quality=82, method=6)
    image.resize(THUMB, Image.LANCZOS).save(thumb, "WEBP", quality=80, method=6)
    return [large, thumb]


def process_dir(source_dir: Path, out_dir: Path, dark: bool = False) -> list[Path]:
    out_dir.mkdir(parents=True, exist_ok=True)
    session = new_session(MODEL)
    written: list[Path] = []
    for source in sorted(source_dir.glob("*.jpg")):
        written += process_file(source, out_dir, session, dark)
    return written


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--src", type=Path, default=ROOT / "brand_assets" / "products")
    parser.add_argument("--out", type=Path, default=ROOT / "Corvano.Web" / "wwwroot" / "img" / "products")
    parser.add_argument("--dark", action="store_true", help="zemin #14261e")
    args = parser.parse_args()
    for path in process_dir(args.src, args.out, args.dark):
        print(path.relative_to(ROOT) if path.is_relative_to(ROOT) else path)


if __name__ == "__main__":
    main()
