# Corvano

Erkek giyim e-ticaret (.NET 10 MVC). Mimari ve süreç: `CLAUDE.md`.

## Ürün görseli hattı

Ham fotoğraflar `brand_assets/products/<slug>-<n>.jpg`; çıktı `Corvano.Web/wwwroot/img/products/` (1280x1600 webp + 640x800 `-thumb`, `--dark` ile koyu zemin ve `-dark` eki).

```sh
py -3.12 -m venv tools/img/.venv && tools/img/.venv/Scripts/pip install -r tools/img/requirements.txt
tools/img/.venv/Scripts/python tools/img/process.py && tools/img/.venv/Scripts/python tools/img/process.py --dark
cd tools/img && .venv/Scripts/python -m pytest -q
```
