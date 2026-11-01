# Ürün, laboratuvar raporu ve karar tutarlılığı

Unity `6000.3.13f1`; çalışma dalı `feat/lead-coastal-cinematic`. Windows player build alınmaz.

## Bulunan sorunlar

- Karar motoru gerçek SKT, bozulma, tür, kalıntı ve aktif kural kopyasını kullanırken faks ayrı kaydedilmiş `analysisFindingDetail` metnini okuyordu. Bu iki kaynak birbirinden kopabiliyordu. Kalıntı testinin temiz olması, ürünün SKT veya geri çağırma açısından geçerli olduğu anlamına gelmez.
- Üç yoğurt kaynak dokusunun kapağında `07 DIC 2021` baskısı vardı. Kampanyanın 2026 tarihinden bağımsız bu yazı, oyuncuya yanlış SKT gösteriyordu. Kaynak sanat dosyası korunur; Editor UV bake adımı bu alanı temizler. Ön ve arka baskı artık doğrudan ürünün gerçek SKT'sini taşır.
- İlk gün yoğurdunun gerçek SKT'si `01.11.2026`; vardiya da `01.11.2026`. Mevcut kitapçık kuralına göre aynı gün geçerlidir. Faks bunu **BUGÜN SON GÜN** olarak açıkça belirtir. Geçmiş SKT aynı kuralla reddedilir; işletim sistemi saati kullanılmaz.
- Antalya Yörük Beyaz Peyniri `D1-03` görünür olarak bozuk değildir; SKT'si `25.10.2026` olduğu için reddedilir. Rapor taze görünümü kimyasal ve SKT durumundan ayırır; bu ürüne küf/şişme bulgusu uydurmaz.
- Son gün Bölge B kaynaklı peynir `D3-04`, numune temiz olsa da onaylanmış geri çağırma nedeniyle reddedilir. Bölge B meyvesi duyuru dışında kalır. Önceki ürünlerin dondurulmuş kural kopyası değişmez.
- Yeni FBX'lerdeki parça ve malzeme adları önceki kutu sürümünden farklıydı. Yeni adlar açıkça eşleştirildi. Kutunun FBX eksen dönüşümü korunur; onay/red butonları gerçek masa üzerine sığar, eski görsel kopyaları kaldırılır.

## Tek değerlendirme kaynağı

`ProductInspectionReport`, `ProductDefinition` ve o ürüne ait `RuleSnapshot` üzerinden oluşturulur. `ProductionInspectionBackend` hem rapora hem puanlamaya aynı snapshot dönüşümünü verir. Serbest yazılmış eski bulgu metni aktif faksın kaynağı değildir.

Raporun başında ürün kimliği, denetim tarihi, kural sürümü ve **nihai kararın gerekçesi** yer alır. Altındaki ayrı alanlar şunlardır:

1. Çevresel kalıntı: pozitif / negatif.
2. Mikrobiyolojik/fiziksel durum: uygun / uygunsuz; süt, peynir, yoğurt, et, meyve, yumurta için farklı açıklama.
3. İçerik ve tür: doğrulandı / doğrulanmadı; gerçek beyan ve içerik.
4. Kaynak ve SKT: üretim yeri, sevkiyat bölgesi, etiket tarihi, vardiya tarihi ve geçerli/bugün son gün/geçmiş durumu.
5. Red sonrası ambalajın ayrıştırma hedefi.

Üretim yeri sevkiyat kaynağından ayrıdır. Örneğin Burdur/Bucak üretimli bir süt, Bölge B sevkiyat deposundan gelebilir. Kampanya ürünleri ve üretim kayıtları kurmacadır.

Kimlik doğrulama, görünür bozulma ve çevresel kalıntı deterministik kayıtlı oyun verisidir; analiz rastgele sonuç üretmez. Aynı oturum/ürün raporu önbellekten aynı nesne olarak gelir; farklı ürünler farklı rapor alır. Yanlış oturum ve ürün kimliği reddedilir.

## Tekrar çalıştırılabilir Editor aracı

**GameUP32 → QA → Product inspection audit** menüsünü açın; Play Mode kapalıyken **Tüm denetimleri çalıştır** düğmesine basın.

Araç kayıtlı kampanya ve Game sahnesini okur. Ürün listesinde her rapor ayrı incelenebilir, ürün asseti seçilebilir ve Markdown sonuç dışa aktarılabilir. Ürün kararlarını veya asset verilerini değiştirmez.

- 31 üründe iki karar, dört ayrıştırma hedefi, puan farkları ve tek seferlik sonuç kaydı.
- Rapor kimliği, önbellek ve rapor/karar tutarlılığı.
- Ön/arka SKT ve UV baskı imzası; veri değişip baskı yenilenmemişse hata verir.
- Gerçek sahnedeki ürün havuzu, tanım/baskı eşleşmesi ve bozulma shader'ı.
- Üç günlük analizli ve analizsiz doğru oynayış: 31 karar, 15 ayrıştırma, 340 puan, sıfır yanlış.
- Her gün sayaç sıfırlanması, üç yanlışta kayıp, restart, yanlış karar ile yanlış ayrıştırmanın ayrı sayılması ve yinelenen callback'in yeniden puan yazmaması.

**GameUP32 → QA → Capture product inspection visuals**: tüm ürünlerin ön/arka görüntüsü, üç yoğurt kapağı, yedi farklı gerçek faks ekranı ve kaynak modellerin ekran görüntülerini üretir. `Docs/Previews/InspectionAudit` içinde görseller, `Logs/InspectionAudit/Products` içinde 62 tekil ürün görüntüsü bulunur. Sahne düzenini kaydetmez.

Toplu denetim giriş noktası: `GameUp.Editor.CampaignInspectionAudit.RunBatch`. Makinece ve insan tarafından okunabilir kalıcı rapor: [Reports/PRODUCT_INSPECTION_AUDIT.md](Reports/PRODUCT_INSPECTION_AUDIT.md).

Veri veya model değişikliğinden sonra **GameUP32 → Authoring → Update product inspection and supplied models**, kampanya kurgusunu sıfırlamadan ürün protokollerini, UV baskılarını ve ilgili tesis modellerini yeniden hazırlar. Çalışma alanındaki diğer dekor ve sinematik düzenine dokunmaz.

## Regresyon doğrulaması

Son Unity koşusu: **100/100 geçti**, 0 başarısız, 0 atlanan; **152.09 saniye**. Ayrı Editor matrisi: **31 ürün / 1540 kontrol / 0 hata**. En güncel tam koşu `Logs/AudioPolish/tests.xml` ve `tests.log` içindedir; önceki ürün tanı kayıtları `Logs/InspectionAudit` altında tutulur. Build alınmadı.

`ProductInspectionRegressionTests`, her ürün için evaluator'dan türetilmeyen ayrı senaryo beklentileri kullanır. SKT sınırı, gerçek takvim/artık yıl, peynir/yoğurt örnekleri, geri çağırmadan önce/sonra aynı ürün ve temiz numune ayrımı, eski rapor metninin yeni sonucu değiştirememesi, kaynak modellerin bağlantıları denetlenir.

Mevcut uçtan uca oyun testi, 31 ürünün tamamını gerçek analiz/faks kuyruğundan geçirir; görünen belge gövdesinin doğru raporla eşleşmesini ve çizilebilir yazı üretmesini doğrular. Faks sireni ve hatalı işlem bildirimleri mevcut testlerde korunur.

Yeni modellerin kaynağı kullanıcının 1 Kasım 2026 yüklemeleridir. Proje içi dosyalar: `Assets/Art/Environment/Mesh/Trashbin.fbx` ve `buttons.fbx`; dış Downloads klasörüne runtime referansı yoktur. Çöp kutusunun mevcut GUID'si korunmuştur.

Model SHA256: Trashbin `01952C34ACDADEB5D9271D0B420E1DC9AF7A9DEA5466CF3F6C39AEE63827D9CB`; buttons `A0CDEAB1127C9C2DBA289703C55B0E9B7397E147B557AFF431D5FF4F4430C9DD`.

Yassı ürünlerde arka UV baskısının yönü düzeltilmiştir; yumurta üzerindeki baskı kartona sınırlandırılır. `ProductWorldLabelBinder` v4 imzası güncel basılı veriyle karşılaştırılır. Sergi kutuları da mevcut Game Kit ile birlikte EditorOnly / katman 30 olarak kalır.

## Bitişik geri dönüşüm istasyonu

Kullanıcının yeniden verdiği `Trashbin.fbx`, projede kullanılan dosyayla aynı SHA256'ya sahiptir; aynı kaynak dosya korunmuştur. Dört aktif kutu artık mevcut sıra merkezi korunarak aralıksız yerleştirilir. Her kutu 0.72 m genişliğindedir; toplam sıra 2.88 m'dir. Kutu collider'ları model ölçüsüne daraltılır ve `WastePoint_0..3` yeni konumlara taşınır. Böylece komşu kutuların tıklama alanları çakışmaz ve ayrıştırma animasyonu doğru kutuya gider. Ana menünün tesis kopyası da güncellenmiştir.

Bu yerleşim **GameUP32 → Authoring → Update adjoining recycling bins** komutuyla tekrar uygulanabilir; ürün verileri ve butonlar değiştirilmez. Son odaklı doğrulama **5/5 geçti**: gerçek oyuncu etkileşim mesafesinden dört ayrı raycast, sıfır geometrik boşluk, collider ayrımı, hareket hedefleri, sahne/prefab bağlantıları, sergi izolasyonu ve mevcut ürün denetim matrisi. Kayıtlar `Logs/RecyclingStation/tests.xml` içindedir; build alınmadı.
