# Antalya kıyısı ve gıda güvenliği tesisi

Unity **6000.3.13f1 / URP 17.3**, dal `feat/lead-coastal-cinematic`. Sahne sayısı **First → Menu → Game** olarak kalır. Açılış, Game sahnesinde kayıtlı bir Timeline'dır.

## Oynanış akışı

Menüde “Göreve başla” seçildiğinde 72 saniyelik açılış oynar. İlk 19 saniyede, kullanıcının paylaştığı görseldeki Emine Erdoğan sözü siyah fonda tam metin ve atıfla görünür. Ardından kamera Antalya kıyısında carettaları izler, ahşap yürüyüş yolundan tesisin açık girişine ilerler, koridordaki gıda/su/iklim ve soğuk zincir afişlerine bakar ve denetim masasına ulaşır.

Sinematikte oynanış kamerası ve HUD kapalıdır. Esc, karar verme ve yeniden başlatma girişi atlatmaz. Gün akışı Boot durumunda kalır; henüz ürün, analiz veya gazete başlamaz. Timeline tamamlanınca tek etkin kamera ve AudioListener çalışma masasına geçer, birinci günün zorunlu gazetesi açılır. Oyun içi yeniden başlatma aynı açılışı tekrarlamaz; menüden yeni göreve başlamak tekrar oynatır. Pencere odağı kaybolursa sinematik zamanı ilerlemez.

Beş caretta sahneye önceden yerleştirilmiştir. Sanatçının döngülü animasyonu Animator Controller'a kayıtlıdır; modeller baş-kuyruk ekseninden hareket yönüne hizalanır. Her biri farklı zamanda kıyıdan suya ilerler, ardından kıyıya dönmeyen bir yüzme rotasında kalır. Runtime'da caretta, kamera, UI, prefab veya materyal üretilmez.

## Üç assetin kullanımı

| Asset | Aktif kullanım | URP hazırlığı |
|---|---|---|
| Polygon Office | Tesis duvarları, zemin, koridor, açık kapılar, aydınlatma ve bitkiler | Materyaller URP/Lit'e çevrildi. Duvarlar için aynı atlası kullanan çift yüzlü tesis materyali kayıtlı; kapalı koridor içeriden ve dışarıdan görünür. |
| Stylized Water | İçe aktarılan deniz mesh'i, distortion ve foam dokuları | `GameUP/COP31/StylizedWaterURP`: dalga, kıyı köpüğü, derinlik ve turkuazdan koyu maviye geçiş. Built-in GrabPass veya ilave yansıma kamerası kullanılmaz. |
| Easy Performant Outline | Ürünler, denetim hedefleri, tesis objeleri ve carettalar | Unity 6 Render Graph köprüsü. Native çözünürlük, tek dilation, blur yok; ince siyah çizgi yalnız görünür yüzeylerde. Deniz/zemin/duvarlar outline'a alınmaz. |

EPO'nun Unity 6 köprüsü, assetin kendi maske/dilation/composite uygulamasını kullanır. URP'nin Render Graph'ı kapatılmaz. PC ve Mobile renderer assetlerinde birer EPO feature kayıtlıdır; yalnız Outliner taşıyan Game kameraları çalıştırır. `URP_OUTLINE`, assembly version define ile URP paketine bağlıdır.

Asset demoları ve eski su yansıma kodu yalnız Editor assembly'lerine ayrılmıştır. Eksik bağımlılıkları olan eski Standard Assets görüntü efektleri `GAMEUP_LEGACY_BUILTIN_DEMO` koşuluyla derleme dışında tutulur; dosyalar korunur. Üretim sahnesi bunlara bağlı değildir.

Esmanur'un `feature/Objects` dalındaki `0106ab2` içeriğinden yalnız Art/Environment, Art/Outside ve ilgili prefablar GUID'leriyle alındı. Caretta yanında gerçek faks, analiz cihazı ve kitap modelleri denetim masasında kullanılır. Ürün kampanyası ve üç günlük haber/kural eşleşmeleri korunur.

## Düzenleme ve doğrulama

GameUP32 → Authoring → **Rebuild coastal facility and opening**, Game sahnesini ve kayıtlı açılış assetlerini yeniden hazırlar. Menü ve First sahnesi yeniden yazılmaz. Önce mevcut sahne düzenlemelerini kaydet; bu komut Game sahnesini yeniden oluşturur.

- `CinematicAuthoring`: kamera animasyon eğrileri, Timeline, söz ve açıklama UI'ı.
- `CoastalEnvironmentAuthoring`: Polygon Office mimarisi, sahil ve beş caretta.
- `CoastalAssetSetup`: URP materyalleri, EPO kamera/obje ayarları.
- `IntroCinematic`: yalnız açılışın oynatılması ve kamera/HUD devri; açıklama metni sadece sahne değişince güncellenir.
- `ShorelineTurtle`: önceden kayıtlı aktörlerin kıyı ve yüzme hareketi.

`GameUp.Editor.CoastalReview.Capture` gerçek URP görüntülerini [önizleme klasörüne](Previews/Coastal) kaydeder. [Unity doğrulama raporu](../VALIDATION.md) ve yerel `Logs/coastal-tests.xml`, `Logs/coastal-tests.log`, `Logs/coastal-review.log` test kanıtlarıdır.

Yeni kontroller: beş sanatçı aktörünün animasyon bağlantısı ve baş yönü; yüzme rotasının kıyıdan uzak kalması; kamera yolunun duvar kesmemesi; çalışma kamerasıyla son pozun eşleşmesi; yalnız görünür yüzeylerde ince outline; sözün metin alanına sığması; gerçek su assetinin mesh/dokularının kullanılması. Mevcut üretim testi ayrıca 72 saniyelik girişi doğal süresinde tamamlar, atlama girdilerini reddeder ve sonrasında üç günlük kampanyayı yürütür.

## Görsel ve içerik kaynağı

Gıda–su–iklim ilişkisi, kaynak izleme, soğuk zincir ve atık azaltımı oyunun gıda güvenliği odağını destekler. Tematik referans: [COP31 Gıda Güvenliği](https://cop31.tr/tr/cop31-aksiyon-ajandasi/gida-guvenligi). Açılış alıntısının metni kullanıcının paylaştığı görselden alınmıştır. Logo mevcut resmî COP31 görselidir; özgün renk ve oran korunur. Yeni fotoğraf, dış model veya yapay zekâ görseli eklenmedi.

Bu dal yerel çalışma içindir. Son adımda pull, push, PR veya main merge işlemi yapılmaz.

1 Kasım doğrulaması: **41/41 test geçti**. Tam sinematik ve kampanya Editor PlayMode'da çalıştırıldı; yeni Windows build'i kullanıcı isteğiyle durduruldu.
