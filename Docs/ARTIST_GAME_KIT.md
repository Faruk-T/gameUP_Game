# Esmanur Game Kit

Game sahnesinde, binanın arkasında **Esmanur_GameKit** kökü vardır. Merkezi `(0, 0, -27)`; 40 × 27 metrelik inceleme platformu arka duvardan ayrı durur.

## Görüntüleme

1. `Assets/Scenes/Game.unity` sahnesini aç.
2. **GameUP32 → Art → Focus Esmanur game kit** komutunu seç.
3. Scene görünümünde tüm sergiye odaklanılır. Hierarchy'de bir modeli seçip **F** ile tek modele yaklaşabilirsin; Alt + sol fareyle çevresinde dön.

Hierarchy grupları:

- `Products_31`: süt, peynir, yoğurt/yumurta, et ve meyve sıraları. Her ürün orijinal kaynak prefabıyla, oranı korunarak sergilenir.
- `Environment_16`: Environment.fbx içindeki 16 mesh parçasının her biri ayrı masada ve kendi adıyla görünür; kitaplar, gazete, masa, bant parçaları, dört kutu, analiz cihazı, kâğıtlar ve sinyaller dahil.
- `Caretta_1`: kaynak caretta, animasyondan alınmış sabit bir örnek pozda. Sergi aktörü hareket sistemi veya Animator çalıştırmaz.

Toplam **48 kaynak model/parça** vardır. T cihazıyla kaldırılan `fax` ve `fax signal` yalnız bu inceleme sergisinde bulunur; oyun etkileşimleri geri eklenmez. Kaynak malzemeler ve mesh'ler paylaşılır; oyun koşulu varyantları sergiye kopyalanmaz.

## Ayrım ve düzenleme

Kök ve tüm çocukları **EditorOnly**, çizim katmanı **30** olur. Oynanış ve sinematik kameraları bu katmanı çizmez; EditorOnly hiyerarşisi player build'inden ayıklanır. Sergide collider, WorkstationTarget, kamera, Animator veya oynanış bileşeni yoktur. Sahnedeki ürün sayısını ve haber/kural akışını değiştirmez. Aktif oyun envanteri, sergi kökünü saymaz.

**GameUP32 → Art → Refresh art frames and Esmanur game kit**, mevcut sahneyi kaydettikten sonra sergiyi ve altı afiş/portre çerçevesini yeniler. Diğer oda objeleri ve metin yerleşimleri korunur. Hazırlama kodu yalnız Editor assembly'sindedir; runtime nesne, mesh veya materyal üretilmez.

## Çerçeve seçimi

Sekiz paket modeli gerçek URP görüntülerinde karşılaştırıldı. **SM_Prop_Blackboard_01** modelinin ahşap, köşeleri düzgün ve içi girintili çerçevesi seçildi. Yatay model dikey kullanılır; orijinal ahşap malzeme ve bevel geometrisi paylaşılır. Mevcut afiş/portre baskısı iç yüzeyi örter; içerik, fotoğraf oranı ve kaydedilmiş duvar yerleşimleri değişmez.

[Çerçeve karşılaştırması](Previews/Coastal/FrameCandidates.png) · [Sergi genel görünümü](Previews/Coastal/GameKitOverview.png) · [Ürünler](Previews/Coastal/GameKitProducts.png) · [Çevre parçaları](Previews/Coastal/GameKitEnvironment.png)

Doğrulama: **48/48 test geçti**. Kaynaklar: `Logs/art-frames-kit-author.log`, `Logs/art-frames-kit-tests.xml`. Yeni test, 31 ürün kaynağının, 16 çevre parçasının ve carettanın gerçek mesh referanslarını; EditorOnly etiketlerini; kamera katmanını ve oynanış bileşeni bulunmamasını denetler.
