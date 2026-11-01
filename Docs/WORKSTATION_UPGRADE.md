# Çalışma alanı, Bakanlık faksı ve ses entegrasyonu

Dal: `feat/lead-coastal-cinematic`. Kaynaklar: Özgür `chore/audio-assets` / `10bb33f`; Esmanur `feature/Objects` / `4b6292a`; Faruk `origin/main` / `04a87a2` (ürün güncellemesi `a9fee0f`). Uzak sahne veya otomatik sahne üretim kodu alınmaz. Mevcut sahil, sinematik, oda, afişler ve portreler üzerinde hedefli düzenleme yapılır.

## Bakanlık denetimi

- Masada Esmanur'un faks ve siren modelleri bulunur. Baskı sırasında kâğıt çıkar; hata veya acil duyuru sırasında kırmızı siren, belge onaylanana kadar yanıp söner. Animasyon duraklatma menüsünde de tutarlı kalmak için ölçeklenmemiş zamanı kullanır.
- Hatalı karar ve hatalı ayrıştırma ayrı yanlış işlemlerdir. Tamamlanan ürün işlemindeki hatalar için zorunlu Bakanlık bildirimi açılır. Bildirim onaylanmadan yeni ürün başlamaz.
- **Her sabah sayaç sıfırlanır. Aynı gün üçüncü yanlışta vardiya kaybedilir.** Üçüncü hatanın bildirimi onaylanınca kayıp ekranı açılır. Eski/tıklanmış belge veya ürün callback'i yeni bir hata yazamaz.
- Normal **F analizi** korunur. Analiz, hata, acil kural ve gün sonu vardiya raporu fiziksel faksın baskı sunumunu kullanır. **T ile eski rapor açma** eylemi bulunmaz.
- Gün sonu ve kayıp sonuçları, gazete ile aynı kâğıt dokusunu kullanan resmî belge düzeninde gösterilir.

## Gazete referansı

Kullanıcının gönderdiği görsel, `NewspaperReference.jpg` adıyla projeye alınır. Caretta fotoğrafı bu texture içindeki yalnız fotoğraf dikdörtgenini gösteren kalıcı bir Sprite assetidir; örnek resimdeki tarih, balık kuralı ve anlamsız yazılar gösterilmez. Haber, tarih ve günlük kurallar kampanya verisinden gelir.

`NewspaperPaperV2.png`, imagegen ile referansın baskıları temizlenerek hazırlanan şeffaf kenarlı boş kâğıttır. Gazete, faks belgeleri ve vardiya raporu bu dokuyu kullanır. Büyük **GÜNLÜK HABERLER** başlığı, siyah beyaz caretta görseli ve çerçeveli **BUGÜNÜN GÖREVLERİ** alanı referanstaki düzeni izler. Üç günün bütün metinleri Editor'da ölçülür; runtime düzen/texture üretimi yapılmaz.

## Bant ve inceleme

32 metrelik modüler bant masanın sağından geçer; başlangıcı ön duvarın dışında, çıkışı arka duvarın dışındadır. Masa ile bant arasında fiziksel boşluk bulunur. Duvar ve pencere altlıklarında geçiş açıklıkları vardır. Esmanur'un gerçek bant parçaları kullanılır. Ürün hareket süresi mesafeden hesaplanır; uzun mesafelerde ürün aniden sıçramaz.

E veya ürün tıklaması ürünü incelemeye alır. Kamera ve oyuncu sabit kalır. Sürükleme veya yön tuşları yalnız ürünü döndürür; tekerlek yakınlaştırır. E veya sağ tık ürünü banttaki yerine geri bırakır. Kitapçık/analiz/karar işlemlerine geçişte inceleme pozu temizlenir.

## Ürün etiketleri ve Faruk'un verileri

31 üründe ön ve arka olmak üzere **62 baskı texture'ı** Editor'da hazırlanır. İçerik doğrudan `ProductDefinition.GetFormattedFrontLabel/GetFormattedBackLabel` verisidir; ad, tür beyanı, yasal tür, SKT, içerik, kaynak ve ambalaj bilgileri birlikte kalır. Gereksiz boş satırlar temizlenir; sözcük araları korunur.

Etiket meshleri ürünün baskı alanına yerleştirilir; düz paketlerde üst/alt yüzler, meyvelerde yapraklardan uzak gövde alanı kullanılır. Bozuk ürünlerin shader deformasyonu da Editor'daki yüzey hesabına katılır. Süt ambalajlarındaki eski tarih alanları maskelenir; geçerli SKT ana etikette okunur. Texture'lar Windows için BC7 ve mipmap ile hazırlanır. Havada duran ürün Canvas'ları ve HUD bilgi kartı kaldırılır. Oyun sırasında texture, mesh, malzeme veya UI üretilmez. Sanatçının kaynak mesh ve texture'ları korunur.

Faruk'un gerçek ürün adları, farklı SKT tarihleri ve sınır günleri, ayrıntılı analiz bulguları ve farklı ambalajlar alınır. **Yalnız toksik kirlilik POZİTİF olan ambalaj kontaminedir.** SKT/görünür bozulma/etiket/geri çağırma tek başına kırmızı kutu sebebi değildir. Kitapçık buna göre güncellenir.

## Esmanur'un yeni kutuları

Aktif dört kutu `Trashbin.fbx` içindeki yeni kâğıt, cam, plastik ve genel kutu meshleriyle değiştirilir. Eksik FBX malzeme GUID'leri doğru proje malzemelerine açıkça eşlenir. Oynanış renkleri mavi/kâğıt, yeşil/cam, sarı/plastik, kırmızı/kontamine olarak korunur. Etkileşim hedefleri ve ayrıştırma noktaları değişmez.

Binanın arkasındaki EditorOnly game kit, dört yeni kutuyu da içerir: **31 ürün + 16 önceki çevre parçası + 4 yeni kutu + 1 caretta = 52 kaynak gösterim**. Eski kutular yalnız bu inceleme alanında kaynak karşılaştırması için kalır; Game kameraları game kit'i çizmez.

## AudioManager

First sahnesindeki tek AudioManager ve tek AudioListener sahneler arasında yaşar. Beş önceden yerleştirilmiş kanal müzik, deniz ambiyansı, bant, analiz ve kısa efektleri yürütür. Menu/Game sahneleri yeni yönetici veya AudioListener oluşturmaz.

Özgür'ün iki müziği, deniz kaydı, analiz, bant, düğme, ürün alma, kitap alma/bırakma/sayfa, gazete, imha ve faks sesleri kullanılır. Müzikler ve deniz kaydı Streaming; kısa efektler DecompressOnLoad olarak içeri alınır. Menü ve oyun müziği ayrı atanır; iç mekânda deniz sesi azaltılır. Analiz süresi ses klibiyle eşlenir. Ses seviyesi mevcut ayarlardaki Master Volume üzerinden uygulanır; mola/belgeler bant ve analiz sesleriyle eşgüdümlüdür.

## Yeniden üretme ve doğrulama

Editor komutu: `GameUP32/Authoring/Upgrade workstation, labels and audio`. Yenilemeden önce elle yaptığınız sahne değişikliklerini kaydedin. Bu komut mevcut sahnelerin yalnız ilgili parçalarını yeniler; tam Game sahnesini tekrar kurmaz.

Aktarım öncesi sahne yedekleri ve üzerine yazılacak temiz kaynak dosyalarının SHA256 manifesti `Logs/WorkstationUpgrade` içindedir. Kod, gerçek sahne, inceleme sırasında kamera kilidi, faks alarmı, günlük üç hata sınırı, etiketler, yeni kutular ve ses bağlantıları Unity testleriyle doğrulanır. Güncel sonuç kökteki `VALIDATION.md` dosyasındadır. Windows build alınmaz.
