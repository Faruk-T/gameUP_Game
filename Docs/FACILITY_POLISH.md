# Tesis, inceleme, kitap ve el yazısı arayüzü

Çalışma dalı: `feat/lead-coastal-cinematic`. Unity: `6000.3.13f1`.

Kullanıcının bu çalışma öncesinde Game sahnesinde yaptığı düzenlemeler başlangıç noktasıdır. Sağ duvardaki kaldırılan panel, ConveyorDirection tabelasının duvar konumu ve taşınan diğer dekorlar korunur. Başlangıç sahnesinin SHA256 değeri `700D6971E297CF536DEF65771A04992FE8B85C3B2E8F926B0DA7293765E798F7`; yerel yedeği `Logs/FacilityPolish/Before/Game.unity` içindedir. Tam Game sahnesi yeniden kurulmaz.

## Canlı ana menü ve pause

Ana menü artık bir liman resmi göstermez. `MenuFacility.prefab`, kayıtlı Game sahnesindeki InspectionOffice görsel ortamından Editor'da hazırlanır; Menü sahnesindeki gerçek Camera aynı çalışma alanını render eder. Ortamda oynanış yöneticileri, etkileşim scriptleri, Collider veya ek AudioListener yoktur. Sahneler First → Menu → Game olarak kalır; dördüncü sahne veya runtime ortam klonlaması eklenmez.

Sahne yerleşimi değiştiğinde `GameUP32/Authoring/Apply facility, UV printing and guide book` komutu ortamı güncel Game kaydından tekrar hazırlar. Yalnız yerleşim/UI için `FacilityPolishAuthoring.ApplyScenes` çalıştırılabilir; bu yol ürün atlaslarını tekrar üretmez.

Pause, ana menüyle aynı boş metal klipsli pano ve sarı kâğıt düğmelerini kullanır. Devam, Ayarlar ve Ana Menü bağlantıları mevcut PauseMenu üzerindedir. Ses/hassasiyet/FOV/tam ekran aynı SettingsPanel/UserSettings sisteminde kalır. Analiz ve ürün hareketi duraklatılmaya devam eder.

## Bant ve ürün durağı

Bant sağ duvardaki kullanıcı tarafından açılmış açıklıktan gelir. Dışarıdaki kapalı servis kabul bölümü Polygon Office duvar, beton zemin, tavan ve floresan modellerini kullanır. Esmanur'un bant modelleri iki düz hat ve köşe parçasıyla yerleştirilir; ürün hareketi aynı rotanın kayıtlı waypoint'lerini izler.

- Geliş: `(20, 1.14, 3.55)`.
- Köşe: `(2.1, 1.14, 3.55)`.
- Denetim durağı: `(2.1, 1.14, -3.2)`; oyuncunun z konumuyla aynı, doğrudan sağındadır.
- Sevk: `(2.1, 1.14, -14)`.

Masa sağ kenarı ile bant arasında boşluk bırakılır. Ön cephedeki eski bant geçişi kapatılır; arka sevk açıklığı yeni hizaya taşınır. ConveyorDirection'ın kullanıcının belirlediği dünya konumu korunur.

## İnceleme

Önceki hata, kilitli imlecin değişmeyen ekran konumundan sürükleme eşiği hesaplanmasıydı. İncelemede doğrudan Input System fare deltası ürünü döndürür; düğme basılı tutma veya ekran konumu eşiği aranmaz. Kamera ve oyuncu dönüşü inceleme boyunca uygulanmaz.

E / sol tık ürünü alır; fare hareketi veya yön tuşları döndürür; tekerlek yaklaştırır; E / sağ tık bırakır. Döndürme mesh merkezini sabit tutar. Editor'da ölçülen bounds, her ürünün en uzun kenarını incelemede yaklaşık 0.86 metreye getirir. Bırakıldığında özgün ölçek, konum ve dönüş geri yüklenir.

Ürün alınırken baskı yüzü kameraya yönlendirilir; düz paketlerde üst yüz, kutularda ön yüz kullanılır. İnceleme ipucu etiketleri kapatmaması için ekranın altına taşınır.

## Ürünün UV'sine işlenen yazılar

Eski Front/Back/DateFront/DateBack yazı düzlemleri kaldırılır. Sanatçının özgün mesh ve texture assetleri değiştirilmez. Üretim prefabı için mesh kopyası oluşturulur ve çakışmayan UV atlası hazırlanır. Böylece aynalanmış veya ortak kaynak UV adaları ön/arka metni birbirine karıştırmaz.

Editor GPU bake adımı özgün albedo/renk, ön ürün bilgisi ve arka künye/SKT/içerik/ambalaj bilgisini tek kalıcı atlas içine birleştirir. Yeni UV doğrudan ürünün mesh'inde kullanılır; yazı ürünle birlikte kıvrılır ve deforme olur. Ayrı etiket Renderer'ı, world-space ürün Canvas'ı veya runtime texture üretimi yoktur. Kaynak malzemelerin normal map taşımadığı audit ile doğrulanmıştır.

Atlaslar `Assets/Art/ProductPrints` altındadır: 2048², Windows BC7, mipmap, Clamp ve Read/Write kapalı. Modelin alt malzeme bitişleri ve bozulma shader'ı korunur. Süt kutularındaki eski tarih baskı alanı da atlasın içinde temizlenir. Siyah/başarısız shader çıktısının kaydedilmesini engelleyen Editor doğrulaması bulunur.

Eski `ProductLabels` klasöründeki 198 asset için proje geneli GUID referans taraması yapılmış, dış referans bulunmadığı doğrulandıktan sonra yerel `Logs/FacilityPolish/RetiredProductLabels` yedeğine alınmıştır. Eski mesh/materyal/texture dosyaları etkin projeden çıkarılmıştır.

## Özgür'ün kitabı

Kaynak: `origin/feat/guide-book`, `b317b5f`; uzaktaki main entegrasyonu `62ffba9`. Yalnız kitap klasörü alınmıştır; uzaktaki sahne mevcut kullanıcı Game kaydının üstüne yazılmamıştır.

Özgür'ün gerçek Book-Page Curl prefabı, maskeleri, köşe hotspotları ve kıvrılma animasyonu kullanılır. `GuideBookView`, mevcut DocumentPresenter üzerinden `HandbookContent.Build(Flow)` metnini ve gerçek DayDefinition gün/tarih bilgisini alır. Gün, `Book.TrySetDay(day.DayNumber)` ile açıkça aktarılır. Inspector yedek günü veya sahte GameManager yoktur.

Güncel haber, açılan ürün grupları, yürürlükteki kurallar, yalnız onaylandıktan sonra aktifleşen acil faks, atık hedefleri ve puanlama aynı gerçek metinden sayfalara ayrılır. Metinler dört önceden hazırlanmış UI yüzeyinde gösterilir; çevirme sırasında ilgili Image ile aynı maskelenmiş dönüşü takip eder. Her açılışta ilk sayfaya dönülür. Kıvrılma ortasında kapatma, parent/sibling sıfırlamasını lifecycle içinde yapmaz; Özgür'ün LateUpdate düzeltmesi korunur.

B/Esc kontrolünü tek WorkstationInteractionController yürütür. Ayrı BookToggle veya EventSystem sahneye eklenmez. Sayfa çevirme mevcut AudioManager'dan kitap sesi alır. Ayrıntılar `Assets/Book-Page Curl/README.md` içindedir.

## El yazısı fontu ve görseller

Tüm etkin sahne metinleri, ürün baskıları, menü ve kitabın canlı metinleri Patrick Hand kullanır. Türkçe ÇĞİÖŞÜçğıöşü karakterleri Unity font API'siyle doğrulanır. Resmî logo ve fotoğraf içerikleri korunur. Font ve OFL lisansı `Assets/Fonts` altındadır; kaynak [Google Fonts / Patrick Hand](https://github.com/google/fonts/tree/main/ofl/patrickhand).

Menü kâğıtlarının basılı metinleri yerleşik `image_gen` ile temizlenmiştir. Yeni şeffaf görseller `ClipboardPaperV2.png`, `SettingsPaperV2.png`, `CreditsPaperV2.png`; tam promptlar `Assets/Art/Menu/Documents/handwritten-skins-prompts.json` içindedir. Menü metinleri artık texture içine gömülü değildir; tamamı Unity'de aynı fontla yazılır.

## Kontroller

Ham Editor/Unity test ve görsel kayıtları `Logs/FacilityPolish` altındadır. Nihai test sonucu kökteki `VALIDATION.md` dosyasında yer alır. Windows player build alınmaz.

Tam koşu **59/59**, 149.35 saniye; son UI ve kullanılmayan asset temizliği sonrası üç sahnenin referans kontrolü **1/1** geçti. Etiket yüzeyleri, kapalı sağ servis girişi, dört menü ekranı, gerçek kitap sayfaları ve pause görünümü görsel olarak kontrol edildi.
