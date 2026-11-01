# Dördüncü kişi — güncel entegrasyon

Unity **6000.3.13f1**, Windows x64. Aktif dal: `feat/lead-coastal-cinematic`. Ayrıntılı gün/ürün/haber planı: [ACTIVE_CAMPAIGN.md](Docs/ACTIVE_CAMPAIGN.md). [Kıyı, tesis ve açılış sinematiği](Docs/COASTAL_CINEMATIC.md).

## Çalıştırma

`Assets/Scenes/First.unity` açılır ve Play'e basılır. Yalnız **First → Menu → Game** build'e alınır. First uygulama, ayar ve EventSystem sahibidir; Menu/Game doğrudan başlatılmaz.

Yeni görevin başında 90 saniyelik atlanamayan açılış vardır: siyah fonda söz → carettaların kumdan denize geçip kavisli yüzmesi → ortalanmış COP31 logosunun yükselmesi → 24 renkli büyük havai fişek → yaklaşınca animasyonla açılan çift kapı → gıda güvenliği afişlerinin olduğu koridor → çalışma masası. Giriş boyunca kamera ve oynanış kilitlidir; gazete bitişten sonra açılır. Yeniden başlatma açılışı tekrarlamaz. Arka duvarda çerçeveli Atatürk/Cumhurbaşkanı portreleri, arka köşelerde ayaklı Türkiye/BM bayrakları bulunur.

Fareyle bakış; **E/sol tık** inceleme; sürükleme döndürme; tekerlek yakınlık; **B** kitapçık; **F** analiz; **W** onay; **R** red. Ayrıştırma kutuları oyuncunun arkasındadır. Esc sıradan belgeyi kapatır veya duraklatır. Gazete ve acil faks açık onay gerektirir.

T / Son rapor cihazı, kısayolu ve fiziksel faks sunumu kaldırıldı. Normal F analizi sonucu belge olarak açılır; Bakanlık acil faksı zorunlu belge ve tek seferlik ses bildirimiyle sürer. [Sanatçı kaynak eşleştirmesi](Docs/ARTIST_SYNC.md).

Son main'in gerekli kitapçık, belge düzeni ve servis düzeltmeleri mevcut sahne korunarak aktarıldı. Kitapçık artık günlük gazeteyi, etkin kuralları ve ayrıştırma rehberini içerir. [Alınan değişiklikler ve hariç tutulan T rapor akışı](Docs/MAIN_SYNC.md).

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
| PauseMenu / SettingsPanel / DocumentPresenter | Duraklatma, ayarlar, belge ve acil bildirim sunumu |
| IntroCinematic / OpeningTiming | Kayıtlı Timeline'ın tek saati, aktör/altyazı örnekleme ve kamera/HUD devri |
| ShorelineTurtle | Kum, kavisli suya giriş, dönüşlü ve hafif dalışlı yüzme |
| WorkstationDocuments | Belge olayından fiziksel gazete ve açık/kapalı kitap görünürlüğü |

Ürün prefabları, sağlam/bozuk malzemeler, UI ve bütün referanslar Editor'de kaydedilir. Oyuncuda Instantiate, Renderer.material, otomatik sahne/asset üretimi veya yedek ürün modeli yoktur. Katalog bir kez indekslenir; eksik yapılandırma açık hata verir.

Her sanatçı modeli için Fresh/Spoiled varyantı hazırlanır: **62 prefab**. Mesh'ler aynıdır; shader orijinal dokuyu kullanarak renk kaybı/küf ve ambalaj şişmesi uygular. Görünür bozulma ile gizli kirlilik ayrı verilerdir. SKT'si geçmiş ürün sağlam görünebilir; temiz görünen ürün pozitif analiz verebilir.

Ürün masaya/bant sonuna veya ambalaj kutuya ulaşmadan ilgili işlem tamamlanmaz. Restart eski RunId/callback/raporları geçersiz kılar. Belge kararları kilitler; pause analiz ve hareketi durdurur.

## Editor

GameUP32 menüsü:

- **Render campaign press photographs:** mevcut modellerden üç haber fotoğrafını render eder; grafik aygıtı gerekir.
- **Rebuild Game room and newspaper:** kampanya, varyantlar, ürün prefabları ve yalnız Game sahnesini yeniler.
- **Rebuild coastal facility and opening:** yeni assetlerin URP ayarlarıyla kıyı, tesis ve Timeline açılışını Game sahnesine kaydeder.
- **Refresh signs, portraits and artist props:** mevcut sahnenin yerleşimini korur; Director/Out of Order tabelaları ve çerçeveyi dolduran portreleri günceller, kullanım dışı cihazı temizler.
- **Art / Refresh art frames and Esmanur game kit:** mevcut afiş/portreleri ahşap paket çerçevesine geçirir ve binanın arkasındaki 48 parçalık EditorOnly sergiyi hazırlar.
- **Art / Focus Esmanur game kit:** Game sahnesinde, Scene görünümünü tüm sanatçı modellerine odaklar. [Sergi rehberi](Docs/ARTIST_GAME_KIT.md).
- **Rebuild First, Menu and Game:** üç sahneyi yeniden yazar.
- **Esmanur model inventory:** kaynak ve aktif ürün raporunu yeniler.
- **Build / Windows:** Builds/Windows/AntalyaDenetimMasasi.exe üretir.

Bu komutlar importta veya oyun açılışında çalışmaz. Elle asset/sahne düzenlediysen yeniden üretmeden önce değişikliklerini koru. Fotoğraflar repoda kayıtlıdır; normal açılışta render gerekmez.

```powershell
.\Tools\Lead\Verify.ps1 -UnityEditor 'D:\Programs\UnityInstalls\6000.3.13f1\Editor\Unity.exe' -Visuals
```

## Doğrulama ve kaynaklar

**52/52 test:** 90 saniyelik giriş kilidi, yüzme → ortalanmış logo → renkli havai fişek → kapı açılması → koridor sırası, kapanan efektler, duvar kesmeyen kamera rotası, animasyonlu beş caretta, 16/16 kaynak çevre parçası (14 aktif, 2 kullanım dışı), Director/Out of Order tabelalar, çerçeveyi dolduran portreler, bayraklar, hacimli arazi ve URP/outline bağlantıları; ardından normal F analizi, acil faks, 31/31 karar, 15/15 ayrıştırma, 340 puan. Tümüne onay 16/31, tümüne red 15/31 ile başarısızdır. Açılmamış kategori, balıklı aktif haber, yabancı ürün modeli, eksik varyant, shader hatası, boş/kesilen gazete metni, afiş taşması, acil faks sınırı, pause/restart ve menüye dönüş denetlenir. Bu çalışma için build alınmadı.

[Doğrulama](VALIDATION.md) · [Ürün matrisi](Docs/CAMPAIGN_MATRIX.md) · [Model envanteri](Docs/MODEL_INVENTORY.md) · [Görsel kaynakları](Assets/Art/Menu/ART_DIRECTION.md) · [Gazete dokusu/fotoğraflar](Assets/Art/Documents/ART_DIRECTION.md).

Önceki balıklı veri ve prefablar Assets/Tests/Editor/LegacyContent altında referans arşividir. Aktif sahne bunlara bağlı değildir; eski saf motorun kabul testleri Editor'de korunur. Ayrı PC'de tam manuel oynayış son teslim kontrolüdür.
