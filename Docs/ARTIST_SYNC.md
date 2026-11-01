# Sanatçı modelleri — kaynak eşleştirmesi

1 Kasım 2026'da `origin/feature/Objects` fetch ile kontrol edildi. Uzak dalın son commit'i `0106ab2` (çevre objeleri); ürünler `0dc833b` commit'indedir. Model, prefab ve doku dosyaları mevcut projede zaten eksiksizdir. Yeniden kopyalama veya GUID değiştirme yapılmaz. Yerel `main` birleştirilmez.

| Kaynak | Aktif kullanım |
|---|---|
| 31 ürün prefabı / GameUp32-Objects.fbx | Üç günlük kampanyadaki 31 ürün; aynı kaynaklardan 62 sağlam/bozuk varyant |
| Environment.fbx / desk | Denetim masası |
| donen alet / donen alet yanları | Ürün bandı |
| xray / xray paper / xray signal | Korunan normal F analiz cihazı |
| closedBook / openBook | Kitapçığın fiziksel durumları |
| Closed_NewsPaper | Masadaki katlı gazete |
| paper | Masadaki boş denetim formu |
| Dört trashbin mesh'i | Oyuncunun arkasındaki ayrıştırma kutuları |
| caretta caretta.fbx | Beş animasyonlu kıyı aktörü |
| fax / fax signal | Kaynak assetleri projede korunur; T / Son rapor cihazıyla birlikte aktif sahneden kaldırılır |

Kaynak çevre parçaları: **16/16 projede, 14 aktif, 2 istek gereği kullanım dışı**. Güncel mesh denetimi [ENVIRONMENT_INVENTORY.md](ENVIRONMENT_INVENTORY.md) dosyasındadır. Ürün modeli denetimi [MODEL_INVENTORY.md](MODEL_INVENTORY.md) içindedir. Sanatçının eşdeğeri bulunan masa, bant, kitap, gazete, analiz ve kutu modelleri onun mesh'lerini kullanır; ürünler için geçici primitive model üretilmez.

Binanın arkasındaki [Esmanur Game Kit](ARTIST_GAME_KIT.md), 31 ürün + 16 çevre parçası + carettayı birlikte gösterir. Sergi EditorOnly'dir; bu tablodaki aktif oyun sayımlarına dahil edilmez.

Normal F analizi, gizli kirlilik bulguları, sonuç belgesi ve analiz önbelleği korunur. T kısayolu, tekrar rapor etkileşimi, ayrı faks cihazı, fiziksel çıktı/ışık animasyonu ve `FaxPresenter` kaldırılır. Bakanlık acil faksı zorunlu belge olarak ve tek seferlik ses bildirimiyle gelmeye devam eder.
