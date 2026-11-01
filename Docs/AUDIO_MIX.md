# Sinematik ve tesis ses düzeni

Unity `6000.3.13f1`, dal `feat/lead-coastal-cinematic`. Tek AudioManager ve AudioListener, First sahnesinde bulunur. Menu ve Game ek ses yöneticisi oluşturmaz.

## Seviyeler ve kanallar

| Kanal | Davranış |
|---|---|
| Music | Menü 0.30; oyun 0.20; belgede/mola sırasında 0.08; dublajlı açılışta 0.06 |
| Ocean | Mevcut `ocean_wave_background.ogg`; menüde 0.12, kıyıda 0.22, çalışma alanında 0.20; loop ve Streaming |
| Conveyor | Mevcut bant sesi, yalnız bant etkin ve dünya girdisi açıkken |
| Analyzer | Seviye 0.40 → **0.95**, yaklaşık **+7.51 dB**; tek sefer; analiz/mola durumuna göre durur ve sürer |
| Effects | Düğme, kitap, gazete, ürün, imha ve uyarı; mevcut dokuz cue bağlantısı korunur |
| Printer | Ayrı kanal; loop kapalı; yeni baskı önceki baskı sesinin yerini alır, sesler üst üste birikmez |
| OpeningNarration | Kullanıcının `sinema_senkron_ses.mp3` kaydı; 0.85, tek sefer, Streaming |

Bütün seviyeler mevcut Master Volume ayarından geçer. Okyanusun çalışma alanı seviyesi önceki 0.065 değerinden 0.20'ye çıkarılmıştır. Koridora giriş ile sinematik sonu arasında dış mekân ambiyansı çalışma alanı seviyesine geçer.

## Dublaj senkronu

Kaydın süresi **86.064 saniye**, sinematik **90 saniye**. Ses 0. saniyede başlar ve klip bütünüyle oynar; esnetilmez, tekrarlanmaz. Kaynak dosya değişmeden `Assets/Audio/Cinematic/sinema_senkron_ses.mp3` içinde saklanır. SHA256: `F98622CE945D61DC690935456673FA43DB6ACA9EF64DB258901855449C17CABC`.

IntroCinematic açılış sesini başlatır; kamera, aktörler ve altyazıların kullandığı aynı zamanla AudioManager'a örnek konumu verir. 120 ms üzerindeki ses saati farkı düzeltilir. Pencere odağı kaybolunca ses ve görüntü birlikte bekler; odak dönüşünün ilk delta zamanı atılır. Klibin bitmesi onu tekrar başlatmaz. Sinematik tamamlanınca veya sahne kapanınca dublaj durur; çalışma alanı/menü miksine geçilir. Açılışın atlanamaz olması korunur.

## Yazıcının tek baskı davranışı

`fax_machine.mp3` **3.1608 saniyedir**. Mekanik ses özel Printer kanalında bir defa oynar. Kâğıt çıkışı aynı süreyle eşlenir. Bakanlık alarmında ışığın yanıp sönmesi belge onayına kadar sürebilir; mekanik yazıcı sesi döngüye girmez.

Yeni bir baskı, öncekinin sesini durdurup tek işi başlatır. Aynı analiz raporunu tekrar açmak yeni baskı veya ses üretmez. Restart/sahne kapanışı, yazıcı kanalını temizler; kitap veya düğme seslerini kesmez.

## Tekrar uygulama ve test

Miks/ses kaynaklarını kaydetmek için: **GameUP32 → Authoring → Apply cinematic and workstation audio mix**. Bu işlem yalnız First sahnesindeki ses kurulumunu ve audio importer ayarlarını günceller; Game sahnesini yeniden üretmez.

Son tam test koşusu: **100/100 geçti**, 0 başarısız, 0 atlanan; **152.09 saniye**. XML ve log: `Logs/AudioPolish/tests.xml`, `tests.log`. Windows player build alınmadı.

- Gerçek 90 saniyelik açılışta dublajın çalması ve kamera saatiyle farkının 250 ms altında kalması.
- Dublajı bekletme, örnek konumundan sürdürme, bitişte tekrar başlamama, menüye dönüşte durma.
- Okyanus kanalının çalması ve çalışma alanı seviyesi; analiz sesinin mola/sürdürme davranışı.
- Tekrarlanan yazıcı isteğinin sonlanması; yalnız yazıcıyı durdururken diğer efektlerin sürmesi.
- Gerçek analiz/faks penceresi açık kalırken yazıcının susması ve önbellekteki raporu tekrar açarken yeniden başlamaması.
- Önceki ürün/puan, bitişik kutu, sahne, sinematik, kitap ve restart kontrolleri.
