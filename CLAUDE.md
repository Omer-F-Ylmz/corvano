# Corvano — erkek giyim e-ticaret (.NET 10 MVC)

## Mimari (Cafixo katmanları)
- `Corvano.Web` (MVC, Autofac, `/health`) → `Corvano.Business` (`I{X}Service` / `{X}Manager`, imza `Task<(HttpStatusCode, IResult)>`) → `Corvano.DataAccess` (`I{X}Dal` / `Ef{X}Dal : EfEntityRepositoryBase`, `EfUnitOfWork`, migrations) → `Corvano.Entities` (flat, nav prop yok, snake_case kolon) → `Corvano.Core` (Result tipleri, `IEntityRepository`, `IUnitOfWork`).
- DI: `Business/DependencyResolvers/Autofac/AutofacBusinessModule` içinde manuel `RegisterType`; assembly scan yok.
- DB: EF Core + SQL Server; dev LocalDB, bağlantı `appsettings.Development.json` (gitignore) ← `appsettings.Development.example.json`.
- CSS: Tailwind CLI `src/input.css` → `Corvano.Web/wwwroot/css/site.css` (`npm run css:build`); `_Layout.cshtml` tek kaynak; inline `<style>` yasak.
- Testler: `tests/Corvano.Tests` (xUnit), gerçek SQL Server'a karşı; CI env `ConnectionStrings__Default` ile.

## Süreç
1. Kırmızı-önce: önce başarısız test, sonra kod.
2. `dotnet build Corvano.sln -warnaserror` → 0 hata 0 uyarı.
3. `dotnet test Corvano.sln` → yeşil. Simülasyon/uydurma çıktı yok; komut gerçekten çalışır.
4. Migration: `dotnet ef migrations add <Ad> -p Corvano.DataAccess -s Corvano.Web` → `dotnet ef database update -p Corvano.DataAccess -s Corvano.Web`.
5. Commit → push → CI (`.github/workflows/ci.yml`) yeşil olmadan iş kapanmaz.
6. CLAUDE.md ≤20 KB; prosedürler skill'de (`.claude/skills/`), burada değil.
7. Her dalga `/clear` ile yeni oturumda başlar.
8. Kapsam disiplini: istenen kadar; iş kuralı eklenmeyecekse eklenmez.

## UI kuralı
UI'a (Razor/CSS/Tailwind) dokunan her işte `/frontend-craft` zorunlu; Bölüm 6 tur raporu olmadan kapanış yok.
