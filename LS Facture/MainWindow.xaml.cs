using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Tesseract;
using Microsoft.Win32;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Runtime.InteropServices;

namespace InvoiceAnalyzer
{
    public partial class MainWindow : Window
    {
        private BitmapSource selectedImage;
        private BitmapSource quarterImage;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnScanImage(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Choisissez une image",
                Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                selectedImage = new BitmapImage(new Uri(openFileDialog.FileName, UriKind.Absolute));
                ProcessSelectedImage();
            }
        }

        private void OnPasteImage(object sender, RoutedEventArgs e)
        {
            try
            {
                IDataObject dataObject = Clipboard.GetDataObject();
                if (dataObject == null)
                {
                    MessageBox.Show("Le presse-papiers est vide ou inaccessible.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string[] formats = dataObject.GetFormats();
                string formatsList = string.Join("\n", formats);

                // Priorité au format FileDrop
                if (dataObject.GetDataPresent(DataFormats.FileDrop))
                {
                    var files = (string[])dataObject.GetData(DataFormats.FileDrop);
                    if (files.Length > 0)
                    {
                        string file = files[0];

                        // Vérifier que le fichier existe et obtenir un chemin absolu
                        string fullPath;
                        try
                        {
                            fullPath = Path.GetFullPath(file);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Chemin du fichier non valide : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        if (File.Exists(fullPath) && IsImageFile(fullPath))
                        {
                            selectedImage = new BitmapImage(new Uri(fullPath));
                            ProcessSelectedImage();
                        }
                        else
                        {
                            MessageBox.Show("Le fichier dans le presse-papiers n'est pas une image ou n'existe pas.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                else if (dataObject.GetDataPresent(DataFormats.Bitmap))
                {
                    selectedImage = Clipboard.GetImage();
                    ProcessSelectedImage();
                }
                else if (dataObject.GetDataPresent(DataFormats.Dib))
                {
                    var dib = dataObject.GetData(DataFormats.Dib) as MemoryStream;
                    if (dib != null)
                    {
                        selectedImage = LoadBitmapSourceFromDib(dib);
                        ProcessSelectedImage();
                    }
                }
                else
                {
                    MessageBox.Show("Le presse-papiers ne contient pas d'image compatible.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (COMException ex)
            {
                MessageBox.Show($"Erreur lors de la récupération de l'image du presse-papiers : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Une erreur inattendue s'est produite : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private BitmapSource LoadBitmapSourceFromDib(MemoryStream dib)
        {
            byte[] dibBuffer = dib.ToArray();

            // Préparer le header BMP
            byte[] bmpHeader = new byte[14];
            int fileSize = 14 + dibBuffer.Length;
            bmpHeader[0] = 0x42; // 'B'
            bmpHeader[1] = 0x4D; // 'M'
            BitConverter.GetBytes(fileSize).CopyTo(bmpHeader, 2);
            BitConverter.GetBytes(0).CopyTo(bmpHeader, 6); // bfReserved1 et bfReserved2
            BitConverter.GetBytes(14 + BitConverter.ToInt32(dibBuffer, 0)).CopyTo(bmpHeader, 10); // bfOffBits

            using (var bmpStream = new MemoryStream())
            {
                bmpStream.Write(bmpHeader, 0, bmpHeader.Length);
                bmpStream.Write(dibBuffer, 0, dibBuffer.Length);
                bmpStream.Seek(0, SeekOrigin.Begin);

                BitmapImage bmpImage = new BitmapImage();
                bmpImage.BeginInit();
                bmpImage.StreamSource = bmpStream;
                bmpImage.CacheOption = BitmapCacheOption.OnLoad;
                bmpImage.EndInit();
                bmpImage.Freeze();

                return bmpImage;
            }
        }

        private void ProcessSelectedImage()
        {
            FullImage.Source = selectedImage;

            // Recadrer et afficher le quart supérieur gauche de l'image
            quarterImage = CropToTopLeftQuarter(selectedImage);
            QuarterImage.Source = quarterImage;

            // Effectuer l'OCR
            string ocrText = PerformOCR(quarterImage);
            ProcessAndDisplayInvoice(ocrText);
        }

        private BitmapSource CropToTopLeftQuarter(BitmapSource image)
        {
            var quarter = new CroppedBitmap(image, new Int32Rect(0, 0, image.PixelWidth / 2, image.PixelHeight / 2));
            return quarter;
        }

        private string PerformOCR(BitmapSource image)
        {
            string tempFilePath = System.IO.Path.GetTempFileName();
            SaveBitmapImageToFile(image, tempFilePath);

            // Chemin vers Tesseract local (à adapter selon votre configuration)
            string tesseractFolder = @"C:\Users\loric\Source\Repos\LS Facture\Tesseract-OCR";
            string tessDataPath = Path.Combine(tesseractFolder, "tessdata");

            try
            {
                using (var engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default))
                using (var img = Pix.LoadFromFile(tempFilePath))
                {
                    var result = engine.Process(img);
                    return result.GetText();
                }
            }
            catch (TesseractException ex)
            {
                MessageBox.Show($"Erreur d'initialisation du moteur Tesseract : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return string.Empty;
            }
            finally
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }

        private void SaveBitmapImageToFile(BitmapSource image, string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(fs);
            }
        }

        private void ProcessAndDisplayInvoice(string text)
        {
            string filteredText = RemoveAfterPeriod(text);
            int? price = ExtractPrice(filteredText);

            if (price.HasValue)
            {
                int customPrice = (int)(price.Value * 1.3);
                int benefit = customPrice - price.Value;

                string prixUsineStr = FormatNumberWithSpaces(price.Value);
                string prixCustomStr = FormatNumberWithSpaces(customPrice);
                string benefitStr = FormatNumberWithSpaces(benefit);

                InvoiceText.Text = $"Facture\n\nPrix usine →\t{prixUsineStr}\n\n" +
                                   $"Prix Custom →\t{prixCustomStr}\n\n" +
                                   $"Bénéfice →\t{benefitStr}";
            }
            else
            {
                MessageBox.Show("Impossible de détecter le prix d'usine dans l'image.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string FormatNumberWithSpaces(int value)
        {
            return value.ToString("N0").Replace(",", "\u00A0");
        }

        private string RemoveAfterPeriod(string text) => text.Split('.')[0];

        private int? ExtractPrice(string text)
        {
            foreach (var line in text.Split('\n'))
            {
                if (line.ToLower().Contains("usine"))
                {
                    var price = string.Join("", System.Text.RegularExpressions.Regex.Split(line, @"\D+"));
                    return int.TryParse(price, out int parsed) ? parsed : (int?)null;
                }
            }
            return null;
        }

        private void OnCopyImage(object sender, RoutedEventArgs e)
        {
            if (selectedImage != null)
            {
                Clipboard.SetImage(selectedImage);
                MessageBox.Show("Image copiée dans le presse-papiers.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Aucune image à copier.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnCopyInvoice(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(InvoiceText.Text))
            {
                Clipboard.SetText(InvoiceText.Text);
                MessageBox.Show("Facture copiée dans le presse-papiers.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Aucune facture à copier.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent(DataFormats.Bitmap))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files.Length > 0)
                    {
                        string file = files[0];
                        if (File.Exists(file) && IsImageFile(file))
                        {
                            selectedImage = new BitmapImage(new Uri(file, UriKind.Absolute));
                            ProcessSelectedImage();
                        }
                    }
                }
                else if (e.Data.GetDataPresent(DataFormats.Bitmap))
                {
                    selectedImage = Clipboard.GetImage();
                    ProcessSelectedImage();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du glisser-déposer de l'image : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool IsImageFile(string filePath)
        {
            string ext = System.IO.Path.GetExtension(filePath).ToLower();
            return ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".gif";
        }
    }
}
