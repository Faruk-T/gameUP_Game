# Dördüncü kişi — üretim entegrasyonu

Unity **6000.3.13f1**, Windows x64, dal: `feat/lead-day-flow`. Faruk'un kuralları, Özgür'ün etkileşimi ve ürün sanatları bu dalda birleşir.

## Çalıştırma

`Assets/Scenes/First.unity` açılır ve Play'e basılır. Editor Play başlangıcı First'e sabittir. Build yalnızca **First → Menu → Game** içerir. Menu/Game, First'teki açık bağımlılıklar olmadan başlatılmaz.

- **First:** tek ApplicationRoot, kalıcı EventSystem, ayarlar, yükleme katmanı.
- **Menu:** Antalya arka planı, görev, ayarlar, rehber, COP31/ekip, çıkış.
- **Game:** iki gün, 12 ürün, gazete, inceleme, kitapçık, analiz/faks, ayrıştırma, acil kural, sonuçlar.

Fareyle bakış; **E/sol tık** inceleme; sürükleme döndürme; tekerlek yakınlık; **B** kitapçık; **F** analiz; **T** son rapor; **W** onay; **R** red. Ayrıştırmada dünya hedefine bakıp sol tıklanır. Esc belgeyi kapatır veya duraklatır. Gazete ve acil faks açık onay gerektirir.

## Kod sınırları

| Bileşen | Sorumluluk |
|---|---|
| ApplicationRoot | Sahne yükleme, kullanıcı ayarları ve imleç odağı |
| DayFlowSession | Gün/ürün sırası, işlem sınırları, kural snapshot'ı |
| DocumentQueue | Tek belge, kuyruk ve eski pencere token'larının reddi |
| GameSessionController | Akışın kayıtlı sahne portlarına bağlanması |
| ProductionInspectionBackend | Gün assetleri, Faruk'un ProductRuleEvaluator'ı, puan ve analiz |
| ProductPresentation | Kayıtlı ürün havuzu, bant/imha/ambalaj hareketi |
| WorkstationInteractionController | Tek oynanış girdi sahibi, bakış/raycast/inceleme |
| HUD / DocumentPresenter / ResultsPresenter | Kayıtlı uGUI alanlarının sunumu |
| PauseMenu / SettingsPanel | Gerçek duraklatma ve ortak ayarlar |
| FaxPresenter | Kaydedilmiş ses, ışık ve kâğıt animasyonu |

Contracts → Lead.Core / Rules → Gameplay; Interaction → Gameplay. Application uygulama katmanıdır. İkinci bir üretim InspectionService/InspectionRulesEngine akışı yoktur.

Ürünler çalışma sırasında Instantiate edilmez. On iki prefab örneği ve bir ambalaj nesnesi sahnede kayıtlıdır. Collider, etiket yüzleri, hedef noktaları, UI olayları ve bağımlılıklar Editor'de atanır. Eksik model için küp, eksik veri için sahte temiz rapor veya test kataloğu üretilmez. Katalog bir kez indekslenir. Editor yazarları, test kataloğu ve otomasyon oyuncu build'ine girmez.

PigeonSimulator5'ten hafif menü/odak yaklaşımı, düğme görseli, Kurale fontu ve Editor görüntü yakalama yaklaşımı kullanıldı. Kayıt/NPC/quest framework'ü taşınmadı.

## Kritik akışlar

Onayda bant hareketi; redde hem gıda imhası hem ambalaj ayrıştırma hareketi bitmeden puan kaydedilmez. İkinci gün üçüncü ürün tamamen bittiğinde acil faks açılır. Onaydan sonra B bölgesi **dip** balığı yasağı sonraki ürüne uygulanır; önceki kararlar değişmez. D2-04 temiz analizle bile yasaktır; D2-05 yüzey balığı bu yasağın dışındadır.

Analiz RunId/ProductId taşır. Restart eski coroutine ve callback'leri geçersiz kılar. Belge kararları kilitler; pause hem kararları hem zamanla ilerleyen ürün/analiz/faks hareketini durdurur. Sıfır ayrıştırma varsa oran “—” görünür.

## Sanat ve içerik

[Kaynaklar, import ayarları ve tam promptlar](Assets/Art/Menu/ART_DIRECTION.md). COP31 tarihi menüde 9–20 Kasım 2026'dır. Oynanıştaki 1–2 Kasım, kurmaca hazırlık günleridir. Caretta carettalar, deniz kirliliği, sıcaklık ve gıda güvenliği gazetelere işlenmiştir.

Süt ürünleri ekip modellerini kullanır. Balık için açıkça atanmış basit balık/tepsi prefabı Editor'de hazırlanmıştır; yeni model, prefabın model çocuğuyla değiştirilebilir.

## Editor komutları

`GameUP32/Authoring/Rebuild First, Menu and Game` sahneleri ve ürün prefablarını yeniden yazar. Otomatik import/bootstrap değildir. Elle sahne düzenlediysen yeniden üretmeden önce değişikliklerini koru.

`GameUP32/Build/Windows` üretim çıktısı: `Builds/Windows/AntalyaDenetimMasasi.exe`.

```powershell
.\Tools\Lead\Verify.ps1 -UnityEditor 'D:\Programs\UnityInstalls\6000.3.13f1\Editor\Unity.exe' -Visuals -Build
```

`-Author` isteğe bağlıdır. Rapor: `Logs/production-tests.xml`; grafik aygıtıyla çalıştırılan görseller: `Logs/Visuals`.

Testler çekirdek akışı, Faruk'un kabul senaryolarını, kayıtlı referansları ve First'ten açılan üç gerçek kampanyayı kapsar: doğru oynayış **12/12 karar, 6/6 ayrıştırma, 132 puan**; tümüne onay/red **%50 doğrulukla başarısız**. Pause, ayarların sahneler arası korunması, eski callback, restart ve menüye dönüş de kontrol edilir. Görüntü yakalama yalnızca Editor/test assembly'sindedir.

Son ölçülen sonuçlar ve önizlemeler: [VALIDATION.md](VALIDATION.md).

## Kapsam sınırı

Tartı/cetvel, yüzde ölçümü, üçüncü gün ve ek gıda grupları P1 veri örnekleridir; iki günlük kampanyaya bağlanmaz. Ayrı cutscene sistemi eklenmedi; kısa giriş sözü COP31/ekip ekranındadır. Ekip dışından ilk oynayış ve ayrı Windows PC'de donanım kontrolü teslim öncesi yapılmalıdır.
