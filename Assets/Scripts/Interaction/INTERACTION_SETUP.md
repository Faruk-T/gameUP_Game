# Etkileşim MVP test sahnesi

Unity menüsünden **GameUP32 → Interaction → Build Test Scene** seçerek `Assets/Scenes/Tests_Interaction.unity` sahnesini üretin. Sahne; sabit kamera, masa, cihaz/düğmeler/atık hedefleri, test akışı ve `InspectionService` bağlantılarıyla gelir. Play'e basınca örnek günün ilk ürünü otomatik başlar. Bu test sahnesi `SampleScene`'i değiştirmez.

## Kontroller

- Fare hareketiyle bakış; kamera ilk girişte fareyi kilitler, `Esc` kilidi açıp kapatır.
- Nişangâhı ürüne getirip sol tık veya `E` ile incelemeye al; incelemede sol fareyle döndür, fare tekerleğiyle yaklaştır/uzaklaştır; ürüne kısa sol tık veya `E` ile masaya bırak.
- Cihaza bakıp sol tıkla analizi çalıştır.
- Onay veya Red düğmesine bakıp sol tıkla karar ver.
- Red sonrası ambalaj hedeflerinden birine bakıp sol tıkla ayrıştır.
- Sonuç kaydı ekranda göründükten sonra `N` ile sonraki örnek ürüne geç.
- Hızlı akış kontrolü için test sahnesinde `F1` analiz, `F2` onay, `F3` red; `1`–`4` atık hedeflerini çağırır. Bu kısa yollar yalnızca test bootstrap'ında bulunur.

Ürün prefabı `ProductDefinition.Prefab` alanından alınır; boşsa test için geçici küp oluşturulur. Ürün masaya geldiğinde etiketleri `ProductWorldLabelBinder` doldurur. Karar/atık hareketi bitmeden sonuç API'si çağrılmaz.

Belge arayüzü dünya girdisini `SetWorldInputLocked(true/false)` ile kilitleyebilir. Analiz raporu testte durum metninde gösterilir; faks/kâğıt sunumu teknik liderin belge arayüzüne aittir.
