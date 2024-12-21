using ClosedXML.Excel;
using Microsoft.Win32;
using PDFExtractor.Services;
using System;
using System.Collections.Generic;
using System.Windows;

namespace PDFExtractor
{
    public partial class MainWindow : Window
    {
        private readonly TextExtractor _pdfExtractor;

        public MainWindow()
        {
            InitializeComponent();
            _pdfExtractor = new TextExtractor();
        }

        private void SelectPdfButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                Multiselect = true // Ermöglicht die Auswahl mehrerer Dateien
            };

            if (openFileDialog.ShowDialog() == true)
            {
                // Alle ausgewählten Dateien anzeigen
                SelectedPdfListBox.Items.Clear();
                foreach (var file in openFileDialog.FileNames)
                {
                    SelectedPdfListBox.Items.Add(file);
                }
            }
        }

        private void ExtractButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPdfListBox.Items.Count == 0)
            {
                MessageBox.Show("Bitte wählen Sie mindestens eine PDF-Datei aus.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Alle PDFs durchgehen und Informationen extrahieren
                ResultsListBox.Items.Clear();

                foreach (var pdfFile in SelectedPdfListBox.Items)
                {
                    var filePath = pdfFile.ToString();
                    var (geschaeftszahl, klagendePartei, vertreter, betragWeegen, betragSumme) = _pdfExtractor.ExtractInformation(filePath);

                    // Ergebnisse anzeigen
                    ResultsListBox.Items.Add($"--- {filePath} ---");
                    ResultsListBox.Items.Add($"Geschäftszahl: {geschaeftszahl}");
                    ResultsListBox.Items.Add($"Klagende Partei: {klagendePartei}");
                    ResultsListBox.Items.Add($"Vertreten durch: {vertreter}");
                    ResultsListBox.Items.Add($"Betrag nach 'WEGEN:': {betragWeegen}");
                    ResultsListBox.Items.Add($"Betrag nach 'Summe:': {betragSumme}");
                    ResultsListBox.Items.Add(""); // Leerzeile für bessere Lesbarkeit
                }
                // Export-Button aktivieren
                ExportButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ein Fehler ist aufgetreten: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Speicherort des Desktops
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string filePath = $"{desktopPath}\\ExtractedData.xlsx";

                // Excel-Datei erstellen und Daten hinzufügen
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.AddWorksheet("Daten");

                    // Tabellenkopf
                    worksheet.Cell(1, 1).Value = "Geschäftszahl";
                    worksheet.Cell(1, 2).Value = "Klagende Partei";
                    worksheet.Cell(1, 3).Value = "Vertreter";
                    worksheet.Cell(1, 4).Value = "Wegen";
                    worksheet.Cell(1, 5).Value = "Summe";

                    // Daten ab Zeile 2
                    int row = 2;

                    // Alle PDFs durchgehen und extrahierte Daten in die Excel-Tabelle einfügen
                    foreach (var pdfFile in SelectedPdfListBox.Items)
                    {
                        var filePaths = pdfFile.ToString();
                        var (geschaeftszahl, klagendePartei, vertreter, betragWegen, betragSumme) = _pdfExtractor.ExtractInformation(filePaths);

                        // Werte in die entsprechenden Zellen einfügen
                        worksheet.Cell(row, 1).Value = geschaeftszahl;
                        worksheet.Cell(row, 2).Value = klagendePartei;
                        worksheet.Cell(row, 3).Value = vertreter;
                        worksheet.Cell(row, 4).Value = betragWegen;
                        worksheet.Cell(row, 5).Value = betragSumme;

                        row++; // Zur nächsten Zeile wechseln
                    }

                    // Datei speichern
                    workbook.SaveAs(filePath);
                }

                MessageBox.Show($"Daten erfolgreich exportiert nach {filePath}", "Export erfolgreich", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ein Fehler ist aufgetreten: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void CopyToClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            // Überprüfen, ob Ergebnisse vorhanden sind
            if (ResultsListBox.Items.Count > 0)
            {
                // Den Text aus der ListBox extrahieren und zu einer einzelnen Zeichenkette zusammenfügen
                var resultText = string.Join(Environment.NewLine, ResultsListBox.Items.Cast<string>());

                // Text in die Zwischenablage kopieren
                Clipboard.SetText(resultText);

                // Bestätigung anzeigen
                MessageBox.Show("Die Daten wurden in die Zwischenablage kopiert.", "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Es gibt keine Daten zum Kopieren.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



    }
}
