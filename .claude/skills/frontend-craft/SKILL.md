---
name: frontend-craft
description: Web arayüzü (landing, vitrin, panel ekranı) tasarım ve uygulama disiplini — marka kimliği kararı, referans eşleme, 3 genişlikte screenshot döngüsü, anti-generic + yasak liste, a11y/perf kabul kriterleri. HTML/CSS/Tailwind/Razor'a dokunan her işte önce bunu yükle.
allowed-tools: Bash(node *) Bash(npm *) Bash(npx *)
---

## 0. Mod tespiti (ilk adım, tek satır raporla)
- Tara: `reference/` (png/jpg/pdf) var mı → REF modu; yoksa FREE modu. `brand_assets/` var mı. Proje türü: `*.csproj` + `Views/` veya `Pages/` → aspnet; değilse static.
- REF: hedef birebir eşleme. Bölüm 3 UYGULANMAZ; referans her zaman kazanır. Referansta olmayan bölüm/özellik/metin eklenmez, referans "iyileştirilmez".
- FREE: koddan önce MARKA KİMLİĞİ KARARI tek satır: `ana #hex · nötr #hex · vurgu #hex · display font · sans font · ölçek 8px`. Ana renk mor/indigo/menekşe olamaz. Sonra Bölüm 3 zorunlu.
- `frontend-design` skill'i listede varsa onu da yükle; yoksa Bölüm 3 onun yerine geçer.

## 1. Sunucu
- static: `node ${CLAUDE_SKILL_DIR}/scripts/serve.mjs` arka planda → http://localhost:3000. Çalışıyorsa ikinci instance açma.
- aspnet: `dotnet run --project <web csproj>` arka planda; URL `Properties/launchSettings.json`'dan.
- `file:///` ile screenshot YASAK.

## 2. Screenshot döngüsü
- `node ${CLAUDE_SKILL_DIR}/scripts/screenshot.mjs <url> [etiket]` → `.screens/` altına 390/768/1440 üç PNG. Her PNG'yi Read ile aç ve incele.
- Tur = screenshot → sapma tablosu → düzelt → screenshot. Min 2, max 4 tur.
- Sapma satırı: `genişlik | öğe | ölçülen | beklenen | tolerans dışı?`
- Tolerans: spacing ±2px, font-size ±1px, radius ±1px, hizalama ±2px, renk exact hex, font ailesi/ağırlık exact.
- REF'te beklenen = referans; FREE'de beklenen = Bölüm 0 kararı + Bölüm 3 token'ları.
- Kapanış: 3 genişlikte 0 tolerans-dışı sapma. 4. tur sonunda hâlâ sapma varsa DUR raporu (kalan sapmalar + neden).

## 3. FREE modu guardrail'leri
- Renk: Tailwind default paleti birincil olamaz; tek marka rengi, tint/shade türet, CSS değişkeni.
- Tipografi: başlık ≠ gövde ailesi (display/serif + sans). Büyük başlık `letter-spacing:-0.03em`; gövde `line-height:1.7`; 5 kademeli ölçek; h1 belirgin, h2/h3 kademeli.
- Spacing: tek ölçek 8/16/24/32/48/64/96; ölçek dışı değer yok; bölümler arası boşluk ölçekten.
- Gölge: düz `shadow-md` yok; katmanlı, marka renginden türetilmiş, düşük opaklık.
- Arka plan: ≥2 radial gradient katmanı + SVG noise grain.
- Derinlik: 3 yüzey seviyesi (base → elevated → floating).
- Görsel (logo/ikon hariç): gradient overlay `from-black/60` + `mix-blend-multiply` renk katmanı.
- Animasyon: yalnız `transform`/`opacity`; `transition-all` yasak; easing `cubic-bezier(.22,1,.36,1)`; `prefers-reduced-motion` ile kapanır; gereksiz animasyon/kütüphane yok.
- Interaktif: her `a, button, [role=button], input, select, textarea` için hover + focus-visible + active.
- İkon: tek aile, SVG (Lucide outline); emoji yok.
- Grid: kırık simetri — ≥1 bölüm asimetrik kolon/tam genişlik dışı; her şey ortalı ve eşit genişlik olmaz.
- YASAK: mor/indigo→mavi gradient · emoji ikon · backdrop-filter blur kart yığını · "h1+p+2 buton" hero kalıbı birebir · stok illüstrasyon/3D render hissi · eşit boyutlu kart tekrarı · her bölümde aynı boşluk.

## 4. Her iki modda kabul kriterleri
- `node ${CLAUDE_SKILL_DIR}/scripts/audit.mjs <url> dev` → PASS; FREE modunda SLOP satırı boş.
- a11y: `html[lang]`, tek `h1`, `header/main/footer` landmark, her `img` alt+width+height, fold altı `loading="lazy"`, kontrast gövde ≥4.5:1 / büyük başlık ≥3:1 (hex'ten hesapla, çiftleri raporla).
- perf: font ≤2 aile ≤4 dosya, `font-display: swap`, tüm medya boyutlu (CLS 0).
- Prod teslim (kullanıcı "prod"/"yayın" dediğinde): Tailwind CDN kaldırılır → `npx @tailwindcss/cli -i src/input.css -o wwwroot/css/site.css --minify`; `audit.mjs <url> prod` PASS.

## 5. Çıktı ve içerik
- static: `index.html` + `styles.css`; prototipte Tailwind CDN serbest. aspnet: Razor view/partial + `wwwroot/css/*.css`; inline `<style>` yasak; CSS değişkenleri site.css'te. Mobile-first.
- `brand_assets/` varsa logo/palet oradan, hex uydurma yok. Görsel yoksa `https://placehold.co/WxH`.
- İÇERİK: lorem ipsum yok · uydurma sayı/yorum/müşteri logosu yok · bölüm başına TEK mesaj · başlıklar markaya özel iddia (jenerik "En iyi çözüm" yok). Metin kullanıcıdan gelmediyse sen yaz, raporda `[ÖNERİ]` işaretle; marka adı kullanıcıdan gelir.

## 6. Tur raporu (her tur sonu, kısa)
Tur N | mod | PNG yolları | sapma tablosu | audit (+SLOP) | KANIT (FREE): ana/nötr/vurgu hangi elemanlarda · font çifti nerede · ölçek hangi bölümlerde | sonraki adım / KAPANIŞ / DUR
