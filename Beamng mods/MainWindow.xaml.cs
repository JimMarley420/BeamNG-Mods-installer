using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;

namespace BeamNGModsInstaller
{
    public partial class MainWindow : Window
    {
        private const string VersionUrl = "http://passw.wuaze.com/version.txt"; // URL vers le fichier version.txt
        private string InstallerUrl = ""; // URL de téléchargement direct à récupérer depuis le fichier version.txt
        private const string CurrentVersion = "1.0.5"; // Version actuelle de l'application

        private string modsDirectory;

        public MainWindow()
        {
            InitializeComponent();
            FindModsFolder();
            UpdateModsList();
            StartModsWatcher();
        }

        // Recherche du dossier mods dans le sous-dossier versionné
        private void FindModsFolder()
        {
            string userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string beamngBasePath = Path.Combine(userProfilePath, "AppData", "Local", "BeamNG.drive");

            if (Directory.Exists(beamngBasePath))
            {
                // Trouver le sous-dossier avec la version la plus récente
                string[] versionFolders = Directory.GetDirectories(beamngBasePath);
                string latestVersionFolder = versionFolders
                    .Where(f => Directory.Exists(Path.Combine(f, "mods")))
                    .OrderByDescending(f => f)
                    .FirstOrDefault();

                if (!string.IsNullOrEmpty(latestVersionFolder))
                {
                    modsDirectory = Path.Combine(latestVersionFolder, "mods");
                }
                else
                {
                    MessageBox.Show("Aucun dossier 'mods' trouvé.");
                }
            }
            else
            {
                MessageBox.Show("Dossier BeamNG.drive introuvable.");
            }
        }

        // Mettre à jour la liste des mods présents dans le dossier mods
        private void UpdateModsList()
        {
            if (modsDirectory != null && Directory.Exists(modsDirectory))
            {
                ModsListBox.Items.Clear();
                string[] mods = Directory.GetFiles(modsDirectory, "*.zip");
                foreach (string mod in mods)
                {
                    ModsListBox.Items.Add(Path.GetFileName(mod));
                }
            }
        }

        // Démarrer le FileSystemWatcher pour surveiller les changements dans le dossier mods
        private void StartModsWatcher()
        {
            if (modsDirectory != null)
            {
                FileSystemWatcher watcher = new FileSystemWatcher
                {
                    Path = modsDirectory,
                    Filter = "*.zip",
                    EnableRaisingEvents = true
                };

                watcher.Created += (s, e) => Dispatcher.Invoke(() => UpdateModsList());
                watcher.Deleted += (s, e) => Dispatcher.Invoke(() => UpdateModsList());
            }
        }

        // Supprimer le mod sélectionné
        private void OnDeleteModClick(object sender, RoutedEventArgs e)
        {
            string selectedMod = ModsListBox.SelectedItem as string;
            if (selectedMod != null)
            {
                string modFilePath = Path.Combine(modsDirectory, selectedMod);
                try
                {
                    File.Delete(modFilePath);
                    MessageBox.Show($"Mod '{selectedMod}' supprimé avec succès !");
                    UpdateModsList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de la suppression : {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner un mod à supprimer.");
            }
        }

        // Gestion du DragOver (éviter l'icône de blocage)
        private void Window_DragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        // Gestion du Drop pour récupérer le fichier déposé
        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (modsDirectory == null)
            {
                MessageBox.Show("Le dossier 'mods' n'a pas été trouvé. Vérifiez l'installation de BeamNG.");
                return;
            }

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                string file = files.FirstOrDefault();

                if (file != null && file.EndsWith(".zip"))
                {
                    try
                    {
                        string destinationPath = Path.Combine(modsDirectory, Path.GetFileName(file));
                        File.Copy(file, destinationPath, true);
                        MessageBox.Show($"Mod installé avec succès dans {modsDirectory} !");
                        UpdateModsList();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erreur lors de l'installation : {ex.Message}");
                    }
                }
                else
                {
                    MessageBox.Show("Le fichier déposé n'est pas un fichier .zip. Veuillez déposer un fichier .zip valide.");
                }
            }
        }

        // Méthode pour vérifier les mises à jour
        private void OnCheckUpdateClick(object sender, RoutedEventArgs e)
        {
            try
            {
                WebClient webClient = new WebClient();
                string[] versionInfo = webClient.DownloadString(VersionUrl).Split('\n');
                string onlineVersion = versionInfo[0].Trim();
                InstallerUrl = versionInfo[1].Trim(); // Récupérer le lien direct vers le fichier .zip

                if (IsNewerVersion(onlineVersion, CurrentVersion))
                {
                    MessageBox.Show($"Nouvelle version disponible : {onlineVersion}. Téléchargement de la mise à jour...");
                    DownloadAndOpenZip();
                }
                else
                {
                    MessageBox.Show("Votre application est déjà à jour.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la vérification des mises à jour : {ex.Message}");
            }
        }

        // Compare les versions pour savoir si une mise à jour est disponible
        private bool IsNewerVersion(string onlineVersion, string currentVersion)
        {
            Version vOnline = new Version(onlineVersion);
            Version vCurrent = new Version(currentVersion);
            return vOnline > vCurrent;
        }

        // Télécharge le fichier .zip et l'ouvre
        private void DownloadAndOpenZip()
        {
            try
            {
                // Chemin temporaire pour le fichier .zip
                string zipFilePath = Path.Combine(Path.GetTempPath(), "update.zip");

                // Téléchargement du fichier .zip
                WebClient webClient = new WebClient();
                webClient.DownloadFile(InstallerUrl, zipFilePath);

                // Ouvrir le fichier .zip
                Process.Start("explorer.exe", zipFilePath);

                // Fermer l'application
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du téléchargement ou de l'ouverture : {ex.Message}");
            }
        }


        private void BackupMod(string modName)
        {
            string modPath = Path.Combine(modsDirectory, modName);
            string backupFolder = Path.Combine(modsDirectory, "backup");
            string backupPath = Path.Combine(backupFolder, modName);

            // Créer le dossier de sauvegarde s'il n'existe pas
            if (!Directory.Exists(backupFolder))
            {
                Directory.CreateDirectory(backupFolder);
            }

            if (File.Exists(modPath))
            {
                File.Copy(modPath, backupPath, true);
                MessageBox.Show($"Mod sauvegardé dans {backupPath}");
            }
            else
            {
                MessageBox.Show("Le mod sélectionné n'a pas été trouvé pour la sauvegarde.");
            }
        }

        private long GetTotalModsSize()
        {
            long totalSize = 0;

            // Obtenir tous les fichiers zip dans le dossier des mods
            string[] mods = Directory.GetFiles(modsDirectory, "*.zip");

            // Additionner la taille de chaque fichier
            foreach (string mod in mods)
            {
                FileInfo fileInfo = new FileInfo(mod);
                totalSize += fileInfo.Length;
            }

            return totalSize;
        }


        private void OnRestoreModClick(object sender, RoutedEventArgs e)
        {
            string selectedMod = ModsListBox.SelectedItem as string;
            if (!string.IsNullOrEmpty(selectedMod))
            {
                RestoreMod(selectedMod);
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner un mod à restaurer.");
            }
        }

        private void GetModStatistics()
        {
            int totalMods = Directory.GetFiles(modsDirectory, "*.zip").Length;
            long totalSize = GetTotalModsSize();
            MessageBox.Show($"Nombre total de mods : {totalMods}\nTaille totale : {totalSize / (1024 * 1024)} Mo");
        }

        private void GetModStatisticsClick(object sender, RoutedEventArgs e)
        {
            GetModStatistics();
        }


        private void OnBackupModClick(object sender, RoutedEventArgs e)
        {
            string selectedMod = ModsListBox.SelectedItem as string;
            if (!string.IsNullOrEmpty(selectedMod))
            {
                BackupMod(selectedMod);
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner un mod à restaurer.");
            }
        }

        private void RestoreMod(string modName)
        {
            string backupPath = Path.Combine(modsDirectory, "backup", modName);
            if (File.Exists(backupPath))
            {
                string restorePath = Path.Combine(modsDirectory, modName);
                File.Copy(backupPath, restorePath, true);
                MessageBox.Show($"Mod restauré depuis {backupPath}");
                UpdateModsList();
            }
            else
            {
                MessageBox.Show("Aucune sauvegarde trouvée.");
            }
        }

        // Installer un mod via le chemin donné dans l'input
        private void OnInstallModViaPathClick(object sender, RoutedEventArgs e)
        {
            string modFilePath = ModFilePathTextBox.Text;

            if (File.Exists(modFilePath) && modFilePath.EndsWith(".zip"))
            {
                try
                {
                    string destinationPath = Path.Combine(modsDirectory, Path.GetFileName(modFilePath));
                    File.Copy(modFilePath, destinationPath, true);
                    MessageBox.Show($"Mod installé avec succès dans {modsDirectory} !");
                    UpdateModsList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'installation : {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Veuillez entrer un chemin valide vers un fichier .zip.");
            }
        }

        private void OnCheckUpdateClick(object sender, object e)
        {

        }
    }
}
