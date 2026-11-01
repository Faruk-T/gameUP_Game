# Kitap sistemi — geliştirici ve AI rehberi

Bu belge, bu projedeki Book-Page Curl paketinin **mevcut özelleştirilmiş uygulamasını** açıklar. Paketin orijinal sürümüyle bu projedeki günlük içerik, Input System ve açma-kapama davranışları aynı değildir. Değişiklik yapmadan önce bu belgeyle birlikte ilgili C# dosyasını ve kullanılan prefabın Inspector değerlerini kontrol edin.

## 1. Sistemin amacı ve temel kurallar

Kitap, oyun içinde B tuşuyla açılan, alt köşelerinden tutularak sayfaları çevrilen bir Unity UI arayüzüdür. Gün numarasına göre iki vardiya sayfasının sprite'ı değişir. Her açılışta kitap ilk sayfaya döner ve gün bilgisi manager entegrasyon noktasından tekrar alınır.

Temel kurallar:

- Sayfa içeriği `Sprite` referanslarıdır. Görüntüleme Canvas altındaki UI `Image` bileşenleriyle yapılır.
- Kıvrılma, 3D mesh deformasyonu yerine UI resimlerinin döndürülmesi, taşınması ve maskelenmesiyle oluşturulur.
- `currentDay` oyun günüdür; `currentPage` açık kitabın sağ sayfasının sıfır tabanlı indeksidir. Birbirinden bağımsızdırlar.
- Güncelleme yalnızca `bookPages[1]` ve `bookPages[2]` alanlarını değiştirir.
- Gün değişikliği tek başına açık sayfayı değiştirmez. Kitabın kapanması veya açılması sayfayı sıfırlar.
- Klavye kontrolü, kapatılan Canvas'ın üzerinde veya altında bulunmamalıdır. Kitap kapalıyken de aktif kalmalıdır.
- `OnEnable` ve `OnDisable` içinde parent/sibling/alt nesne aktivasyonu değiştirilmez. Görsel sıfırlama `LateUpdate` aşamasına ertelenir.
- Gün manager'ı henüz gerçek bir sınıfa bağlanmamıştır; `RefreshDayFromManager()` içindeki yorumlar entegrasyon yerini gösterir.

## 2. Dosya ve asset haritası

Aşağıdaki yollar proje köküne göredir:

| Yol | Sorumluluk |
| --- | --- |
| `Assets/Book-Page Curl/scripts/Book.cs` | Sayfa geometrisi, sürükleme, sprite güncellemesi, günlük içerik, açılış/kapanış sıfırlaması |
| `Assets/Book-Page Curl/scripts/BookToggle.cs` | B tuşunu okuyarak bağlı Canvas nesnesini açar/kapatır |
| `Assets/Book-Page Curl/scripts/AutoFlip.cs` | Orijinal paketin kodla/otomatik sayfa çevirme bileşeni |
| `Assets/Book-Page Curl/Example/prefabs/Book.prefab` | Maskeleri, sayfa Image'larını, hotspotları ve Book bileşenini içeren temel kitap |
| `Assets/Book-Page Curl/Example/prefabs/Book Game Canvas.prefab` | Oyun için sayfa ve günlük sprite eşleştirmeleri ayarlanmış Canvas; temel Book prefabını içerir |
| `Assets/Book-Page Curl/Example/prefabs/Book Toggle Controller.prefab` | Aktif kalan BookToggle kökü ve altındaki kitap Canvas'ını birlikte taşır |
| `Assets/Book-Page Curl/Example/scenes/Example_1_Basic Book.unity` | Controller prefabının kullanıldığı temel örnek/test sahnesi |
| `Assets/Book-Page Curl/Example/scenes/Example_InGameBook.unity` | Kitap Canvas prefabının kullanıldığı diğer sahne; B kontrolünü kullanmak için ayrıca controller bağlantısını doğrulayın |
| `Assets/Book-Page Curl/Example/scenes/Example_2_Controled Flipping.unity` | Orijinal kontrollü sayfa çevirme örneği |
| `Assets/Book-Page Curl/Example/scenes/Example_3_WorldSpace.unity` | World Space Canvas örneği |
| `Assets/Book-Page Curl/Kitap Sprite/` | Öğretici, vardiya ve kural sayfalarının sprite assetleri |
| `Assets/Editor/BookLifecycleRegression.cs` | Play Mode içinde menüden çalıştırılan açma-kapama regresyon testleri |

`Kitap Canvas`, sahnedeki nesne adı olarak hâlâ kullanılabilir. Güncel Canvas prefab dosyasının adı `Book Game Canvas.prefab` olduğundan yalnızca eski dosya adına göre arama yapmayın. Prefab bağlantıları dosya adından ziyade GUID üzerinden korunur.

## 3. Hiyerarşi ve Inspector bağlantıları

Önerilen kullanımın basitleştirilmiş yapısı:

```text
Book Toggle Controller                <- aktif kalır; BookToggle burada
└── Kitap Canvas                       <- bookCanvas referansı; SetActive ile açılır/kapanır
    ├── BookBackground
    └── Book                           <- Book bileşeni ve BookPanel
        ├── LeftNext / RightNext       <- dinlenme hâlinde görünen sayfalar
        ├── Left / Right               <- çevrilirken kullanılan resimler
        ├── TurnPageClip               <- ClippingPlane referansı
        ├── NextPageClip
        ├── LeftHotSpot / RightHotSpot
        └── gölgeler ve ilgili alt nesneler

EventSystem                            <- InputSystemUIInputModule
Main Camera
```

Bu çizim rol dağılımını gösterir; gölgelerin parent'ı ve sayfaların sıralaması animasyon sırasında değişebilir.

Book Inspector alanları:

| Alan | Anlamı |
| --- | --- |
| `canvas` | Kitabın bulunduğu Canvas; boşsa `Start` içinde parent'lardan aranır |
| `BookPanel` | Kitabın ölçü ve yerel koordinat referansı |
| `background` | İndekste içerik bulunmadığında gösterilen sprite |
| `bookPages` | Sıralı sayfa sprite dizisi |
| `currentDay` | Başlangıç/yedek gün; Inspector'da seri hâle getirilir |
| `dailyPages` | Her gün için iki vardiya sprite'ının eşleştirmesi |
| `currentPage` | Sağ tarafta görünen sprite'ın indeksi; normal akışta 0, 2, 4... |
| `interactable` | Kullanıcı sürüklemesini etkinleştirir; kodla çevirme API'sini kapatmaz |
| `enableShadowEffect` | Sürükleme başlatılırken gölgenin açılmasını belirler |
| `Left`, `Right`, `LeftNext`, `RightNext` | Sayfa Image referansları |
| `ClippingPlane`, `NextPageClip` | Kıvrılan yüzlerin görünür bölümünü belirleyen maskeli Image'lar |
| `Shadow`, `ShadowLTR` | Sağdan sola ve soldan sağa çevirme gölgeleri |
| `OnFlip` | Başarılı çevirme tamamlandığında tetiklenen UnityEvent |

`BookToggle.Awake()`, `bookCanvas` atanmamışsa veya controller Canvas'ın kendisi/alt nesnesiyse hata verir ve bileşeni kapatır. Controller'ın Canvas'ın **parent'ı** olması geçerlidir; kapatılan nesnenin **child'ı** olması geçerli değildir.

## 4. Sayfa indeksleri ve mevcut içerik

Dinlenme hâlinde `UpdateSprites()` şu eşleştirmeyi yapar:

```text
Sol sayfa  = bookPages[currentPage - 1]  (geçerli indeks varsa)
Sağ sayfa  = bookPages[currentPage]      (geçerli indeks varsa)
Diğer durumlarda background
```

Oyun Canvas'ının mevcut dört sayfası:

| Dizi indeksi | İçerik | Günle değişir mi? |
| --- | --- | --- |
| 0 | `Ogretici` | Hayır |
| 1 | Günün ilk vardiya sprite'ı | Evet |
| 2 | Günün ikinci vardiya sprite'ı | Evet |
| 3 | `YakmaKurali` | Hayır |

Buna göre açık kitap görünümü:

| `currentPage` | Sol | Sağ |
| --- | --- | --- |
| 0 | background | Öğretici |
| 2 | Günün vardiya 1 sayfası | Günün vardiya 2 sayfası |
| 4 | Yakma kuralı | background |

Bir yaprak çevrilince `currentPage` ileri yönde 2 artar, geri yönde 2 azalır. Oyun günü ilerlediğinde `currentPage` artırılmaz; iki kavramı aynı sayaç olarak kullanmayın.

Sayfalara yazı eklemek için mevcut uygulamada metni sayfa resmine işlemeniz gerekir. TextMeshPro veya başka UI nesneleri otomatik olarak `bookPages` içeriğine katılmaz; bunun için ayrıca sayfa içerik sistemi geliştirilmelidir.

## 5. Modern input akışı

Bu proje Unity `6000.3.13f1` ve Input System `1.19.0` kullanır. `activeInputHandler: 1`, yeni Input System'in etkin olduğunu belirtir.

UI tarafında `EventSystem`, `InputSystemUIInputModule` ve Canvas üzerindeki `GraphicRaycaster` gereklidir. Örnek sahnelerde eski `StandaloneInputModule`/`TouchInputModule` yerine yeni UI modülü kullanılır. Modülde action atanmamışsa Input System'in varsayılan UI action ataması devreye girer. B tuşu için ayrı bir Input Action asseti gerekmez; `BookToggle`, `Keyboard.current.bKey.wasPressedThisFrame` okur.

Prefab EventTrigger bağlantıları:

| Nesne | Olay | Dinamik callback |
| --- | --- | --- |
| LeftHotSpot | Begin Drag | `OnBeginDragLeftPage(BaseEventData)` |
| RightHotSpot | Begin Drag | `OnBeginDragRightPage(BaseEventData)` |
| Her iki hotspot | End Drag | `OnEndDragPage(BaseEventData)` |

Callback'ler **dinamik olay parametresiyle** bağlanmalıdır. YAML karşılığı `m_Mode: 0` değeridir. Parametresiz statik çağrı seçilirse sürüklemeyi başlatan pointer bilgisi iletilmez.

Akış:

1. Begin Drag, olay verisini `PointerEventData` olarak alır.
2. `BeginPointerDrag`, etkileşim durumunu, tuşu ve sayfa sınırını kontrol eder.
3. Başlatan pointer `dragPointer` olarak saklanır. Başka parmak aynı sürüklemeyi devralamaz.
4. `UpdateBook`, bu pointer'ın ekran konumunu kitap koordinatlarına dönüştürüp takip noktasını yumuşatır.
5. End Drag, pointer ID'sini doğrular; bırakma anındaki konumu kullanıp `ReleasePage()` çağırır.

`OnMouseDragRightPage()`, `OnMouseDragLeftPage()` ve `OnMouseRelease()` eski özel UnityEvent bağlantıları için korunmuştur. Konum okuması artık `Pointer.current` üzerinden yapılır. Yeni bağlantılarda pointer verili callback'leri tercih edin.

`transformPoint()`; Overlay için kamerasız, Camera/World Space için olay kamerası, Canvas kamerası veya `Camera.main` kullanarak `RectTransformUtility.ScreenPointToLocalPointInRectangle` çağırır.

## 6. Kıvrılma ve çevirme kararı

Geometri `BookPanel.rect` ölçülerinden hesaplanır:

- `sb`: kitap sırtının alt noktası.
- `st`: kitap sırtının üst noktası.
- `ebr` / `ebl`: sağ/sol alt köşe.
- `f`: takip hedefi.
- `c`: geometrik sınırlar uygulandıktan sonraki sayfa köşesi.
- `radius1`: sayfa genişliği; alt sırt etrafındaki hareket sınırı.
- `radius2`: sayfa köşegen uzunluğu; üst sırt etrafındaki hareket sınırı.

`Calc_C_Position()` hedefi bu iki yarıçapla sınırlar. `CalcClipAngle()` katlanma çizgisinin konumunu/açısını hesaplar. `UpdateBookRTLToPoint()` veya `UpdateBookLTRToPoint()` resimleri ve maskeleri bu sonuca göre yeniden parent'lar, taşır ve döndürür.

`ReleasePage()` köşenin iki alt kenara uzaklığını karşılaştırır. Başlanan kenara daha yakınsa `TweenBack()`, diğer tarafa yeterince geçmişse `TweenForward()` çalışır. Son hareket `TweenTo()` coroutine'iyle yaklaşık 0,15 saniyede yapılır. `WaitForSeconds` ve `Time.deltaTime` kullanıldığından süreler oyun zaman ölçeğinden etkilenir.

Başarılı çevirmenin sonunda `Flip()` indeksi değiştirir, görünür sprite'ları yeniler ve `OnFlip` çağırır. Geri kapanma, gün değiştirme ve `ResetBook()` bu olayı tetiklemez.

## 7. Günlük vardiya içeriği

Her `Book.DailyPages` kaydı `day`, `element1` ve `element2` içerir. Gün numaraları 1'den başlar; dizideki kaydın sırası yerine `day` alanı eşleştirilir.

Mevcut prefab eşleştirmeleri:

| Gün | `bookPages[1]` | `bookPages[2]` |
| --- | --- | --- |
| 1 | `vardiya11` | `vardiya12` |
| 2 | `Vardiya21` | `Vardiya22` |
| 3 | `Vardiya31` | `Vardiya32` |

Dosya adından sprite arama veya otomatik gün algılama yapılmaz. Eşleştirmeler Inspector'daki açık sprite referanslarıdır.

### Dışarıdan gün değiştirme

```csharp
using UnityEngine;

public class BookDayBridge : MonoBehaviour
{
    [SerializeField] private Book book;

    public void OnGameDayChanged(int day)
    {
        book.SetDay(day);
    }
}
```

Bu, entegrasyon örneğidir; projeye eklenmiş bir manager veya bridge sınıfı değildir. Mevcut manager'ın olayına bağlanması ayrıca yapılmalıdır.

| API | Davranış |
| --- | --- |
| `SetDay(int day)` | UnityEvent'e uygun `void` giriş noktası; doğrulamayı `TrySetDay` yapar |
| `TrySetDay(int day)` | Geçerli istek kabul edilirse `true`, geçersizse `false` döner |
| `CurrentDay` | Son uygulanmış gün; sıraya alınmış gün henüz burada görünmez |

`TrySetDay` şu durumları reddeder ve uyarı yazar: günün 1'den küçük olması, gün kaydının bulunmaması, aynı gün için birden fazla kayıt bulunması, sprite'lardan birinin eksik olması veya `bookPages` dizisinin üç elemandan kısa olması. Reddedilen istek mevcut içeriği değiştirmez.

Kitap boşta ise gün hemen uygulanır. Sürükleme veya Book'un tamamlama coroutine'i devam ediyorsa kayıt `pendingDay` alanında bekler. İki işlem de bitince `Update()` uygular. Birden fazla geçerli istek gelirse son istek kazanır. Bu nedenle `TrySetDay() == true`, her zaman aynı çağrı içinde görünümün değiştiği anlamına gelmez.

`ApplyDay()` yalnızca indeks 1 ve 2'yi değiştirir, `currentDay` değerini günceller ve açık sayfaların sprite'larını yeniler. Öğretici, kural ve diğer indeksler korunur.

### Kapalı kitap ve manager bağlantısı

Kitap kapalıyken `Update()` çalışmaz ve mevcut kod manager olayına abone değildir. Yeniden açılışta günün asıl kaynaktan okunması için `OnEnable()` → `RefreshDayFromManager()` kullanılır.

Metot içindeki psödo kod:

```csharp
// int oyunGunu = GameManager.Instance.CurrentDay;
// SetDay(oyunGunu);
// return;
```

Gerçek manager hazır olduğunda bu metodu onun API'sine bağlayın. `GameManager.Instance.CurrentDay` mevcut projede çalışan bir bağımlılık değildir. Manager'ın kitap açıldığında kullanılabilir olmasını sağlayın. Bağlantı yapılana kadar metot Inspector'da/son uygulamada tutulan `currentDay` değerini kullanır.

`SetDay` kendiliğinden devre dışı nesnelere mesaj göndermez. Başka kod elinde referans tutuyorsa inactive Book üzerinde metot çağırabilir; fakat sıradaki işlemler aktiflik ve lifecycle'a bağlıdır. Günün kalıcı/asıl sahibi kitap değil manager olmalıdır.

**Önemli:** Kapanışta `ResetBook()` bekleyen `pendingDay` isteğini temizler. Kapanmadan önce sırada kalan gün, yeniden açılışta manager'dan tekrar okunmalıdır. Henüz manager bağlantısı yoksa son uygulanmış gün korunur; yalnızca sıraya alınmış gün kaybolabilir. Günün saklanması, kayıt dosyasından yüklenmesi ve takvim ilerlemesi bu bileşenin sorumluluğu değildir.

### Yeni gün ekleme

1. İki sayfa görselini `Sprite (2D and UI)` olarak içe aktarın.
2. `Book Game Canvas` prefabındaki Book bileşeninde `Daily Pages` listesine kayıt ekleyin.
3. Benzersiz pozitif `day` numarası ve iki sprite atayın.
4. Diğer günlerde de kullanılan kitap sayfa düzenini koruyun; vardiya slotları kodda 1 ve 2'dir.
5. Manager'ın bu gün numarasını ilettiğini ve kitap yeniden açıldığında yeniden okuyabildiğini doğrulayın.

## 8. Açma-kapama ve güvenli sıfırlama

`BookToggle.Awake()` Inspector'daki `startOpen` değerini uygular. Varsayılanı `false` olduğundan oyun başında kitap kapanır. `Update()` her B basışını bir kez algılar. `ToggleBook()`, bağlı Canvas'ın `activeSelf` değerini tersine çevirir. Prefab assetinin kendisini değil, sahnedeki instance'ını etkinleştirir/devre dışı bırakır.

Lifecycle sırası:

```mermaid
sequenceDiagram
    participant K as BookToggle
    participant B as Book
    participant M as Gün kaynağı
    K->>B: Canvas.SetActive(true)
    B->>B: OnEnable → ResetBook
    B->>M: RefreshDayFromManager (entegrasyon noktası)
    Note over B: İlk açılışta Start ölçüleri ve başlangıç düzenini saklar
    B->>B: LateUpdate → RestoreFirstPageVisuals
    Note over B: İlk sayfa, güncel içerik, temiz görsel durum
    K->>B: Canvas.SetActive(false)
    B->>B: OnDisable → ResetBook
    Note over B: Durum hemen sıfırlanır; görsel reset yeniden açılışı bekler
```

`ResetBook()` hemen şunları yapar:

- Bu Book bileşeninin coroutine'lerini durdurur ve `currentCoroutine` referansını temizler.
- `pageDragging`, `dragPointer` ve `pendingDay` değerlerini temizler.
- `currentPage = 0` yapar; takip noktalarını ve çevirme yönünü sıfırlar.
- `visualResetPending = true` işaretler.

`Update()`, görsel reset beklerken normal sürükleme/gün kuyruğu işlerini çalıştırmaz. Aktif Book'un `LateUpdate()` metodu `RestoreFirstPageVisuals()` çağırır. Canvas kapalıysa bu aşama yeniden açılışa kadar çalışmaz.

`Start()` ilk dinlenme düzenini iki şekilde saklar:

1. Sekiz Image için `PageTransformState`: parent, sibling index, anchor, pivot, size, konum, ölçek ve dönüş.
2. `BookPanel` altındaki **tüm** çocukların sırası: hotspotlar da buna dahildir.

Görsel reset; parent'ları ve RectTransform değerlerini geri yükler, sibling sıralarını düzeltir, kıvrılan resimleri/gölgeleri gizler ve ilk sayfa sprite'larını yeniler. Tam çocuk sırası kaydedilmezse resimler doğru görünse bile hotspotların sırası değişebilir.

### Geçmiş hatayı yeniden üretmeyin

Şu işlemleri `OnEnable` / `OnDisable` gövdesine geri taşımayın:

```csharp
// Bu lifecycle callback'leri sırasında hiyerarşi değiştirmeyin:
rect.SetParent(...);
rect.SetSiblingIndex(...);
child.SetActive(...);
```

Unity aktivasyon ağacını gezerken bu işlemler `Cannot set the parent ... while activating or deactivating` ve `Cannot change the sibling position ...` hatalarına yol açmıştır. Düzeltme, hata mesajını bastırmak değil görsel işi `LateUpdate` içine ertelemektir.

`Book`, `[ExecuteInEditMode]` taşır. Runtime açma-kapama callback'leri ve görsel reset `Application.isPlaying` ile korunur; `Start` içindeki orijinal editör önizleme davranışı sürer. Editör ve runtime callback davranışlarını aynı varsaymayın.

## 9. Kodla sayfa çevirme ve AutoFlip

Düşük seviyeli Book API'si:

| Metot/özellik | Kullanım |
| --- | --- |
| `DragRightPageToPoint(Vector3)` | Sağ sayfayı kitap yerel koordinatındaki noktadan çevirmeye başlatır |
| `DragLeftPageToPoint(Vector3)` | Sol sayfayı çevirmeye başlatır |
| `UpdateBookRTLToPoint(Vector3)` | Sağdan sola çevirme geometrisini günceller |
| `UpdateBookLTRToPoint(Vector3)` | Soldan sağa çevirme geometrisini günceller |
| `ReleasePage()` | Mevcut köşeye göre tamamlama/geri dönüş seçer |
| `EndBottomLeft`, `EndBottomRight`, `Height` | Yerel animasyon geometrisine erişim |
| `TotalPageCount` | Sprite dizisinin uzunluğu; açık çift sayfa sayısı değildir |
| `ResetBook()` | İlk sayfa durumunu hemen, görsel düzeni sonraki LateUpdate'te sıfırlar |

Bu yöntemler ekran koordinatı değil **BookPanel yerel koordinatı** bekler. Ekran konumu için önce `transformPoint()` kullanılır. Düşük seviyeli yöntemlerin çağrı sırasını dışarıdaki kod yönetir; kullanıcı input callback'lerindeki tüm kontrolleri otomatik sağlamazlar.

`AutoFlip` bu API'yi parabolik bir takip yolu üzerinden kullanır. `FlipRightPage()` ileri, `FlipLeftPage()` geri bir yaprak çevirir. `StartFlipping()` seçilen yönde sona kadar ilerler. `PageFlipTime`, `AnimationFramesCount`, `TimeBetweenPages` ve `DelayBeforeStarting` parametrelerini kullanır. `TimeBetweenPages`, çevirmeleri başlatma aralığıdır; bitişten sonra eklenen garantili bekleme değildir.

Otomatik kontrolle kullanıcı sürüklemesini birlikte kullanacaksanız sahipliği ayrıca yönetin. Örneğin otomatik kullanımda `interactable = false` yapılabilir. Book'un `ResetBook()` çağrısı yalnızca **Book üzerindeki** coroutine'leri durdurur. `AutoFlip` ayrı bileşendir; kendi coroutine'leri ve `isFlipping` durumu vardır. AutoFlip'in yarıda kesilip yeniden açılma akışı mevcut regresyonun kapsamında değildir; bu kullanım eklenecekse onun lifecycle temizliğini de uygulayın ve test edin.

## 10. Testler ve son doğrulama

`BookLifecycleRegression`, Unity Test Runner testi değil, Editor menüsünden çalışan bir Play Mode regresyon aracıdır.

Çalıştırma:

1. `Example_1_Basic Book` sahnesini açın. Sahnede tek Book/BookToggle çifti ve gün 1–2 ayarları bulunmalıdır.
2. Play Mode'a girin; ilk açılış/asset yüklemesinin tamamlanmasını bekleyin.
3. `Tools → Book → Run Lifecycle Regression` seçin.
4. Test bitene kadar Play Mode'dan çıkmayın ve kitaba input vermeyin.
5. Console'da `BOOK_LIFECYCLE_REGRESSION_PASS` mesajını ve raporu kontrol edin.

Rapor proje kökünden `Temp/BookLifecycleValidation.txt` yoluna yazılır. Temp geçicidir; kalıcı test kanıtı gerekiyorsa raporu ayrıca saklayın. Her çalıştırma raporu yeniler.

Testin doğruladıkları:

- Beş kez tekrarlı açma-kapama.
- Sağ sayfa sürüklemesi sırasında kapatma.
- Geri dönüş animasyonu sırasında kapatma.
- İleri tamamlama animasyonu sırasında kapatma.
- Tamamlanmış çevirmenin sayfayı 2'ye ilerletmesi ve sonrasında sol sürüklemede kapatma.
- Book bileşenini devre dışı bırakıp yeniden etkinleştirme.
- Gün 2 uygulandıktan sonra yeniden açma.
- Geçici bir Input System klavyesiyle B basma/bırakma olayları göndererek gerçek `BookToggle.Update()` akışı.
- Yeniden açılışlarda ilk sayfa, sprite'lar, gizli kıvrılma/gölge nesneleri, parent/sibling ve konumların geri gelmesi.
- Çalışma sırasında `Error`, `Exception` veya `Assert` logu oluşmaması.

Test geçici klavyeyi sonunda kaldırır ve gün 1'e döner. Gerçek işletim sistemi klavyesi yerine Input System state event'leri kullanır. Uyarılar testte otomatik başarısızlık sayılmaz; Console ayrıca incelenmelidir. Zaman tabanlı beklemeler kullanıldığından oyunu duraklatmadan/`timeScale = 1` ile çalıştırın. Birden fazla kitap varsa ilk bulunan nesneler seçildiğinden araç çoklu kitap testi olarak değerlendirilmemelidir.

Son uygulama doğrulamasında 12 senaryo geçti; Console'da hata ve uyarı yoktu. Bu, o tarihteki çalıştırmanın sonucudur; sonraki kod/prefab değişiklikleri için test yeniden çalıştırılmalıdır. Test henüz tüm çoklu dokunma, her Canvas modu, AutoFlip ve gerçek manager entegrasyonu davranışlarını kapsamamaktadır.

## 11. Sorun giderme ve değişiklik rehberi

| Belirti | Önce kontrol edilecek yer |
| --- | --- |
| B ile kapanıyor ama yeniden açılmıyor | Controller'ın aktif kalması, Canvas'ın altına konmamış olması |
| B hiç çalışmıyor | BookToggle etkinliği, `bookCanvas` ataması, Game görünümü odağı ve Input System |
| Sürükleme başlamıyor | EventSystem/UI modülü, GraphicRaycaster, hotspot raycast'i, dinamik Begin Drag callback'i, `interactable` |
| Gün değişmiyor | Manager bağlantısı, eşsiz gün kaydı, iki sprite referansı, en az üç Book Pages elemanı |
| `TrySetDay` true ama sprite henüz değişmedi | Devam eden drag/coroutine nedeniyle bekleyen istek |
| Kapanırken parent/sibling hatası | Hiyerarşi işlemlerinin lifecycle callback'lerine taşınmış olması |
| Yarım çevirmeden sonra bozuk sayfa/hotspot | Başlangıç RectTransform durumları ve tüm panel çocuk sırasının geri yüklenmesi |
| Yeniden açılınca eski gün | Manager henüz bağlı olmayabilir; pending gün reset sırasında temizlenmiş olabilir |
| AutoFlip ile yeniden açılışta çevirmiyor | AutoFlip'in ayrı coroutine ve `isFlipping` lifecycle durumu |

Geliştirici/AI için değişiklik sırası:

1. Önce hangi prefab/sahnenin kullanıldığını doğrulayın; temel Book ile oyun Canvas'ının override'larını ayırt edin.
2. Günlük içerik değişecekse Inspector'daki `Daily Pages` verisini; sayfa slotları değişecekse `ApplyDay()` ve dokümantasyonu birlikte güncelleyin.
3. Manager bağlanacaksa `RefreshDayFromManager()` içindeki psödo kodu gerçek API ile değiştirin; olmayan sınıfı varmış gibi varsaymayın.
4. Açma-kapama değişecekse `visualResetPending` → `LateUpdate` sözleşmesini ve controller'ın aktif kalmasını koruyun.
5. Sayfa içeriği/sayısı değişecekse çift sayfa indekslerini ve regresyon testinin varsayımlarını da güncelleyin.
6. Input değişecekse `UnityEngine.Input` çağrılarına geri dönmeyin; pointer kimliği ve olay parametresi aktarımını koruyun.
7. Değişiklik sonrasında derlemeyi ve ilgili Play Mode regresyonlarını çalıştırın; test kapsamını aşan davranışı ayrıca doğrulayın.

Bu sistem şu anda kendiliğinden gün ilerletmez, gerçek manager'a abone olmaz, gün verisini diske kaydetmez ve dinamik metni sprite'a dönüştürmez. Bunlar ek entegrasyon işleridir; mevcut özelliklerle karıştırılmamalıdır.
