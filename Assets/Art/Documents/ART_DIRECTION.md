# Gazete kâğıdı

`Newsprint.png`, yerleşik image_gen aracıyla üretilmiş yazısız gazete kâğıdı dokusudur. Tam prompt `generation-prompts.json` içindedir. Keten tonlu ince kâğıt, hafif kat izi ve düşük kontrastlı lifler; güncel yerel gazete görünümü amaçlanır.

Manşet, künye, tarih, fotoğraf, altyazı, sütunlar ve günlük kurallar Unity'de kaydedilmiş ayrı UI öğeleridir. Metin bitmap'e gömülmez; oyun tarihinden ve mevcut DocumentDefinition/RuleInfo verisinden gelir. Yeni bir haber kopyası veya çalışma sırasında UI üretimi yoktur.

COP31 duvar afişleri, resmî yatay logo ve mevcut lisanslı/üretilmiş görsellerle Editor'de düzenlenmiş world-space Canvas öğeleridir. Logo renkleri ve oranı korunur. Kaynaklar: `Assets/Art/Menu/ART_DIRECTION.md`.

## Aktif haber fotoğrafları

Day1Products.png, Day2Products.png ve Day3Products.png, CampaignPhotoAuthoring ile Esmanur'un mevcut modellerinden Unity'de render edilmiştir. Yeni bir model veya yapay zekâ fotoğrafı kullanılmaz. İlk fotoğraf et/süt, ikinci fotoğraf meyve, üçüncü fotoğraf yumurta ve soğuk zincir ürünlerini gösterir. Matbaa düzeni, metinler, tarihler ve günlük kurallar ayrı UI öğeleri olarak kalır. Sol duvar afişi de Day1Products görselini kullanır.
