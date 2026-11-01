# Sanatçı modelleri — kaynak eşleştirmesi

1 Kasım 2026'da `origin/feature/Objects` fetch ile kontrol edildi. Güncel revizyon `4b6292a`; dört yeni kutu `Trashbin.fbx` içindedir. Önceki çevre kaynakları `0106ab2`, ürünler `0dc833b` revizyonundandır. Yeni FBX'in eksik malzeme/texture bağlantıları doğru dosyalara eşlendi. Yerel `main` birleştirilmez. [Güncel çalışma alanı entegrasyonu](WORKSTATION_UPGRADE.md).

| Kaynak | Aktif kullanım |
|---|---|
| 31 ürün prefabı / GameUp32-Objects.fbx | Üç günlük kampanyadaki 31 ürün; aynı kaynaklardan 62 sağlam/bozuk varyant |
| Environment.fbx / desk | Denetim masası |
| donen alet / donen alet yanları | Ürün bandı |
| xray / xray paper / xray signal | Korunan normal F analiz cihazı |
| closedBook / openBook | Kitapçığın fiziksel durumları |
| Closed_NewsPaper | Masadaki katlı gazete |
| paper | Faksın baskı kâğıdı |
| Trashbin.fbx içindeki dört yeni mesh | Oyuncunun arkasındaki ayrıştırma kutuları; eski meshler EditorOnly kit'te korunur |
| caretta caretta.fbx | Beş animasyonlu kıyı aktörü |
| fax / fax signal | Aktif Bakanlık faksı ve kırmızı alarm; T / eski raporu açma hedefi bulunmaz |

Kaynak çevre parçaları: **20/20 projede, 16 aktif; eski dört kutu yeni revizyonla değiştirilmiştir**. Güncel mesh denetimi [ENVIRONMENT_INVENTORY.md](ENVIRONMENT_INVENTORY.md) dosyasındadır. Ürün modeli denetimi [MODEL_INVENTORY.md](MODEL_INVENTORY.md) içindedir. Sanatçının eşdeğeri bulunan masa, bant, kitap, gazete, analiz ve kutu modelleri onun mesh'lerini kullanır; ürünler için geçici primitive model üretilmez.

Binanın arkasındaki [Esmanur Game Kit](ARTIST_GAME_KIT.md), 31 ürün + 16 çevre parçası + carettayı birlikte gösterir. Sergi EditorOnly'dir; bu tablodaki aktif oyun sayımlarına dahil edilmez.

Normal F analizi, gizli kirlilik bulguları, sonuç belgesi ve analiz önbelleği korunur. T kısayolu, tekrar rapor etkileşimi, ayrı faks cihazı, fiziksel çıktı/ışık animasyonu ve `FaxPresenter` kaldırılır. Bakanlık acil faksı zorunlu belge olarak ve tek seferlik ses bildirimiyle gelmeye devam eder.
