# Antalya COP31 görsel paketi

Ana palet: koyu Akdeniz yeşili, sıcak keten, turkuaz, sınırlı terrakota. Menüde sol alan okunabilirlik için sakin; odağın sağında Antalya limanı, Toroslar ve denetim masası bulunur.

| Dosya | Kullanım | Kaynak |
|---|---|---|
| AntalyaHarbour.png | Menü arka planı | Built-in image generation; yeni illüstrasyon |
| AntalyaHeat.png | Birinci gün gazete haberi | Built-in image generation; yeni illüstrasyon |
| CarettaCoast.png | İkinci gün gazetesi ve sahne kıyı panosu | Built-in image generation; yeni illüstrasyon |
| COP31Horizontal.png | Menüde resmî kimlik | COP31 kurumsal kimlik indirmesi; özgün renk ve oran korunur |
| SharedButtonLight.png | Menü düğmeleri | Kullanıcının Feathered Fortune / PigeonSimulator5 proje arşivi |

Üretim modu: yerleşik `image_gen` aracı. Tam ve değişiklik yapılmamış üç üretim promptu [generation-prompts.json](generation-prompts.json) dosyasında saklanır. Çıktılar elle yeniden çizilmedi veya stok fotoğraf gibi sunulmadı. Yapay zekâ illüstrasyonları resmî COP31 logosu içermez. Logo ayrı bir Image bileşenidir; yeniden üretilmez, renklendirilmez veya esnetilmez.

Kurale fontu PigeonSimulator5 arşivinden alınmıştır; lisans `Assets/Fonts/Kurale-OFL.txt` içinde korunur. Küçük metinler Unity'nin Türkçe karakterleri destekleyen LegacyRuntime fontunu kullanır.

## İçerik referansları

- [COP31 Türkiye](https://cop31.tr/tr): 9–20 Kasım 2026, Antalya.
- [Kurumsal kimlik](https://cop31.tr/tr/kurumsal-kimlik).
- [Özgün yatay logo PNG](https://cop31.tr/assets/downloads/brand/cop31-turkiye-yatay-logo.png).
- [Okyanuslar ve denizler](https://cop31.tr/tr/cop31-aksiyon-ajandasi/okyanuslar-ve-denizler).
- [Gıda güvenliği](https://cop31.tr/tr/cop31-aksiyon-ajandasi/gida-guvenligi).
- [Emine Erdoğan](https://cop31.tr/tr/lider/emine-erdogan): ekip sayfasında kullanılan kısa alıntının kaynağı.

Oyundaki 1–2 Kasım tarihleri konferans öncesi hazırlık günleridir. Haberler, sızıntı olayı ve ürün partileri kurmacadır. Gerçek Antalya üreticileri hakkında gerçek bir kirlilik iddiası kurulmaz. Deniz kirliliği, caretta carettaların yaşam alanları ve atık ayrıştırma aynı oyun döngüsünde anlatılır.

## Unity import

Menü arka planı: en çok 2048; belge ve logo: en çok 1024. Mipmap kapalı, Read/Write kapalı, platform sıkıştırması açık. Menü sahnesi boşaltılınca büyük arka planın sahne referansı bırakılır; Game sahnesinde menü rotatorü veya arka plan kopyası bulunmaz.
