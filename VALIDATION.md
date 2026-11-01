# Aktif kampanya ve sinematik doğrulaması — 1 Kasım 2026

Unity **6000.3.13f1**, Windows x64, D3D11 / RTX 3060 Laptop. Dal: `feat/lead-coastal-cinematic`.

| Kontrol | Sonuç |
|---|---|
| Unity testleri | **44/44 geçti**, başarısız/atlanan yok |
| Gerçek sahneler | First → Menu → Game; tek ApplicationRoot ve EventSystem |
| Atlanamayan açılış | 90 saniye; Esc / restart / karar girdileri atlatmaz; gün Boot durumunda kalır |
| Söz ekranı | Tam metin ve atıf siyah fonda; 1080p ve 720p görüntüsü kontrol edildi |
| Kamera rotası | Kıyı → tesis → iki afiş → masa; küre taramasıyla duvar kesişmesi yok; son poz çalışma kamerasıyla aynı |
| Carettalar | Esmanur'un 5 aktörü; doğru baş yönü; gerçek yüzgeç animasyonu; kavisli giriş, farklı dönüş yönleri, derinlik ve yatış; kıyıya dönmeyen yüzme |
| Logo ve efekt sırası | Yüzme başladıktan sonra logo yükselir; 44. saniyeden sonra 5 havai fişek; 52. saniyede efektler kapanmış, ardından koridor yürüyüşü |
| Tek sinematik saati | Kamera / logo / parçacık Timeline'ı ile aktör pozları ve altyazılar aynı zamandan örneklenir |
| Çevre modelleri | Esmanur'un 16/16 çevre mesh'i; faks/analiz sinyal ve kâğıt çocukları dahil |
| Gerçek paket propları | Polygon Office tabela, afiş, tavan, ağaç, kaya, kutu ve düğmeleri; yazı yüksekliği kontrolü |
| Dağlar | 360° kayıtlı hacimli arazi; geçerli köşeler, zirve/vadiler, eğim/yükseklik rengi; runtime üretim yok |
| Kullanıcı yerleşimleri | Faks, analiz, kitap ve bank düzeltmeleri kayıtlı; yeniden sahne üretiminde uygulanır |
| Fiziksel belgeler | Kitapçık açıkken gerçek açık kitap; kapatınca kapalı kitap; gazete okunurken katlı kopya gizli |
| Kamera / ses devri | Açılıştan sonra tek etkin kamera ve AudioListener; gazete yalnız bitişten sonra açılır |
| URP assetleri | Polygon Office materyalleri, gerçek Stylized Water mesh/dokuları, EPO Render Graph feature; derleme ve shader hatası yok |
| Outline | Native çözünürlük; tek dilation; blur ve duvar arkasından görünüm yok |
| Kampanya | **3 gün, 8 + 10 + 13 = 31 ürün** |
| Kategori açılımı | İlk gün yalnız et/süt, ikinci gün meyve, üçüncü gün yumurta |
| Model kaynağı | Yalnız Esmanur'un 31 prefabı; bütün kaynaklar oynanışta |
| Sağlam/bozuk varyant | 62 kayıtlı prefab; aynı kaynak mesh'ler, bozuk görünüm için shader |
| Shader | Editor URP görüntülerinde ve aktif shader kontrollerinde hata yok |
| Doğru oynayış | **31/31 karar, 15/15 ayrıştırma, 340 puan** |
| Tümüne onay | 16/31 doğruluk; başarısız |
| Tümüne red | 15/31 doğruluk; başarısız |
| Haber / kural / fotoğraf | Günün açılan gruplarıyla eşleşiyor; RelatedRuleIds aynı günün InitialRules listesiyle aynı |
| Kasım haberi | Antalya sıcaklığı iddiası kaldırıldı; küresel ve yıl boyu süren iklim riski |
| Aktif balık içeriği | Ürün/haber yok; aktif sahne eski arşive bağlı değil |
| Acil faks | 3. gün üçüncü ürün ayrıştırması da bitince gelir |
| Geri çağırma sınırı | B kaynaklı süt faks öncesi geçer; B peynir faks sonrası reddedilir; B meyvesi kapsam dışıdır |
| Gazeteler | Üç baskının metni görünür ve kesilmiyor; gerçek katlama düğmeleriyle onay |
| Oda | Teşhir dolapları kaldırıldı; dört arka kutu raycast ile erişilebilir |
| Sol afiş | Başlık görünür ve kesilmiyor; başlık/fotoğraf yerleşimi ayrıldı, oyun görüntüsü kontrol edildi |
| Pause / restart | Analiz donar; eski rapor/callback reddedilir, sonuç sıfırlanır |
| Windows build | Kullanıcının isteğiyle alınmadı; doğrulama Editor kapsamındadır |

Tam kampanyalar Editor PlayMode'da gerçek assetler ve üretim portlarıyla yürütüldü. Bu dalın doğrulaması Editor PlayMode ve D3D11/URP görüntü kontrollerini kapsar. Yeni Windows build'i alınmadı.

Ham rapor: `Logs/props-tests.xml` (44/44). Son arazi düzenlemesinden sonra çevre/sinematik testlerinin tekrarı: `Logs/props-final-art-tests.xml`. Sahne üretimi, mesh envanteri ve shader kontrolü: `Logs/props-final-art.log`; görüntüler: Docs/Previews/Coastal. Tam oynanış ve çalışma zamanı logo/efekt ekranı: `Logs/Visuals`. Yerel loglar Git'e eklenmez. Yeniden çalıştırma script'i sonuçları `Logs/production-tests.xml` adıyla yazar.

[Aktif plan](Docs/ACTIVE_CAMPAIGN.md) · [Ürün matrisi](Docs/CAMPAIGN_MATRIX.md) · [Model envanteri](Docs/MODEL_INVENTORY.md) · [Çevre envanteri](Docs/ENVIRONMENT_INVENTORY.md)

[Sahil, tesis, asset uyarlaması ve açılış](Docs/COASTAL_CINEMATIC.md)

![Açılış sözü](Docs/Previews/Coastal/OpeningQuote720.png)

![Carettalar ve kıyı](Docs/Previews/Coastal/Swimming.png)

![COP31 logosu ve havai fişekler](Docs/Previews/Coastal/LogoFireworks.png)

![Tesis koridoru](Docs/Previews/Coastal/CorridorColdChain.png)

![Birinci gün](Docs/Previews/Newspaper.png)

![İkinci gün](Docs/Previews/NewspaperDay2.png)

![Üçüncü gün](Docs/Previews/NewspaperDay3.png)

![Kodla bozulmuş görünüm](Docs/Previews/SpoiledFruit.png)

![Düzeltilen sol afiş](Docs/Previews/LeftPoster.png)
