# Faruk (Kişi 3 — Veri, Kurallar, Analiz ve Değerlendirme) Entegrasyon Rehberi

Bu modül (`Assets/Scripts/Rules/` ve `Assets/Data/`), `IMPLEMENTATION.md` içindeki **R01–R09** işlerini, **T01–T17** kabul senaryolarını, **P1 (Tartı, Cetvel, Meyve/Sebze/Et, Muzlu Gofret Yüzdesi)** genişlemesini ve `LEAD_INTEGRATION.md` içindeki `GameUp.Contracts` (`ICampaignSource` + `IInspectionBackend`) bağlantılarını eksiksiz uygular.

---

## 1. Teknik Lider (Hakan — `LeadRuntime`) İçin 10 Saniyede Bağlantı

1. `Assets/Scenes/LeadPrototype.unity` (veya ana sahneyi) açın.
2. Unity üst menüsünden:
   **`GameUP32 -> Faruk (Rules & Data) -> 5. Açık Sahnedeki LeadRuntime'a Faruk Backend'ini Bağla`**
   seçeneğine tıklayın.
3. Bu komut `LeadSystems` nesnesine [`ProductionInspectionBackend`](file:///c:/Users/user/Desktop/gameUP_Game-main/Assets/Scripts/Rules/ProductionInspectionBackend.cs) bileşenini ekler, `Assets/Data/Days/Day_01_Dairy.asset` ve `Day_02_Fish.asset` verilerini bağlar ve `LeadRuntime` üzerindeki `Campaign Source` ile `Inspection Backend` referanslarını otomatik olarak günceller.
4. *(İsteğe Bağlı P1 3. Gün)*: `ProductionInspectionBackend` üzerindeki `Configured Days` listesine `Assets/Data/Days/Day_03_P1_Extended.asset` eklendiğinde, ana döngüde hiçbir kod değişikliği yapmadan 3. Gün (**Finike Portakalı, Kumluca Domatesi Tartı Hilesi, Antalya Çipurası Cetvel Boy Kontrolü, Muzlu Gofret %10 vs %0.001, Toros Kuzu Eti, Alanya Avokadosu**) devreye girer.

---

## 2. Etkileşim (Özgür) ve 3D Tasarımcı İçin 3D Ön/Arka Etiket Bağlantısı

Resident Evil tarzı 3D nesne incelemesinde oyuncunun nesneyi döndürerek **Ön Yüz Beyanını** ve **Arka Yüz Etiketini (SKT, Tür, İçindekiler, Kaynak/Habitat, Ambalaj)** doğrudan 3D model üzerinde okuyabilmesi için [`ProductWorldLabelBinder`](file:///c:/Users/user/Desktop/gameUP_Game-main/Assets/Scripts/Rules/ProductWorldLabelBinder.cs) hazırlandı:

- Herhangi bir 3D ürün prefabına `ProductWorldLabelBinder` bileşenini ekleyin.
- Ürün masaya geldiğinde:
  ```csharp
  // Doğrudan ProductDefinition ScriptableObject ile:
  binder.Bind(productDefinition);

  // Veya LeadRuntime / ProductSession içindeki ProductInfo ile:
  binder.BindFromLeadProductInfo(session.Product);
  ```
- Bileşen otomatik olarak ürünün ön (`-Z`) ve arka (`+Z`) yüzlerine World-Space etiket yüzeyleri yerleştirir (veya kendi atadığınız `Text` bileşenlerini doldurur).

---

## 3. Üretilen Veri Assetleri (`Assets/Data/`)

| Klasör | İçerik |
| --- | --- |
| `Assets/Data/Rules/` | `Rule_R_EXPIRY`, `Rule_R_IDENTITY`, `Rule_R_POLLUTION`, `Rule_R_SOURCE_BAN` (P0) + `Rule_R_WEIGHT`, `Rule_R_LENGTH`, `Rule_R_INGREDIENT_PCT` (P1) |
| `Assets/Data/Documents/` | `Doc_Day1_Newspaper`, `Doc_Day2_Newspaper` (Caretta), `Doc_Day2_EmergencyFax` (Saat 12.00), `Doc_Day3_P1_Newspaper` |
| `Assets/Data/Products/` | `D1-01`..`D1-06` (4 Yerel Süt Ürünü & Sütlü Buz), `D2-01`..`D2-06` (Balık & Acil Yasak), `D3-01`..`D3-06` & `P1-01`..`P1-02` (P1 Tartı, Cetvel, Muzlu Gofret, Et/Sebze/Meyve) |
| `Assets/Data/Days/` | `Day_01_Dairy.asset`, `Day_02_Fish.asset` (P0 12 ürün), `Day_03_P1_Extended.asset` (P1 +6 ürün) |

---

## 4. Otomatik Doğrulama (`15/15 PASS`)

Unity üst menüsünden **`GameUP32 -> Faruk (Rules & Data) -> 2. T01–T17 Kabul Testlerini Çalıştır`** komutuyla veya `Assets/Scenes/Tests_Rules.unity` sahnesinde Play tuşuna basarak tüm sınır testlerini (`T01`–`T17`, `P1` ve `LEAD 132 Puan`) doğrulayabilirsiniz.
