# Main entegrasyonu — 1 Kasım 2026

Kaynak: `origin/main`, **3db0fa785110f3a28bbf9f6794e84f6b9388a47b**. Çalışma dalı: `feat/lead-coastal-cinematic`. Main dalına merge veya push yapılmaz; gerekli değişiklikler mevcut projeye seçilerek uygulanır.

## Alınanlar

- `09154a6`: günlük gazete özeti, günlük odak, etkin kurallar, atık rehberi, kontroller ve puanlama içeren kapsamlı el kitapçığı. İçerik, oturum denetleyicisini büyütmemek için `HandbookContent` içine ayrılır. Günün tarihi, ürün grupları, haberler ve kurallar aktif oturumdan okunur. Acil kural bölümü yalnızca duyuru onaylandıktan sonra görünür; yeniden başlatmada temizlenir.
- Aynı commit'teki belge satır aralığı/kaydırma iyileştirmesi ve geri bildirim konumu. Kaydedilmiş Game sahnesinin yalnız ilgili UI bileşenleri yenilenir; oda veya sinematik yeniden üretilmez. Editor komutu: `GameUP32/Authoring/Refresh handbook layout`.
- Play Mode/yeniden yükleme sırasında yok edilmiş ürün görseline erişimi ve olay aboneliği temizliğini koruyan düzeltmeler.
- `e04f42e`: `AnalysisService.cs` → `ProductAnalysisProcessor.cs`, `RunResults.cs` → `RunResultsStore.cs`; main ile aynı yeni script GUID'leri. Normal F analizi korunur.
- `69fd6ea` içindeki Esmanur çevre ve caretta kaynakları mevcut dalda zaten bulunur. Kaynak model ve prefab farkı yoktur; mevcut gazete malzemesinin bu dalda yapılan URP düzeltmesi korunur.

## Mevcut oynanışa uyarlamalar

Main kitapçığındaki sabit puan metni yerine `ProductRuleEvaluator` sabitleri kullanılır: doğru karar +10, doğru ayrıştırma +2, hatalı red -10, hatalı onay -15, yanlış kutu -2. Böylece mevcut 31 ürünlük kampanyanın en iyi sonucu 340 puan kalır.

Kontamine ambalaj açıklaması hem görünür bozulmayı hem POZİTİF analizi kapsar. Yalnız SKT, etiket veya geri çağırma yüzünden reddetmek ambalajı kontamine yapmaz. Bunlar mevcut ürün verileri ve değerlendiriciyle aynı davranıştır.

## Bilinçli olarak alınmayanlar

- **T / Son rapor** kısayolu, yeniden rapor açma API'si, Report hedefi, fiziksel faks istasyonu ve FaxPresenter. F analizi ve otomatik rapor belgesi çalışmaya devam eder. Bakanlık acil faksı zorunlu belge/ses bildirimi olarak korunur.
- Editor açılışında veya Play Mode çıkışında otomatik `AuthorGame()` çalıştıran kod. Bu kod kaydedilmiş sahne üzerindeki düzenlemeleri tekrar üretirdi.
- Main'deki eski Game sahnesi: sahil, carettaların yüzmesi, ortalanmış logo/havai fişekler, kapı animasyonu, koridor, portreler/bayraklar, tabelalar ve EditorOnly Esmanur game kit korunur.
- 31 ürün prefabının yeniden üretilmiş kopyaları: yerel fileID'ler normalize edilerek karşılaştırıldığında içerik farkı yoktur.
- Kullanılmayan `HandbookCover` malzemesi ve mevcut dalın daha önce gerçek modellerle değiştirdiği geçici/fallback çevre üretimi.

## Doğrulama

Yeni içerik testleri üç günün gazete/kurallarını, acil duyurunun onay sınırını ve yeniden başlatmayı denetler. Gerçek sahneli oynanış testi kitapçığın kaydırılmasını, metnin içerik alanına sığmasını ve başlığın kesilmemesini de kontrol eder. Sonuçlar kökteki `VALIDATION.md` dosyasındadır. Windows build alınmaz.
