# Teknik lider uygulaması ve entegrasyon

Bu dal, IMPLEMENTATION.md içindeki dördüncü kişinin gün akışı, belge, öğretici, faks ve teslim altyapısını uygular. Başlangıç sahnesi korunur; çalışma sahnesi `Assets/Scenes/LeadPrototype.unity` dosyasıdır.

## Çalıştırma

1. Projeyi Unity **6000.3.13f1** ile aç.
2. `GameUP > Lead > Open or create integration scene` menüsünü seç.
3. Play'e bas. Sabah gazetesindeki **Göreve başla** düğmesiyle başla.
4. `GameUP > Lead > Build Windows prototype` menüsü ayrı Windows development build'i üretir. Mevcut ana build sahne listesi değiştirilmez.

Tuşlar: **E** etiket, **W** onay, **R** red, **F** analiz, **B** kitapçık. Sağ fare sürükleme çevreye bakar; incelemeden sonra dünya üzerindeki sol fare sürükleme ürünü döndürür. Prototipte fare, düğmeler ve ayrıştırma için görünür kalır. Normal belge Escape ile kapanır; sabah ve acil faks açık onay ister.

İki gün ve 12 ürün vardır. İlk gün SKT/beyan, ikinci gün analiz ve kaynak yasağı kullanılır. İkinci gün üçüncü ürünün ayrıştırması da tamamlandıktan sonra acil faks gelir; dördüncü ürün yeni kural snapshot'ıyla başlar.

## Kod sınırları

| Plan işi | Bu daldaki durum |
| --- | --- |
| L01 | Ortak sürüm doğrulandı; ayrı Windows development build'i alındı |
| L02 | Gün/ürün sırası, işlem sınırları ve yeniden başlatma uygulandı |
| L03 | Ortak belge arayüzü, sıra, onay ve girdi kilidi uygulandı |
| L04 | İki gazete, caretta haberi ve ilk ürünlerin yönlendirmeleri eklendi; ekip dışından ilk oynayış bekliyor |
| L05 | Kimlikli analiz raporu, faks sesi/ışığı/kâğıdı uygulandı |
| L06 | Üçüncü balığın ayrıştırmasından sonra acil faks ve yeni snapshot uygulandı |
| L07 | Gün/final sonuçları, başarı koşulu ve tekrar oyna uygulandı |
| L08 | Geçici portlarla çalışan sahne hazır; gerçek ekip parçaları bağlanacak |
| L09 | Yerel Windows oyuncu testi geçti; nihai sanat ve ayrı bilgisayarda teslim kontrolü bekliyor |

| Dizin / bileşen | İş |
| --- | --- |
| `Assets/Scripts/Contracts` | Unity bağımlılığı olmayan ortak modeller ve üç bağlantı sözleşmesi |
| `Assets/Scripts/Lead/Core/DayFlowSession.cs` | Gün/ürün sırası, snapshot ve işlem sınırları |
| `Assets/Scripts/Lead/Core/DocumentQueue.cs` | Belge sırası ve eski pencere callback'lerine karşı oturum token'ı |
| `Assets/Scripts/Lead/Runtime/LeadRuntime.cs` | Sahne bağımlılıkları, port bağlantıları, belge/akış entegrasyonu |
| `Assets/Scripts/Lead/Runtime/LeadView.cs` | Seri hâle getirilmiş uGUI referansları ve ekran sunumu |
| `Assets/Scripts/Lead/Runtime/FaxPresenter.cs` | Kâğıt, ışık ve üretilmiş kısa faks sesi |
| `Assets/Scripts/Lead/Prototype` | Değiştirilebilir örnek veri, cevap anahtarı ve masa sunumu |
| `Assets/Editor/Lead` | Ayrı sahne oluşturma ve Windows build komutları |
| `Assets/Tests/Editor/Lead` | Akış, belge, snapshot, eski callback ve çift kayıt kontrolleri |

PigeonSimulator5'teki akış sınırı, modal oturum kimliği ve odak yönetimi yaklaşımlarından yararlanıldı. Onun kayıt, envanter, görev veya arayüz framework bağımlılıkları taşınmadı.

## Faruk'un bağlayacağı sistem

1. Üretim veri kaynağı `ICampaignSource.CreateCampaign()` ile `CampaignInfo` döndürür. Bu, ScriptableObject'lerden okunabilir.
2. Kural/analiz bileşeni `IInspectionBackend` uygular.
3. `LeadRuntime` Inspector'ında `Campaign Source` ve `Inspection Backend` alanları bu MonoBehaviour bileşenlerine atanır.

`Evaluate` yalnızca değerlendirme döndürür; puan ve günlük kayıt `CommitOutcome` içinde tutulur. `CommitOutcome` ürün başına idempotent olmalıdır. `GetDaySummary` ve `GetRunSummary` kaydedilmiş sonuçlardan gelir.

Analiz callback'i Unity ana thread'inde çağrılır. Rapor, değerlendirme ve sonuç; verilen `ProductKey` ile aynı `RunId`/`ProductId` değerini taşımalıdır. `RuleSnapshot` ürün başladıktan sonra değişmez. Acil ek, `R_SOURCE_BAN` kimliğiyle sonraki snapshot'ta görünür.

`PrototypeInspectionBackend`, IMPLEMENTATION.md'deki 12 senaryonun cevap anahtarıdır; genel gıda kurallarını değerlendiren üretim sistemi değildir. Faruk'un gerçek sistemi bağlandığında bu bileşen değiştirilir. Ürün kimliklerine göre yazılmış örnek cevaplar üretim kural koduna taşınmaz.

## Özgür'ün bağlayacağı sistem

1. Masa/oyuncu bileşeni `IProductPresentation` uygular.
2. Inspector'daki `Product Presentation` alanına atanır.
3. `Present` callback'i ürün masaya ulaştığında, `Route` callback'i onay/imha hareketi bitince, `Sort` callback'i ambalaj seçilen hedefe ulaştığında çağrılır.
4. Oyuncu komutları `LeadRuntime.Approve`, `Reject`, `Analyze`, `SelectWaste` yöntemlerine bağlanabilir; kimlikli ileri kullanım `runtime.Flow` üzerindeki yöntemlerdir.
5. Gerçek inceleme açıldığında `NotifyProductInspected(ProductKey)` çağrılır. Prototipin etiket düğmesi yerine gerçek inceleme kullanılabilir.

Karar girişi için `Flow.CanDecide`, ayrıştırma için `Flow.CanSort`, genel modal/gün sınırı için `WorldInputAllowed` kullanılır. Belge kapandı diye girdi koşulsuz açılmaz. Gerçek FPS kontrolünde `Lock Cursor During Gameplay` seçeneği ve kamera politikası birlikte ayarlanır.

`PrototypeWorkstation`, geçici küp ve sabit noktalarla çalışan porttur. Yerine gerçek etkileşim prefabı bağlandığında bu bileşen ve onun W/R/E/F kısayolları kaldırılır; iki giriş sürücüsü birden çalıştırılmaz.

## Tekrarlanabilir doğrulama

Unity'nin projeyi aynı anda açık tutan başka bir örneği olmadan, PowerShell'de:

```powershell
.\Tools\Lead\Verify.ps1 -UnityEditor 'D:\Programs\UnityInstalls\6000.3.13f1\Editor\Unity.exe'
.\Tools\Lead\Verify.ps1 -UnityEditor 'D:\Programs\UnityInstalls\6000.3.13f1\Editor\Unity.exe' -Build -Smoke
```

İlk komut sahneyi hazırlar ve EditMode testlerini çalıştırır. İkincisi ayrıca Windows development player oluşturur ve oyuncuda üç tam kampanyayı gerçek uGUI düğme bağlantıları üzerinden işler:

- Doğru oynayış: 12/12 karar, 6/6 ayrıştırma, 132 puan.
- Her şeyi reddet: 6/12 karar ve başarısız sonuç.
- Her şeyi onayla: 6/12 karar ve başarısız sonuç.
- Açık kitapçık sırasında gelen analiz raporu sırada bekler.
- Acil faks doğru sınırda gelir ve dördüncü ürüne yeni snapshot uygulanır.
- Yeniden başlatma kimlik, kural, rapor ve puanı temizler.

Smoke yalnızca `--lead-smoke-test` argümanıyla Editor/development player içinde çalışır; normal Play veya normal çalıştırma otomatik karar vermez. Ekran görüntüleri ve sonuç JSON'u `Logs/Smoke-*` altında, derleme `Builds/LeadPrototype` altındadır. Bu klasörler gitignore kapsamındadır.

## Kalan ekip bağımlılıkları

- Gerçek kural/ürün ScriptableObject'leri ve analiz servisi: Faruk.
- Gerçek nesne inceleme, etkileşim ve atık prefabları: Özgür.
- Nihai oda, ürünler, etiket görselleri ve caretta görseli: 3D tasarımcı.
- Son sanat ve sistemlerle başka ekip üyesinde ilk oynayış ve ayrı bilgisayarda teslim kontrolü.

Örnek portlarla geçen testler, bu üretim parçalarının test edildiği anlamına gelmez. İlk oynanabilir sahne ve build, kalan parçaların aynı sözleşmeye sırayla bağlanması içindir.

## Doğrulama kaydı — 31 Ekim 2026

- Unity 6000.3.13f1 ile gerçek C# derlemesi tamamlandı.
- EditMode: **29/29 test geçti**. Rapor yerelde `Logs/lead-editmode-results.xml`.
- Windows x64 development build'i başarılı. Yerel çalıştırılabilir dosya: `Builds/LeadPrototype/GameUP32.exe`.
- Windows oyuncusunda gerçek uGUI düğme bağlantıları tetiklenerek üç tam kampanya / **36 ürün** işlendi: doğru oynayış 12/12, ayrıştırma 6/6, puan 132; tümünü reddet ve tümünü onayla yolları %50 ile başarısız.
- Açık kitapçık sırasında raporun beklemesi, acil kural sınırı ve temiz yeniden başlatma oyuncu testinde geçti.
- Gerçek kamera ve arayüz render hedefinden alınan gazete, masa, analiz, acil faks ve final görüntüleri gözle incelendi. Gazete **1920×1080** ve **1280×720** çözünürlüklerde okunabilir.
- Son görsel oyuncu testi ve PNG'ler yerelde `Logs/Smoke-Visual` altında. Boş kareler, yakalama kodunda hata olarak kabul edilir.
- İlk Windows build'inin normalize ettiği URP/Player ayarları aynı Editor/paket sürümleriyle kaydedildi; mevcut `SampleScene` ve ana build sahne seçimi korunuyor.
