using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf;
using System.Text.RegularExpressions;

namespace PDFExtractor.Services
{
    public class TextExtractor
    {
        public (string Geschaeftszahl, string KlagendePartei, string Vertreter, string BetragWegen, string BetragSumme) ExtractInformation(string pdfPath)
        {
            // Extrahiere Text aus der PDF-Datei
            string text = ExtractTextFromPdf(pdfPath);

            // Suche nach Geschäftszahl, klagender Partei, Vertretung und Beträgen
            string geschaeftszahl = ExtractGeschaeftszahl(text);
            string klagendePartei = ExtractKlagendePartei(text);
            string vertreter = ExtractVertreter(text);
            string betragWeegen = ExtractBetragWegen(text); // Betrag nach "WEGEN:"
            string betragSumme = ExtractBetragSumme(text); // Betrag nach "Summe:"

            return (geschaeftszahl, klagendePartei, vertreter, betragWeegen, betragSumme);
        }



        private string ExtractTextFromPdf(string pdfPath)
        {
            // PDF laden und Text extrahieren
            using var pdfReader = new PdfReader(pdfPath);
            using var pdfDocument = new PdfDocument(pdfReader);
            var strategy = new SimpleTextExtractionStrategy();

            string text = "";
            for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
            {
                text += PdfTextExtractor.GetTextFromPage(pdfDocument.GetPage(i), strategy);
            }
            return text;
        }

        private string ExtractGeschaeftszahl(string text)
        {
            // Regex für Geschäftszahl
            var match = Regex.Match(text, @"\b\d{1,2}\sC\s\d{1,4}\/\d+[a-zA-Z]?\b", RegexOptions.IgnoreCase);
            return match.Success ? match.Value : "Keine Geschäftszahl gefunden";
        }

        private string ExtractKlagendePartei(string text)
        {
            // Regex für klagende Partei (Vor- und Nachname)
            var match = Regex.Match(text, @"klagende Partei:\s*([A-ZÄÖÜa-zäöüß]+)\s([A-ZÄÖÜa-zäöüß]+)", RegexOptions.IgnoreCase);
            return match.Success ? $"{match.Groups[1].Value} {match.Groups[2].Value}" : "Keine klagende Partei gefunden";
        }

        private string ExtractVertreter(string text)
        {
            // Regex für Titel, Vorname, Nachname und optionale Zusätze (wie LL.M.)
            var match = Regex.Match(
                text,
                @"vertreten\s*durch:\s*((?:[A-Za-zäöüÄÖÜß]+\.*\s?)*[A-ZÄÖÜ][a-zäöüß]+(?:,\s*[A-Za-z0-9\.]+)?)(?=\s*(?:\n|$))",
                RegexOptions.IgnoreCase);

            if (match.Success)
            {
                // Nur den Namen extrahieren
                return match.Groups[1].Value.Trim();
            }

            return "Keine Vertretung gefunden";
        }

        private string ExtractBetragWegen(string text)
        {
            // Regex für den Geldbetrag nach "WEGEN:"
            var match = Regex.Match(text, @"WEGEN:\s*(€?\s*\d{1,3}(?:[\.,]\d{1,2})?\s*(?:EUR|€)?)", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                // Entferne Währungszusatz und gib nur den Betrag zurück
                return Regex.Replace(match.Groups[1].Value, @"\s*(EUR|€)", "").Trim();
            }

            return "Kein Betrag gefunden";
        }


        private string ExtractBetragSumme(string text)
        {
            // Regex für "Summe:" und den Geldbetrag
            var matches = Regex.Matches(text, @"SUMME:\s*(€?\s*\d{1,3}(?:[\.,]\d{1,2})?\s*(?:EUR|€)?)", RegexOptions.IgnoreCase);

            if (matches.Count >= 2)
            {
                decimal maxAmount = decimal.MinValue;
                string maxAmountString = "";

                foreach (Match match in matches)
                {
                    var betrag = match.Groups[1].Value.Trim();
                    // Entferne Währungszusatz
                    var cleanedBetrag = Regex.Replace(betrag, @"\s*(EUR|€)", "").Trim();

                    if (Decimal.TryParse(cleanedBetrag, System.Globalization.NumberStyles.Currency, null, out decimal amount))
                    {
                        if (amount > maxAmount)
                        {
                            maxAmount = amount;
                            maxAmountString = cleanedBetrag;
                        }
                    }
                }

                return maxAmountString;
            }

            return "Kein Betrag gefunden";
        }







    }
}
