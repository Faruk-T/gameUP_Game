# Antalya kıyısı ve gıda güvenliği tesisi

Unity **6000.3.13f1 / URP 17.3**, dal `feat/lead-coastal-cinematic`. Sahne sayısı **First → Menu → Game** olarak kalır. Açılış, Game sahnesinde kayıtlı bir Timeline'dır.

## Oynanış akışı

Menüde “Göreve başla” seçildiğinde 90 saniyelik açılış oynar. İlk 19 saniyede, kullanıcının paylaştığı görseldeki Emine Erdoğan sözü siyah fonda tam metin ve atıfla görünür. Ardından kamera Antalya kıyısında carettaları izler. Yüzme başladıktan sonra resmî COP31 logosu denizin üzerinde yükselir; logo yükselince beş havai fişek sırayla patlar. Kamera bu bölüm tamamlandıktan sonra tesisin açık girişine ilerler, koridordaki gıda/su/iklim ve soğuk zincir afişlerine bakar ve denetim masasına ulaşır.

| Zaman | Görüntü / davranış |
|---|---|
| 0–19 sn | Siyah fonda söz ve atıf |
| 20–33 sn | Kum üzerinde ilerleme ve kavisli suya giriş |
| 33–39 sn | Beş caretta denizde; dönüş, yüzgeç hareketi ve hafif dalış görünür |
| 39–44 sn | COP31 logosu yukarı yükselir |
| 44–50.4 sn | Beş zamanlanmış havai fişek; logo 50–52 arasında söner |
| 52–71 sn | Yürüyüş yolu ve tesis girişi |
| 71–88 sn | Koridor, yön tabelası ve iki gıda güvenliği afişi |
| 88–90 sn | Masaya varış, kararma ve oynanışa devir |

`OpeningTiming` bölüm sınırlarını tutar. Kamera, logo ve efektler aynı Timeline'a bağlıdır. `IntroCinematic`, caretta pozlarını ve altyazıları da bu saatten örnekler; ayrı sayaçların zamanla kayması önlenir. Altyazılar yalnız ilgili bölümde, koyu bir bant üzerinde görünür. Havai fişekler bölüm sonunda pasifleşir; koridorda donmuş parçacık kalmaz.

Sinematikte oynanış kamerası ve HUD kapalıdır. Esc, karar verme ve yeniden başlatma girişi atlatmaz. Gün akışı Boot durumunda kalır; henüz ürün, analiz veya gazete başlamaz. Timeline tamamlanınca tek etkin kamera ve AudioListener çalışma masasına geçer, birinci günün zorunlu gazetesi açılır. Oyun içi yeniden başlatma aynı açılışı tekrarlamaz; menüden yeni göreve başlamak tekrar oynatır. Pencere odağı kaybolursa sinematik zamanı ilerlemez.

Beş caretta sahneye önceden yerleştirilmiştir. Sanatçının döngülü animasyonu Animator Controller'a kayıtlıdır; modeller baş-kuyruk ekseninden hareket yönüne hizalanır. Kumda yavaş ilerler, Bézier eğrisiyle suya girer, farklı faz ve yönlerde eliptik rotalarda yüzerler. Hareket yönüne bakış, hafif yatış, su üzerindeki iz ve dalış/yükselme dümdüz kayma hissini azaltır. Rotalar kıyıya dönmez. Yakın suda geçirgenlik artırılarak su altındaki gövde görünür bırakılır. Runtime'da caretta, kamera, UI, prefab veya materyal üretilmez.

## Üç assetin kullanımı

| Asset | Aktif kullanım | URP hazırlığı |
|---|---|---|
| Polygon Office | Tesis duvarları, zemin/tavan, koridor, açık kapılar, aydınlatma, bitkiler, ağaç/kayalar, tabela ve afiş propları | Materyaller URP/Lit'e çevrildi. Duvarlar için aynı atlası kullanan çift yüzlü tesis materyali kayıtlı; kapalı koridor içeriden ve dışarıdan görünür. |
| Stylized Water | İçe aktarılan deniz mesh'i, distortion ve foam dokuları | `GameUP/COP31/StylizedWaterURP`: dalga, kıyı köpüğü, derinlik ve turkuazdan koyu maviye geçiş. Built-in GrabPass veya ilave yansıma kamerası kullanılmaz. |
| Easy Performant Outline | Ürünler, denetim hedefleri, tesis objeleri ve carettalar | Unity 6 Render Graph köprüsü. Native çözünürlük, tek dilation, blur yok; ince siyah çizgi yalnız görünür yüzeylerde. Deniz/zemin/duvarlar outline'a alınmaz. |

EPO'nun Unity 6 köprüsü, assetin kendi maske/dilation/composite uygulamasını kullanır. URP'nin Render Graph'ı kapatılmaz. PC ve Mobile renderer assetlerinde birer EPO feature kayıtlıdır; yalnız Outliner taşıyan Game kameraları çalıştırır. `URP_OUTLINE`, assembly version define ile URP paketine bağlıdır.

Asset demoları ve eski su yansıma kodu yalnız Editor assembly'lerine ayrılmıştır. Eksik bağımlılıkları olan eski Standard Assets görüntü efektleri `GAMEUP_LEGACY_BUILTIN_DEMO` koşuluyla derleme dışında tutulur; dosyalar korunur. Üretim sahnesi bunlara bağlı değildir.

Esmanur'un `feature/Objects` dalındaki `0106ab2` içeriğinden yalnız Art/Environment, Art/Outside ve ilgili prefablar GUID'leriyle alındı. **16/16 çevre mesh'i**, **31/31 ürün modeli** ve animasyonlu caretta aktif sahne/kampanyada kullanılır. [Çevre envanteri](ENVIRONMENT_INVENTORY.md), sahnedeki gerçek mesh referanslarından otomatik yazılır. Ürün kampanyası ve üç günlük haber/kural eşleşmeleri korunur.

## Prop ve çevre yerleşimi

Mevcut oda, arka geri dönüşüm alanı, oyuncu masası ve etkileşim konumları korunur. Kullanıcının faks, analiz cihazı, kapalı kitap ve bank üzerinde yaptığı yerel düzeltmeler `SceneArtCorrections.json` içinde kayıtlıdır; yeniden yazımda uygulanır.

- Tesis adı, COP31, ürün kontrol yönü ve masa kontrol yazıları gerçek `SM_Prop_Sign_Exit_01` geometrisine bağlanır. Yaşam alanı uyarısı `SM_Prop_Sign_Wetfloor_01` ayaklı propunu kullanır.
- Beş afişin arkalığı `SM_Prop_Poster_01` olur; içerikler yerel Canvas üzerinde kalır. Koridor afişleri kamera kadrajına sığar.
- Masa, bant ve dört atık kutusu Esmanur'un mesh'lerini kullanır. Faks/analiz cihazının kâğıt ve sinyal parçaları kendi prefab çocuklarıdır; kopya parça eklenmez.
- Katlı gazete ve açık/kapalı kitap fiziksel objelerdir. Kitapçık açılınca fiziksel kitap da açılır; gazete okunurken masadaki katlı kopya gizlenir. `WorkstationDocuments` yalnız belge değişimi olayında görünürlüğü günceller.
- Basit ambalaj küpü gerçek `SM_Prop_CardboardBox_01`, karar düğmeleri `SM_Prop_Elevator_Button_01`, sahil kayaları ve ağaçları Polygon Office proplarıyla değiştirilir.

İçe aktarılan paketlerde ayrı bir dağ modeli bulunmadı. Fotoğraf/tek düzlem arka plan kaldırıldı; çevreyi 360° saran, zirve ve vadileri olan **7.453 köşeli kayıtlı 3B arazi mesh'i** kullanılır. Arazi rengi eğim/yükseklikten gelir; düşük bitki tonları ve kireç taşı sırtları URP ışığı ve sisle görünür. Arazi yalnız Editor'de üretilir, runtime'da değişmez. Deniz ve kıyı tabanı ufukta boşluk bırakmayacak şekilde genişletildi.

## Düzenleme ve doğrulama

GameUP32 → Authoring → **Rebuild coastal facility and opening**, Game sahnesini ve kayıtlı açılış assetlerini yeniden hazırlar. Menü ve First sahnesi yeniden yazılmaz. Önce mevcut sahne düzenlemelerini kaydet; bu komut Game sahnesini yeniden oluşturur.

- `CinematicAuthoring`: kamera animasyon eğrileri, Timeline, söz ve açıklama UI'ı.
- `OpeningEffectsAuthoring`: kayıtlı logo yükselişi, Timeline ControlTrack havai fişekleri ve caretta su izleri.
- `CoastalEnvironmentAuthoring`: Polygon Office mimarisi, sahil ve beş caretta.
- `FacilityPropAuthoring`: gerçek paket/sanatçı propları, dağ mesh'i ve çevre envanteri.
- `SceneArtCorrections`: kaydedilmiş kullanıcı yerleşimlerini koruma.
- `CoastalAssetSetup`: URP materyalleri, EPO kamera/obje ayarları.
- `IntroCinematic`: yalnız açılışın oynatılması ve kamera/HUD devri; açıklama metni sadece sahne değişince güncellenir.
- `ShorelineTurtle`: önceden kayıtlı aktörlerin kıyı ve yüzme hareketi.

`GameUp.Editor.CoastalReview.Capture` gerçek URP görüntülerini [önizleme klasörüne](Previews/Coastal) kaydeder. [Unity doğrulama raporu](../VALIDATION.md) ve yerel `Logs/props-tests.xml`, `Logs/props-tests.log`, `Logs/props-final-art.log`, `Logs/props-final-art-tests.xml` test kanıtlarıdır.

Kontroller: sanatçı aktörlerinin animasyonu/baş yönü; yüzmenin yön ve derinlik değiştirmesi; suya giriş → logo yükselişi → gerçek parçacıklar → koridor sırası; efektlerin kapanması; 16 sanatçı parçasının kullanılması; gerçek tabela/afiş geometrisi ve yazı taşması; geçerli hacimli dağ mesh'i; kamera yolunun duvar kesmemesi ve son pozun eşleşmesi; outline, söz yerleşimi ve su kaynakları. Üretim testi 90 saniyelik girişi doğal süresinde tamamlar, atlama girdilerini reddeder, fiziksel kitabı açıp kapatır ve ardından üç günlük kampanyayı yürütür.

## Görsel ve içerik kaynağı

Gıda–su–iklim ilişkisi, kaynak izleme, soğuk zincir ve atık azaltımı oyunun gıda güvenliği odağını destekler. Tematik referans: [COP31 Gıda Güvenliği](https://cop31.tr/tr/cop31-aksiyon-ajandasi/gida-guvenligi). Açılış alıntısının metni kullanıcının paylaştığı görselden alınmıştır. Logo mevcut resmî COP31 görselidir; özgün renk ve oran korunur. Yeni fotoğraf, dış model veya yapay zekâ görseli eklenmedi.

Bu dal yerel çalışma içindir. Son adımda pull, push, PR veya main merge işlemi yapılmaz.

1 Kasım doğrulaması: **44/44 test geçti**. Tam sinematik ve kampanya Editor PlayMode'da çalıştırıldı. Son arazi düzenlemesinin ardından **7/7 çevre/sinematik testi** ayrıca geçti. Kullanıcının isteğiyle build alınmadı.
