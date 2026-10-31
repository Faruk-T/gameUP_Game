# Esmanur model envanteri

Kaynak: Esmanur / esmnrrr, feature/Objects, commit 0dc833b. Aktif oyun yalnız bu kaynak ürün modellerini kullanır.

- Kaynak prefab: **31**
- Oynanış ürünü: **31**
- Gün: **3**
- Kodla hazırlanan görünüm: her model için Fresh + Spoiled (**62 prefab varyantı**)
- Teşhir dolabı: **0**

| Kaynak model | Gün / ürün | Görünüm |
|---|---|---|
| [Cantaloupe](../Assets/Art/Urunler/Mesh/Cantaloupe.prefab) | 2 / D2-01 | Fresh |
| [Cantaloupe_01](../Assets/Art/Urunler/Mesh/Cantaloupe_01.prefab) | 2 / D2-04 | Fresh |
| [Eggs](../Assets/Art/Urunler/Mesh/Eggs.prefab) | 3 / D3-02 | Fresh |
| [Papaya](../Assets/Art/Urunler/Mesh/Papaya.prefab) | 2 / D2-02 | Spoiled |
| [Papaya_01](../Assets/Art/Urunler/Mesh/Papaya_01.prefab) | 2 / D2-05 | Fresh |
| [Pineapple](../Assets/Art/Urunler/Mesh/Pineapple.prefab) | 2 / D2-03 | Fresh |
| [Pineapple_01](../Assets/Art/Urunler/Mesh/Pineapple_01.prefab) | 2 / D2-06 | Spoiled |
| [Strawberry](../Assets/Art/Urunler/Mesh/Strawberry.prefab) | 3 / D3-05 | Fresh |
| [Strawberry_01](../Assets/Art/Urunler/Mesh/Strawberry_01.prefab) | 3 / D3-07 | Fresh |
| [Watermelon](../Assets/Art/Urunler/Mesh/Watermelon.prefab) | 3 / D3-06 | Spoiled |
| [Watermelon_01](../Assets/Art/Urunler/Mesh/Watermelon_01.prefab) | 3 / D3-10 | Fresh |
| [Yoghurt_01](../Assets/Art/Urunler/Mesh/Yoghurt_01.prefab) | 1 / D1-04 | Fresh |
| [Yoghurt_02](../Assets/Art/Urunler/Mesh/Yoghurt_02.prefab) | 2 / D2-09 | Fresh |
| [Yoghurt_03](../Assets/Art/Urunler/Mesh/Yoghurt_03.prefab) | 3 / D3-12 | Spoiled |
| [cheese1](../Assets/Art/Urunler/Mesh/cheese1.prefab) | 1 / D1-03 | Fresh |
| [cheese2](../Assets/Art/Urunler/Mesh/cheese2.prefab) | 1 / D1-07 | Spoiled |
| [cheese3](../Assets/Art/Urunler/Mesh/cheese3.prefab) | 2 / D2-08 | Fresh |
| [cheese4](../Assets/Art/Urunler/Mesh/cheese4.prefab) | 3 / D3-04 | Fresh |
| [cheese5](../Assets/Art/Urunler/Mesh/cheese5.prefab) | 3 / D3-09 | Fresh |
| [cheese6](../Assets/Art/Urunler/Mesh/cheese6.prefab) | 3 / D3-13 | Fresh |
| [meat1](../Assets/Art/Urunler/Mesh/meat1.prefab) | 1 / D1-02 | Spoiled |
| [meat2](../Assets/Art/Urunler/Mesh/meat2.prefab) | 1 / D1-05 | Fresh |
| [meat3](../Assets/Art/Urunler/Mesh/meat3.prefab) | 1 / D1-08 | Fresh |
| [meat4](../Assets/Art/Urunler/Mesh/meat4.prefab) | 2 / D2-10 | Fresh |
| [meat5](../Assets/Art/Urunler/Mesh/meat5.prefab) | 3 / D3-03 | Fresh |
| [milk1](../Assets/Art/Urunler/Mesh/milk1.prefab) | 1 / D1-01 | Fresh |
| [milk2](../Assets/Art/Urunler/Mesh/milk2.prefab) | 1 / D1-06 | Fresh |
| [milk3](../Assets/Art/Urunler/Mesh/milk3.prefab) | 2 / D2-07 | Fresh |
| [milk4](../Assets/Art/Urunler/Mesh/milk4.prefab) | 3 / D3-01 | Fresh |
| [milk5](../Assets/Art/Urunler/Mesh/milk5.prefab) | 3 / D3-08 | Fresh |
| [milk6](../Assets/Art/Urunler/Mesh/milk6.prefab) | 3 / D3-11 | Fresh |

Varyantlar aynı sanatçı mesh'lerini kullanır; yeni ürün modeli import edilmez. Spoiled görünümünde boyama/küf deseni ve ambalaj şişmesi shader koduyla belirlenir. Malzemeler Editor'de hazırlanır, çalışma sırasında prefab veya materyal oluşturulmaz.

Ürün bazındaki onay/red matrisi: [CAMPAIGN_MATRIX.md](CAMPAIGN_MATRIX.md). Önceki kampanya verileri GUID'leri korunarak Assets/Tests/Editor/LegacyContent altında referans arşivine taşınmıştır; aktif sahne bu arşive bağlı değildir.
