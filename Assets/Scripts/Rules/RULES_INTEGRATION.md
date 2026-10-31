# Kuralların üretim bağlantısı

ProductionInspectionBackend, kaydedilmiş DayDefinition/ProductDefinition/RuleDefinition assetlerini ortak GameUp.Contracts modellerine çevirir. Kararları ProductRuleEvaluator verir. Akışı DayFlowSession yürütür. İkinci bir üretim InspectionRulesEngine/InspectionService yoktur.

İki günlük katalog Game sahnesindeki backend'e atanır; eksik katalog için test verisi üretilmez. Yeni ürün eklenirse asset, prefab ve kayıtlı ProductPresentation havuzu birlikte güncellenir.

DocumentDefinition ayrı dosyaya taşındı. Bozuk m_Script referansları belge asset GUID'leri korunarak onarıldı. Ürün çalışma durumu ScriptableObject'e yazılmaz. Analiz RunId ile önbelleğe alınır; BeginRun eski coroutine'leri ve puanı temizler.

Editor/Verification eski saf motorun kabul test referans uygulamasıdır; oyuncu build'ine girmez. Test kataloğu yalnızca testlerde açıkça oluşturulur. Editor açılışında sahne/veri üreten bootstrap kaldırılmıştır.

Sahne kurulumu ve test komutları: kökte LEAD_INTEGRATION.md.

