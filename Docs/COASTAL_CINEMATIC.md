# Antalya kıyısı ve gıda güvenliği tesisi

Unity **6000.3.13f1 / URP 17.3**, dal `feat/lead-coastal-cinematic`. Sahne sayısı **First → Menu → Game** olarak kalır. Açılış, Game sahnesinde kayıtlı bir Timeline'dır.

## Oynanış akışı

Menüde “Göreve başla” seçildiğinde 90 saniyelik açılış oynar. İlk 19 saniyede, kullanıcının paylaştığı görseldeki Emine Erdoğan sözü siyah fonda tam metin ve atıfla görünür. Ardından kamera Antalya kıyısında carettaları izler. Yüzme başladıktan sonra resmî COP31 logosu ekranın yatay merkezinde yükselir; kamera dağlara dönerken logo yana kaymaz ve perspektifle eğilmez. Ardından sekiz renkte 24 büyük havai fişek logonun iki yanına yayılır. Kamera bu bölüm tamamlandıktan sonra tesisin kapalı girişine yaklaşır. Çift kanatlı kapılar menteşelerinden içeri açılır, kamera koridordaki afişlere bakıp denetim masasına ulaşır.

| Zaman | Görüntü / davranış |
|---|---|
| 0–19 sn | Siyah fonda söz ve atıf |
| 20–33 sn | Kum üzerinde ilerleme ve kavisli suya giriş |
| 33–39 sn | Beş caretta denizde; dönüş, yüzgeç hareketi ve hafif dalış görünür |
| 39–44 sn | COP31 logosu yukarı yükselir |
| 44–51.84 sn | 24 zamanlanmış, büyük ve renkli havai fişek; logo 50–52 arasında söner |
| 52–68.5 sn | Yürüyüş yolu ve kapalı tesis girişine yaklaşma |
| 68.5–70.5 sn | İki kapı kanadının yumuşakça içeri açılması |
| 72.8–88 sn | Açık eşikten koridora geçiş, yön tabelası ve iki afiş |
| 88–90 sn | Masaya varış, kararma ve oynanışa devir |

`OpeningTiming` bölüm sınırlarını tutar. Kamera, logo ve efektler aynı Timeline'a bağlıdır. `IntroCinematic`, caretta pozlarını ve altyazıları da bu saatten örnekler; ayrı sayaçların zamanla kayması önlenir. Altyazılar yalnız ilgili bölümde, koyu bir bant üzerinde görünür. Havai fişekler bölüm sonunda pasifleşir; koridorda donmuş parçacık kalmaz.

Logo sinematik Canvas'ına merkez çapalıdır; özgün PNG'nin simetrik şeffaf kenarları ve en/boy oranı korunur. Havai fişekler sahnede önceden kayıtlı 24 ParticleSystem'dir: patlama başına 150 kıvılcım, daha büyük çap, doygun renkli çekirdek ve ışıklı iz. Karşılıklı patlama sırası kadrajı dengeler. Yeni kamera takibi veya runtime efekt/obje üretimi yoktur.

Giriş, paketin gerçek büyük kapı çerçevesine oturur. İki yaprak gerçek menteşe noktasından döner; kanatlar ve collider'ları statik batch'e alınmaz. Kamera kapılar açılana kadar eşik dışında kalır. Açılış sonunda manuel Timeline son karede duraklatılır; kapılar açık kalır ve Timeline yeniden değerlendirilmez.

Sinematikte oynanış kamerası ve HUD kapalıdır. Esc, karar verme ve yeniden başlatma girişi atlatmaz. Gün akışı Boot durumunda kalır; henüz ürün, analiz veya gazete başlamaz. Timeline tamamlanınca tek etkin kamera ve AudioListener çalışma masasına geçer, birinci günün zorunlu gazetesi açılır. Oyun içi yeniden başlatma aynı açılışı tekrarlamaz; menüden yeni göreve başlamak tekrar oynatır. Pencere odağı kaybolursa sinematik zamanı ilerlemez.

Beş caretta sahneye önceden yerleştirilmiştir. Sanatçının döngülü animasyonu Animator Controller'a kayıtlıdır; modeller baş-kuyruk ekseninden hareket yönüne hizalanır. Kumda yavaş ilerler, Bézier eğrisiyle suya girer, farklı faz ve yönlerde eliptik rotalarda yüzerler. Hareket yönüne bakış, hafif yatış, su üzerindeki iz ve dalış/yükselme dümdüz kayma hissini azaltır. Rotalar kıyıya dönmez. Yakın suda geçirgenlik artırılarak su altındaki gövde görünür bırakılır. Runtime'da caretta, kamera, UI, prefab veya materyal üretilmez.

## Üç assetin kullanımı

| Asset | Aktif kullanım | URP hazırlığı |
|---|---|---|
| Polygon Office | Tesis duvarları, zemin/tavan, koridor, animasyonlu çift kapı, aydınlatma, bitkiler, ağaç/kayalar, tabela, afiş/portre çerçeveleri ve bayrak direkleri | Materyaller URP/Lit'e çevrildi. Duvarlar için aynı atlası kullanan çift yüzlü tesis materyali kayıtlı; kapalı koridor içeriden ve dışarıdan görünür. |
| Stylized Water | İçe aktarılan deniz mesh'i, distortion ve foam dokuları | `GameUP/COP31/StylizedWaterURP`: dalga, kıyı köpüğü, derinlik ve turkuazdan koyu maviye geçiş. Built-in GrabPass veya ilave yansıma kamerası kullanılmaz. |
| Easy Performant Outline | Ürünler, denetim hedefleri, tesis objeleri ve carettalar | Unity 6 Render Graph köprüsü. Native çözünürlük, tek dilation, blur yok; ince siyah çizgi yalnız görünür yüzeylerde. Deniz/zemin/duvarlar outline'a alınmaz. |

EPO'nun Unity 6 köprüsü, assetin kendi maske/dilation/composite uygulamasını kullanır. URP'nin Render Graph'ı kapatılmaz. PC ve Mobile renderer assetlerinde birer EPO feature kayıtlıdır; yalnız Outliner taşıyan Game kameraları çalıştırır. `URP_OUTLINE`, assembly version define ile URP paketine bağlıdır.

Asset demoları ve eski su yansıma kodu yalnız Editor assembly'lerine ayrılmıştır. Eksik bağımlılıkları olan eski Standard Assets görüntü efektleri `GAMEUP_LEGACY_BUILTIN_DEMO` koşuluyla derleme dışında tutulur; dosyalar korunur. Üretim sahnesi bunlara bağlı değildir.

Sanatçının `feature/Objects` dalındaki `0106ab2` içeriğinden Art/Environment, Art/Outside ve ilgili prefablar GUID'leriyle alındı. **16/16 çevre mesh'i projededir; 14'ü aktif, iki faks parçası T cihazıyla birlikte kullanım dışıdır.** **31/31 ürün modeli** ve animasyonlu caretta aktif kampanya/sahnede kullanılır. [Kaynak eşleştirmesi](ARTIST_SYNC.md) ve [çevre envanteri](ENVIRONMENT_INVENTORY.md) gerçek mesh referanslarıyla denetlenir. Üç günlük haber/kural eşleşmeleri korunur.

## Prop ve çevre yerleşimi

Mevcut oda, arka geri dönüşüm alanı, oyuncu masası ve etkileşim konumları korunur. Kullanıcının analiz cihazı, kapalı kitap ve bank üzerinde yaptığı yerel düzeltmeler `SceneArtCorrections.json` içinde kayıtlıdır. **Refresh signs, portraits and artist props** komutu mevcut sahneye hedefli düzenleme yapar; gizlenen/silinen dekorları yeniden oluşturmaz.

- Dokuz fiziksel tabela, `SM_Prop_Sign_Director_01` çerçeve geometrisini ve temiz, koyu renkli yazı alanını kullanır. Kaynak atlas yazıları kaldırılmıştır; metinler kayıtlı Canvas'tadır. Yaşam alanı uyarısı `SM_Prop_Sign_OutofOrder_01` ve tek bir ahşap direkle sahile yerleşir.
- Mevcut dört afiş ve iki portrenin çerçevesi, `SM_Prop_Blackboard_01` modelindeki gerçek ahşap rim/bevel geometrisini kullanır. Baskılar yerel Canvas üzerinde kalır; gizlenen afişler etkinleştirilmez. Koridor afişleri kamera kadrajına sığar.
- Masa, bant ve dört atık kutusu sanatçının mesh'lerini kullanır. Normal F analiz cihazının kâğıt ve sinyal parçaları kendi prefab çocuklarıdır. T / Son rapor cihazı, fiziksel faks modeli, ışık/çıktı animasyonu, kısayol ve tekrar rapor kodu kaldırılmıştır. Analiz sonucu otomatik belge olarak açılır; Bakanlık acil faksı tek seferlik sesle ve zorunlu onayla gelir.
- Katlı gazete ve açık/kapalı kitap fiziksel objelerdir. Kitapçık açılınca fiziksel kitap da açılır; gazete okunurken masadaki katlı kopya gizlenir. `WorkstationDocuments` yalnız belge değişimi olayında görünürlüğü günceller.
- Basit ambalaj küpü gerçek `SM_Prop_CardboardBox_01`, karar düğmeleri `SM_Prop_Elevator_Button_01`, sahil kayaları ve ağaçları Polygon Office proplarıyla değiştirilir.
- Oyuncunun arkasındaki duvarda yalnız fotoğraf içeren Atatürk ve Cumhurbaşkanı portreleri bulunur. Fotoğraflar oranı bozulmadan çerçeveyi doldurur; taşan kenarlar kayıtlı maskeyle kırpılır, boş kâğıt bandı bırakılmaz. İki arka köşede Türkiye/BM makam bayrakları vardır. Kumaşlar ortak kayıtlı mesh'i, ayrı gerçek bayrak dokularını kullanır; kumaş simülasyonu çalışmaz. [Görsel kaynakları](../Assets/Art/OfficeIdentity/SOURCES.md).

İçe aktarılan paketlerde ayrı bir dağ modeli bulunmadı. Fotoğraf/tek düzlem arka plan kaldırıldı; çevreyi 360° saran, zirve ve vadileri olan **7.453 köşeli kayıtlı 3B arazi mesh'i** kullanılır. Arazi rengi eğim/yükseklikten gelir; düşük bitki tonları ve kireç taşı sırtları URP ışığı ve sisle görünür. Arazi yalnız Editor'de üretilir, runtime'da değişmez. Deniz ve kıyı tabanı ufukta boşluk bırakmayacak şekilde genişletildi.

## Düzenleme ve doğrulama

GameUP32 → Authoring → **Rebuild coastal facility and opening**, Game sahnesini ve kayıtlı açılış assetlerini yeniden hazırlar. Menü ve First sahnesi yeniden yazılmaz. Önce mevcut sahne düzenlemelerini kaydet; bu komut Game sahnesini yeniden oluşturur.

- `CinematicAuthoring`: kamera animasyon eğrileri, Timeline, söz ve açıklama UI'ı.
- `OpeningEffectsAuthoring`: kayıtlı logo yükselişi, Timeline ControlTrack havai fişekleri ve caretta su izleri.
- `EntranceDoorAuthoring`: gerçek çerçeveye oturan menteşeli kapı yaprakları ve kayıtlı açılma klibi.
- `OfficeIdentityAuthoring`: arka duvar portreleri, ayaklı bayrakların kayıtlı mesh/materyalleri ve import ayarları.
- `CoastalEnvironmentAuthoring`: Polygon Office mimarisi, sahil ve beş caretta.
- `FacilityPropAuthoring`: gerçek paket/sanatçı propları, dağ mesh'i ve çevre envanteri.
- `FacilitySceneAuthoring`: mevcut sahne düzenini koruyan tabela/portre yenilemesi ve kullanım dışı cihaz temizliği.
- `WallArtFrameAuthoring`: paylaşılan ahşap afiş/portre çerçevesi.
- `ArtistGameKitAuthoring` / `ArtKitReview`: binanın arkasındaki EditorOnly model sergisi, odaklama ve görsel denetim. [Kullanım](ARTIST_GAME_KIT.md).
- `SceneArtCorrections`: kaydedilmiş kullanıcı yerleşimlerini koruma.
- `CoastalAssetSetup`: URP materyalleri, EPO kamera/obje ayarları.
- `IntroCinematic`: yalnız açılışın oynatılması ve kamera/HUD devri; açıklama metni sadece sahne değişince güncellenir.
- `ShorelineTurtle`: önceden kayıtlı aktörlerin kıyı ve yüzme hareketi.

`GameUp.Editor.CoastalReview.Capture` gerçek URP görüntülerini [önizleme klasörüne](Previews/Coastal) kaydeder; logo, kapılar, arka oda, ahşap çerçeveler, masa tabelaları ve direkli sahil uyarısı dahil. [Unity doğrulama raporu](../VALIDATION.md), yerel `Logs/art-frames-kit-tests.xml`, `Logs/art-frames-kit-tests.log` ve `Logs/art-frames-kit-author.log` test kanıtlarıdır.

Kontroller: sanatçı aktörlerinin animasyonu/baş yönü; yüzmenin yön ve derinlik değiştirmesi; suya giriş → ortalanmış logo → gerçek parçacıklar → kapı açılması → koridor sırası; doygun renk ve kıvılcım boyutu; efektlerin kapanması; kapalı kapının geçidi örtmesi ve açılan kapıların kameraya yol vermesi; kapılar hareket ederken kamera küre taraması; arka duvar portreleri ve hacimli bayraklar; 16 sanatçı parçasının kullanılması; gerçek tabela/afiş geometrisi ve yazı taşması; geçerli arazi; son kamera pozunun eşleşmesi; outline, söz ve su kaynakları. Üretim testi 90 saniyelik girişi doğal süresinde tamamlar, kapıların bitişte açık kaldığını doğrular, atlama girdilerini reddeder ve üç günlük kampanyayı yürütür.

## Görsel ve içerik kaynağı

Gıda–su–iklim ilişkisi, kaynak izleme, soğuk zincir ve atık azaltımı oyunun gıda güvenliği odağını destekler. Tematik referans: [COP31 Gıda Güvenliği](https://cop31.tr/tr/cop31-aksiyon-ajandasi/gida-guvenligi). Açılış alıntısının metni kullanıcının paylaştığı görselden alınmıştır. Logo mevcut resmî COP31 görselidir; özgün renk ve oran korunur. Portreler ATAM ve COP31 kaynaklarından alınan gerçek fotoğraflardır; yeni yapay zekâ görseli kullanılmaz.

Bu dal yerel çalışma içindir. Son adımda pull, push, PR veya main merge işlemi yapılmaz.

1 Kasım doğrulaması: **48/48 geçti**. Tam sinematik, normal F analizi, Bakanlık acil duyurusu, kapılar, ahşap çerçeveler, izole model sergisi ve üç günlük kampanya doğrulandı. Son rapor `Logs/art-frames-kit-tests.xml` içindedir. Kullanıcının isteğiyle build alınmadı.
