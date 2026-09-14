import shutil
from pathlib import Path

from PIL import Image

import process

SAMPLE = Path(__file__).resolve().parents[2] / "brand_assets" / "products" / "kravat-kiremit-1.jpg"


def test_a_sample_photo_becomes_a_4_to_5_webp_in_two_sizes(tmp_path):
    source_dir = tmp_path / "in"
    source_dir.mkdir()
    shutil.copy(SAMPLE, source_dir / SAMPLE.name)

    written = process.process_dir(source_dir, tmp_path / "out")

    assert sorted(p.name for p in written) == ["kravat-kiremit-1-thumb.webp", "kravat-kiremit-1.webp"]
    sizes = {}
    for path in written:
        with Image.open(path) as image:
            assert image.format == "WEBP"
            assert image.width * 5 == image.height * 4
            sizes[path.name] = image.size
    assert sizes["kravat-kiremit-1.webp"] == (1280, 1600)
    assert sizes["kravat-kiremit-1-thumb.webp"] == (640, 800)
