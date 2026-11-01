# Aktif üç günlük kampanya

Bu dosya önceki iki günlük balıklı prototipin yerine geçen oynanış kapsamını açıklar. Çalışma dalı `feat/lead-coastal-cinematic`. Başlangıç First → Menu → Game, Unity 6000.3.13f1. Yeni görevde gazete öncesi [kıyı ve tesis açılışı](COASTAL_CINEMATIC.md) oynar.

| Gün / tarih | Açılan grup | O gün gelen ürünler | Gazetenin konusu | Oyuncunun uyguladığı kontrol |
|---|---|---|---|---|
| 1 / 1 Kasım | Et ve süt | 8 ürün | Küresel ısınma ve gıda tedarikinin yıl boyu süren riski | SKT, tür beyanı, küf/renk değişimi/şişme |
| 2 / 2 Kasım | Meyve | 10 ürün; meyve, et ve süt | Meyvede çevresel kalıntı ve atığın kıyılara etkisi | Önceki kontroller + laboratuvar analizi |
| 3 / 3 Kasım | Yumurta | 13 ürün; dört grubun tamamı | Soğuk zincir, enerji sürekliliği ve kaynak takibi | Önceki kontroller + gün ortası geri çağırması |

İlk gün Antalya'da mevsim dışı bir sıcak hava olayı yaşandığı iddia edilmez. Gazete küresel iklim riskinden söz eder. Aktif gazetelerde balık veya dip balığı yasağı bulunmaz. Caretta carettalar, atıkların kıyılara ulaşmasını önleme bağlamında ilk iki günün haberlerinde korunur.

## Haber ve kuralların bağlantısı

Her gazetenin RelatedRuleIds listesi, günün InitialRules listesiyle eşleşir. Haberin altında aynı snapshot'ın kısa kuralları basılır. Fotoğraflar yalnız mevcut sanatçı modelleri kullanılarak Unity'de render edilmiştir; her baskı o günkü ürün gruplarını gösterir. Haberler kurmacadır; gerçek üretici/parti uyarısı değildir.

Üçüncü gün ilk üç ürün, ayrıştırmaları dâhil tamamlanınca Bakanlık acil faksı gelir. Faks onayından sonra **B bölgesinden gelen et, süt ve yumurtalar** temiz analizle bile reddedilir. **Meyveler bu duyurunun kapsamı dışındadır.** Önceki kararlar değişmez.

- D3-01: B kaynaklı süt; faks öncesi uygun.
- D3-04: B kaynaklı peynir; faks sonrası reddedilir.
- D3-05: B kaynaklı çilek; normal kontrolleri geçiyorsa onaylanır.

## Modeller ve bozulma

Esmanur'un 31 kaynak prefabı kullanılır; hepsi sırayla masaya gelir. Teşhir dolapları kaldırılmıştır. Her kaynak için Fresh ve Spoiled olmak üzere 62 kaydedilmiş görünüm varyantı vardır. Yeni ürün mesh'i import edilmez veya kopyalanmaz.

Spoiled malzemeleri Editor kodu üretir. FoodCondition shader'ı orijinal doku üzerinde renk kaybı/küf deseni ve ambalaj şişmesi uygular. Oyun sırasında prefab, materyal veya UI oluşturulmaz. Etiket damgaları ürün boyutuna göre küçük tutulur; tam etiket inceleme kartında okunur.

VisibleSpoilage kuralı görünür bozulmayı değerlendirir. Görünür bozulma ile gizli çevresel kirlilik ayrı verilerdir; temiz görünen numune pozitif analiz verebilir. SKT geçmiş bir ürünün görünümü sağlam kalabilir. Kararı yalnız ProductRuleEvaluator verir.

## Kabul ölçütleri

31/31 doğru karar, 15/15 doğru ayrıştırma ve **340 puan** beklenir. Tümüne onay veren oyuncu 16/31, tümüne red veren oyuncu 15/31 doğrulukla başarısız olmalıdır. [Ürün matrisi](CAMPAIGN_MATRIX.md), [model envanteri](MODEL_INVENTORY.md).

Önceki kampanyanın assetleri ve prefabları GUID'leri korunarak Assets/Tests/Editor/LegacyContent altına alınmıştır. Aktif Game sahnesi bu arşive bağlı değildir. Faruk'un eski kabul testleri referans motoru sınamaya devam eder.

## Editor komutları

- Render campaign press photographs: mevcut ürünlerden üç haber fotoğrafını render eder; grafik aygıtı gerektirir.
- Rebuild Game room and newspaper: kampanya assetlerini, 62 varyantı, 31 ürün prefabını ve Game sahnesini kaydeder.
- Generate healthy and spoiled artist variants: yalnız görünüm varyantlarını yeniler.
- Esmanur model inventory: aktif ürün ve kaynak bağlantı raporunu yeniler.

Hepsi GameUP32 menüsü altındadır. Otomatik importta veya oyuncu açılışında çalışmazlar.
