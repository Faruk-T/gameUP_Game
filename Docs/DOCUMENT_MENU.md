# Evrak temalı ana menü

**Güncel durum:** Arka plan gerçek tesis kamerasıdır; UI fontu Patrick Hand'dir. V2 kâğıtlar metinsizdir, rehber ve ekip metinleri Unity'de yazılır. Bu dosyanın aşağıdaki ilk sprite paketi tarihsel ilk tasarımı açıklar; güncel uygulama [FACILITY_POLISH.md](FACILITY_POLISH.md) içindedir.

Menü, kullanıcının gönderdiği `Gemini_Generated_Image_s0la2ps0la2ps0la.jpg` referansındaki manila dosya, metal klipsli pano ve damgalı belge tasarımına göre yenilendi. Antalya limanı arka planı ve özgün COP31 logosu korunur.

## Teslim edilen görseller

`Assets/Art/Menu/Documents` altında dört ayrı, 1024 × 1536 RGBA PNG bulunur:

| Dosya | İçerik |
|---|---|
| `ClipboardMainMenu.png` | Metal klips, ahşap pano ve boş resmî form. Yalnız T.C. ÇEVRE VE GIDA DENETİMİ / COP31 ANTALYA başlığı; İPTAL damgası ve menü seçenekleri yok. |
| `SettingsPaper.png` | AYARLAR, dört ayar etiketi, siyah simgeler ve boş tam ekran kutusu. Slider, işaret veya alt düğme yok. |
| `InspectorGuide.png` | Denetçi rehberi ve yazıları; alt düğme boş. Kullanıcının onayıyla kaldırılmış T raporu yönergesi bulunmaz. |
| `CreditsDocument.png` | ORTAK GELECEK İÇİN, damgalar ve ekip metni. Ekip satırında Esmanur: 3D Artist yazar; iki alt düğme boş. |

Üretim, yerleşik `image_gen` aracıyla yapıldı. Değişiklik yapılmamış tam promptlar `Assets/Art/Menu/Documents/generation-prompts.json` dosyasında saklanır. Kaynak referans kullanıcı tarafından sağlandı. PNG'lerin alfa kanalı korunur; çevre arka planı şeffaftır. Referansta olmayan ayrı ana menü görseli, rehber panosunun aynı malzeme ve klips tasarımından türetilir.

## Unity uygulaması

- `DocumentMenuAuthoring`, yalnız Editor'da menü sahnesini hazırlar. Runtime UI, texture, malzeme veya prefab üretimi eklenmez; mevcut MenuController, SettingsPanel ve AudioManager kullanılır.
- Başlat, ayarlar, rehber, ekip, çıkış ve geri dönüş işlemleri gerçek Unity Button bileşenleridir. İşlev metinleri ve ayar değerleri Unity'de çizilir; arka plan görsellerine düğme yazıları basılmaz.
- Ses, hassasiyet ve FOV gerçek Slider; tam ekran gerçek Toggle kullanır. Koordinatlar kaynak görselin 1024 × 1536 ölçüsünden hesaplanarak sahneye kaydedilir. Ayarlar, mevcut UserSettings kaydetme ve uygulama akışını sürdürür.
- Sarı düğme grafiği, rehber PNG'sindeki boş düğme alanını kullanan kalıcı `DocumentButton.asset` Sprite'ıdır; ayrı bir texture kopyası üretilmez. Kenarlar 9-slice ile korunur.
- Açık yön tuşu gezinme bağlantıları, seçim rengi, Enter/Submit, mevcut Esc davranışı ve Özgür'ün düğme sesi korunur. Menü başlangıcında Göreve Başla seçilir; alt sayfalarda ilgili geri düğmesi veya ilk ayar seçilir.
- CanvasScaler/Expand, 4:3 ve geniş ekranlarda ana pano ile sağdaki başlığın tamamını görünür tutar; arka plan EnvelopeParent ile ekranı kaplar.
- Dört PNG, Windows için BC7, mipmap kapalı, Read/Write kapalı, Full Rect ve Clamp ile içeri alınır. Kaynak ölçü korunur. Görseller oyun sahnesine veya kalıcı ApplicationRoot'a bağlanmaz.
- İlk ve Game sahneleri bu işlemle yeniden kurulmaz. Paylaşılan Settings authoring metodu aynı belge tasarımına yönlenir; mevcut oyun sahnesindeki pause ayarları bu menü yenilemesinde değiştirilmez.

Komut: `GameUP32/Authoring/Refresh document main menu`.

## Doğrulama

`Docs/Previews/Menu` altında dört sayfa ve ana menünün 4:3 / 21:9 kontrolleri bulunur. Gerçek kontrollerin tıklanması, hassasiyet slider'ının ayarı değiştirmesi, belge sayfalarından geri dönüş ve First → Menu → Game akışı mevcut ProductionTests entegrasyon testiyle doğrulanır. Ham yerel sonuçlar `Logs/DocumentMenu` içindedir. Windows player build alınmaz.

Son tam Unity koşusu: **59/59 geçti**, başarısız/atlanan yok, 142.76 saniye. Kaynak rapor `Logs/DocumentMenu/tests.xml`; başarılı Editor authoring kaydı `author-final.log` dosyasıdır. Ana menü, ayarlar, rehber ve ekip sayfaları D3D11/URP çıktılarında görsel olarak kontrol edildi. Başlığın ikinci satırı ve üç slider tutamağının ölçüleri bu kontrolün ardından düzeltildi.
