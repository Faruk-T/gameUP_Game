# Makam odası görselleri

Dosyalar kaynaklarından değiştirilmeden alınmıştır. Gerçek fotoğraflardır; yapay zekâ portresi kullanılmaz. Çerçevelerde yalnızca fotoğraf görünür; isim veya unvan yazısı bulunmaz.

| Dosya | Kaynak |
|---|---|
| Ataturk.jpg | [Atatürk Araştırma Merkezi – Portreler](https://atam.gov.tr/galeri/ataturk-portreleri/), `Ataturk-Portreleri-4-716x1024.jpg` |
| President.jpg | [COP31 – Recep Tayyip Erdoğan](https://cop31.tr/tr/lider/recep-tayyip-erdogan), `recep-tayyip-erdogan-31.jpg` |
| Turkiye.png | [Flagpedia – Türkiye](https://flagpedia.net/turkey/download/images), kamu malı bayrak çizimi |
| UnitedNations.png | [Flagpedia – Birleşmiş Milletler](https://flagpedia.net/organization/un/download/images), kamu malı bayrak çizimi |

BM bayrağının renk ve amblem referansı: [Birleşmiş Milletler](https://research.un.org/en/maps/flags). Bayraklar oyun ortamında dekor olarak kullanılır.

Afiş ve portre çerçeveleri, Polygon Office paketinin `SM_Prop_Blackboard_01` modelindeki ahşap kenar ve girintili panel geometrisini kullanır; özgün atlas malzemesi paylaşılır.

Çerçeveler ve direkler mevcut Polygon Office paketinden gelir. Ağırlıklı taban, ay-yıldız başlık ve dökümlü kumaş kayıtlı 3B mesh'lerdir; runtime mesh üretimi veya kumaş simülasyonu bulunmaz. Portreler oranı bozulmadan çerçevenin içini tamamen doldurur; taşan kenarlar kayıtlı RectMask2D ile kırpılır. Boş kâğıt alanı veya isim katmanı yoktur. Bayrak dokuları kumaşın UV'lerine uygulanır.
