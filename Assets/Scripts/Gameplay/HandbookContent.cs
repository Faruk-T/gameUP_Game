using System.Linq;
using System.Text;
using GameUp.Lead;
using GameUP.Rules;

namespace GameUp.Gameplay
{
    /// <summary>Formats the field manual from the current day's public information and active rules.</summary>
    public static class HandbookContent
    {
        public static string Build(DayFlowSession flow)
        {
            var day = flow.Day;
            var text = new StringBuilder();
            text.Append("<b>VARDİYA: GÜN ").Append(flow.DayIndex + 1)
                .Append("  •  TARİH: ").Append(day.Date.ToString("dd.MM.yyyy")).AppendLine("</b>");
            text.Append("<b>DENETLENEN GRUPLAR:</b> ")
                .AppendLine(string.Join(", ", day.Products.Select(p => p.Category).Distinct()));

            Heading(text, "1. SABAH GAZETESİ VE GÜNLÜK ODAK");
            text.Append("<b>").Append(day.Title).AppendLine("</b>");
            text.AppendLine(day.Newspaper);
            text.AppendLine();
            text.AppendLine(flow.DayIndex switch
            {
                0 => "<b>Bugünün odağı:</b> Et ve süt ürünlerinde SKT, ön/arka etiket uyumu ve görünür bozulmayı birlikte kontrol edin. Bugünün tarihiyle aynı SKT geçerlidir; daha eski SKT reddedilir.",
                1 => "<b>Bugünün odağı:</b> Meyveler açıldı. Temiz görünüm gizli kirliliği dışlamaz. <b>F / Analiz</b> ile rapor alın; POZİTİF ürünü reddedin. Et ve süt ürünlerinde de önceki kontroller sürer.",
                _ => "<b>Bugünün odağı:</b> Yumurta açıldı; karma vardiyada SKT, etiket, bozulma ve analizi kontrol edin. Kaynak bölgesini okuyun. Bakanlığın zorunlu acil duyurusunu onayladığınızda yeni kural sonraki ürünlere uygulanır; önceki kararlar değişmez."
            });

            if (day.Emergency != null && flow.CurrentRules.Any(r => r.Id == day.Emergency.AdditionalRule.Id))
            {
                Heading(text, "AKTİF BAKANLIK ACİL FAKSI / 12.00", "#9E2A2B");
                text.AppendLine(day.Emergency.Text);
            }

            Heading(text, "2. YÜRÜRLÜKTEKİ DENETİM KURALLARI");
            foreach (var rule in flow.CurrentRules) text.Append("• ").AppendLine(rule.Text);

            Heading(text, "3. ATIK AYRIŞTIRMA REHBERİ");
            text.AppendLine("<b>R / RED</b> sonrası gıda imha edilir; boş ambalajı arkanızdaki uygun kutuya tıklayarak ayrıştırın.");
            text.AppendLine("• <b><color=#295EB3>MAVİ / KÂĞIT:</color></b> Temiz kâğıt ve karton ambalajlar.");
            text.AppendLine("• <b><color=#248059>YEŞİL / CAM:</color></b> Temiz cam şişe ve kavanozlar.");
            text.AppendLine("• <b><color=#987000>SARI / PLASTİK:</color></b> Temiz plastik kap ve paketler.");
            text.AppendLine("• <b><color=#A62E29>KIRMIZI / KONTAMİNE:</color></b> Analizde POZİTİF çıkan ürünlerin ambalajları, malzemesi ne olursa olsun ayrı imhaya gider. Analiz raporunun ambalaj satırı hedefi belirtir.");
            text.AppendLine("SKT, görünür bozulma, etiket uyumsuzluğu veya geri çağırma tek başına ambalajı kontamine yapmaz. Toksik kirlilik yoksa ambalajı etiketinde yazan malzemeye göre ayırın.");

            Heading(text, "4. KONTROLLER VE PUANLAMA");
            text.AppendLine("• <b>E veya ürüne sol tık:</b> Yakından incele / masaya bırak.");
            text.AppendLine("• <b>İncelemede sol tık + sürükle:</b> Döndür. <b>Fare tekerleği:</b> Yaklaş / uzaklaş.");
            text.AppendLine("• <b>F veya analiz cihazı:</b> Gizli kirlilik analizi al; tamamlanan rapor otomatik açılır.");
            text.AppendLine("• <b>Bakanlık faksı:</b> Hatalı karar ve hatalı ayrıştırma ayrı yanlış işlemlerdir. Aynı gün üçüncü yanlış işlemde vardiya kaybedilir. Sayaç her sabah sıfırlanır; hata bildirimini onaylamadan yeni ürün gelmez.");
            text.AppendLine("• <b>İnceleme:</b> Kamera sabit kalır; sadece ürün döner ve yakınlaşır. Tüm bilgiler ürünün ön/arka etiketindedir.");
            text.AppendLine("• <b>B veya masadaki kitap:</b> El kitapçığını, gazete özetini ve güncel kuralları aç.");
            text.AppendLine("• <b>W / ONAY:</b> Ürünü piyasaya gönder. <b>R / RED:</b> Reddet, ardından ambalajı ayrıştır.");
            text.AppendLine("• <b>ESC:</b> Belgeyi kapat / oyunu duraklat. Zorunlu gazete ve acil duyuruyu onay düğmesiyle tamamlayın.");
            text.Append("• <b>Puanlama:</b> Doğru karar ").Append(Signed(ProductRuleEvaluator.ScoreCorrectDecision))
                .Append(" | Doğru ayrıştırma ").Append(Signed(ProductRuleEvaluator.ScoreCorrectSorting))
                .Append(" | Hatalı red ").Append(Signed(ProductRuleEvaluator.ScoreFalseRejection))
                .Append(" | Hatalı onay ").Append(Signed(ProductRuleEvaluator.ScoreFalseAcceptance))
                .Append(" | Yanlış kutu ").Append(Signed(ProductRuleEvaluator.ScoreWrongSorting))
                .AppendLine(". Başarı için en az %75 karar doğruluğu gerekir.");
            return text.ToString();
        }

        private static string Signed(int score) => score.ToString("+0;-0;0");
        private static void Heading(StringBuilder text, string title, string color = "#1F6E64")
        {
            text.AppendLine();
            text.Append("<b><color=").Append(color).Append('>').Append(title).AppendLine("</color></b>");
        }
    }
}
