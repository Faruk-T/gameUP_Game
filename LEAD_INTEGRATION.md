# Dördüncü kişi — güncel entegrasyon

Unity **6000.3.13f1**, Windows x64. Aktif dal: `feat/lead-coastal-cinematic`. Ayrıntılı gün/ürün/haber planı: [ACTIVE_CAMPAIGN.md](Docs/ACTIVE_CAMPAIGN.md). [Kıyı, tesis ve açılış sinematiği](Docs/COASTAL_CINEMATIC.md).

## Çalıştırma

`Assets/Scenes/First.unity` açılır ve Play'e basılır. Yalnız **First → Menu → Game** build'e alınır. First uygulama, ayar ve EventSystem sahibidir; Menu/Game doğrudan başlatılmaz.

Yeni görevin başında 72 saniyelik atlanamayan açılış vardır: siyah fonda söz → animasyonlu carettalar ve Antalya kıyısı → tesis girişi → gıda güvenliği afişlerinin olduğu koridor → çalışma masası. Giriş boyunca kamera ve oynanış kilitlidir; gazete bitişten sonra açılır. Yeniden başlatma açılışı tekrarlamaz.

Fareyle bakış; **E/sol tık** inceleme; sürükleme döndürme; tekerlek yakınlık; **B** kitapçık; **F** analiz; **T** son rapor; **W** onay; **R** red. Ayrıştırma kutuları oyuncunun arkasındadır. Esc sıradan belgeyi kapatır veya duraklatır. Gazete ve acil faks açık onay gerektirir.

## Kampanya

| Gün | Yeni açılan grup | Ürün sayısı | Haber |
|---|---|---:|---|
| 1 / 1 Kasım | Et ve süt | 8 | Küresel ısınma ve gıda tedariki |
| 2 / 2 Kasım | Meyve | 10 | Çevresel kalıntı, atık ve kıyı koruma |
| 3 / 3 Kasım | Yumurta | 13 | Soğuk zincir ve kaynak geri çağırması |

Esmanur'un **31 mevcut modeli**, 31 denetim ürünü olarak kullanılır. Ürün dolapları/teşhir nesneleri yoktur. Aktif veriler Assets/Data/Campaign altındadır. Haber metinlerinde balık yoktur; ilk gün mevsim dışı Antalya sıcaklığı iddiası yerine yıl boyunca süren küresel iklim riski anlatılır. Caretta carettalar atık/kıyı koruma bağlamında haberlerde kalır.

Üçüncü gün ilk üç ürün, ayrıştırmaları da tamamlanınca faks gelir. Onaydan sonra B bölgesi et/süt/yumurtaları temiz analizle bile reddedilir. Meyveler kapsam dışıdır. Önceki kararlar değişmez.

## Kod sınırları

| Bileşen | Sorumluluk |
|---|---|
| ApplicationRoot | Sahne, ayarlar, imleç odağı |
| CampaignDefinition / DayDefinition | Sıralı günler, kategori açılımları, kayıtlı ürünler ve belgeler |
| DayFlowSession | Ürün sırası, işlem sınırları, immutable kural snapshot'ı |
| DocumentQueue | Tek belge, kuyruk, onay token'ları |
| GameSessionController | Akış ve sahne portları |
| ProductionInspectionBackend | Asset kataloğu, analiz, Faruk'un ProductRuleEvaluator'ı, puan |
| ProductPresentation | Kayıtlı 31 ürünlük havuz ve fiziksel hareket callback'leri |
| WorkstationInteractionController | Tek oynanış girdi sahibi |
| NewspaperView / DocumentPresenter / ResultsPresenter | Kayıtlı UI sunumu |
| FoodCondition shader | Aynı kaynak mesh üzerinde bozulma deseni ve şişme |
| PauseMenu / SettingsPanel / FaxPresenter | Duraklatma, ayarlar, faks sunumu |

Ürün prefabları, sağlam/bozuk malzemeler, UI ve bütün referanslar Editor'de kaydedilir. Oyuncuda Instantiate, Renderer.material, otomatik sahne/asset üretimi veya yedek ürün modeli yoktur. Katalog bir kez indekslenir; eksik yapılandırma açık hata verir.

Her sanatçı modeli için Fresh/Spoiled varyantı hazırlanır: **62 prefab**. Mesh'ler aynıdır; shader orijinal dokuyu kullanarak renk kaybı/küf ve ambalaj şişmesi uygular. Görünür bozulma ile gizli kirlilik ayrı verilerdir. SKT'si geçmiş ürün sağlam görünebilir; temiz görünen ürün pozitif analiz verebilir.

Ürün masaya/bant sonuna veya ambalaj kutuya ulaşmadan ilgili işlem tamamlanmaz. Restart eski RunId/callback/raporları geçersiz kılar. Belge kararları kilitler; pause analiz ve hareketi durdurur.

## Editor

GameUP32 menüsü:

- **Render campaign press photographs:** mevcut modellerden üç haber fotoğrafını render eder; grafik aygıtı gerekir.
- **Rebuild Game room and newspaper:** kampanya, varyantlar, ürün prefabları ve yalnız Game sahnesini yeniler.
- **Rebuild coastal facility and opening:** yeni assetlerin URP ayarlarıyla kıyı, tesis ve Timeline açılışını Game sahnesine kaydeder.
- **Rebuild First, Menu and Game:** üç sahneyi yeniden yazar.
- **Esmanur model inventory:** kaynak ve aktif ürün raporunu yeniler.
- **Build / Windows:** Builds/Windows/AntalyaDenetimMasasi.exe üretir.

Bu komutlar importta veya oyun açılışında çalışmaz. Elle asset/sahne düzenlediysen yeniden üretmeden önce değişikliklerini koru. Fotoğraflar repoda kayıtlıdır; normal açılışta render gerekmez.

```powershell
.\Tools\Lead\Verify.ps1 -UnityEditor 'D:\Programs\UnityInstalls\6000.3.13f1\Editor\Unity.exe' -Visuals -Build
```

## Doğrulama ve kaynaklar

**41/41 test:** 72 saniyelik giriş kilidi, duvar kesmeyen kamera rotası, animasyonlu beş caretta, URP asset ve outline bağlantıları; ardından 31/31 karar, 15/15 ayrıştırma, 340 puan. Tümüne onay 16/31, tümüne red 15/31 ile başarısızdır. Açılmamış kategori, balıklı aktif haber, yabancı ürün modeli, eksik varyant, shader hatası, boş/kesilen gazete metni, afiş taşması, acil faks sınırı, pause/restart ve menüye dönüş denetlenir.

[Doğrulama](VALIDATION.md) · [Ürün matrisi](Docs/CAMPAIGN_MATRIX.md) · [Model envanteri](Docs/MODEL_INVENTORY.md) · [Görsel kaynakları](Assets/Art/Menu/ART_DIRECTION.md) · [Gazete dokusu/fotoğraflar](Assets/Art/Documents/ART_DIRECTION.md).

Önceki balıklı veri ve prefablar Assets/Tests/Editor/LegacyContent altında referans arşividir. Aktif sahne bunlara bağlı değildir; eski saf motorun kabul testleri Editor'de korunur. Ayrı PC'de tam manuel oynayış son teslim kontrolüdür.
