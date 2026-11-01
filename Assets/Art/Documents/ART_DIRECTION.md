# Gazete kâğıdı

Güncel görünüm `NewspaperPaperV2.png` kullanır. Kullanıcının 1 Kasım'da paylaştığı gazete referansından imagegen ile yalnız boş, yıpranmış kâğıt çıkarıldı; kenarlar gerçek alpha içerir. V2, gazete, faks ve vardiya raporunun ortak dokusudur. Eski `Newsprint.png` önceki kaynak olarak korunur.

`NewspaperReference.jpg` kullanıcının referansıdır. `CarettaNewsPhoto.asset`, bu dosyanın yalnız caretta fotoğrafını gösteren kalıcı Sprite'ıdır: `(x=160, y=612, w=1020, h=580)`; Unity alt-sol koordinat sistemi. Referanstaki örnek tarih, balık kuralı ve anlamsız metinler oyunda kullanılmaz. Güncel üç baskı `Docs/Previews/Workstation/NewspaperDay1–3.png` görüntülerindedir.

`Newsprint.png`, yerleşik image_gen aracıyla üretilmiş yazısız gazete kâğıdı dokusudur. Tam prompt `generation-prompts.json` içindedir. Keten tonlu ince kâğıt, hafif kat izi ve düşük kontrastlı lifler; güncel yerel gazete görünümü amaçlanır.

Manşet, künye, tarih, fotoğraf, altyazı, sütunlar ve günlük kurallar Unity'de kaydedilmiş ayrı UI öğeleridir. Metin bitmap'e gömülmez; oyun tarihinden ve mevcut DocumentDefinition/RuleInfo verisinden gelir. Yeni bir haber kopyası veya çalışma sırasında UI üretimi yoktur.

COP31 duvar afişleri, resmî yatay logo ve mevcut lisanslı/üretilmiş görsellerle Editor'de düzenlenmiş world-space Canvas öğeleridir. Logo renkleri ve oranı korunur. Kaynaklar: `Assets/Art/Menu/ART_DIRECTION.md`.

## Ürün fotoğrafları ve önceki baskı kaynakları

Day1Products.png, Day2Products.png ve Day3Products.png, CampaignPhotoAuthoring ile Esmanur'un mevcut modellerinden Unity'de render edilmiştir. İlk fotoğraf et/süt, ikinci fotoğraf meyve, üçüncü fotoğraf yumurta ve soğuk zincir ürünlerini gösterir. Belge veri assetlerinde ve tesis afişlerinde korunur; güncel gazetenin ana görseli kullanıcının caretta referansıdır. Matbaa düzeni, metinler, tarihler ve günlük kurallar ayrı UI öğeleri olarak kalır.
