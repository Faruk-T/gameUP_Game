# Üretim etkileşimi

Başlangıç: Assets/Scenes/First.unity. Ayrıntılar kökteki LEAD_INTEGRATION.md dosyasında.

WorkstationInteractionController tek oynanış girdi sahibidir. ProductPresentation, IProductPresentation üzerinden GameSessionController'a bağlanır. İkinci bir InspectionService çalışmaz.

Ürüne bakıp E/sol tık: incele; sürükle: döndür; tekerlek: yaklaş; E/kısa tık: bırak. W onay, R red, F analiz, B kitapçık, Esc belge/pause. Atık hedefleri sol tıkla seçilir.

Bütün ürünler, etiketler, Collider'lar ve hedefler Game sahnesinde kayıtlıdır. Runtime Instantiate/AddComponent, test bootstrap veya OnGUI yoktur.
