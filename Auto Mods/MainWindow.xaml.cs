using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;  // Ajoutez ceci
using Microsoft.Win32;

namespace Auto_Mods
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<Mod> mods;
        private ObservableCollection<Mod> allMods;
        private string modDirectory;

        public MainWindow()
        {
            InitializeComponent();
            mods = new ObservableCollection<Mod>();
            allMods = new ObservableCollection<Mod>();
            ModListView.ItemsSource = mods;
            SetModDirectory();
            LoadMods();
            InstallationProgressBar.Visibility = Visibility.Collapsed;
            InstallationProgressText.Visibility = Visibility.Collapsed;
        }

        private void SetModDirectory()
        {
            // Vérifier si le répertoire des mods est déjà défini dans les paramètres
            modDirectory = Properties.Settings.Default.ModDirectory;
            if (string.IsNullOrEmpty(modDirectory))
            {
                var dialog = new System.Windows.Forms.FolderBrowserDialog(); // Utilisez System.Windows.Forms
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    modDirectory = dialog.SelectedPath;
                    Properties.Settings.Default.ModDirectory = modDirectory;
                    Properties.Settings.Default.Save();
                }
                else
                {
                    MessageBox.Show("Please select a mod directory.");
                    Application.Current.Shutdown();
                }
            }
        }

        private void LoadMods()
        {
            mods.Clear();
            allMods.Clear();
            var modFiles = Directory.GetFiles(modDirectory);
            foreach (var file in modFiles)
            {
                var mod = new Mod
                {
                    ModName = Path.GetFileName(file),
                    ModSize = (new FileInfo(file).Length / 1024) + " KB",
                    InstallDate = File.GetCreationTime(file).ToString("g")
                };

                mods.Add(mod);
                allMods.Add(mod);
            }
        }

        private async void InstallMod(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Zip Files (*.zip)|*.zip",
                Title = "Select a Mod to Install"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string sourcePath = openFileDialog.FileName;
                string destinationPath = Path.Combine(modDirectory, Path.GetFileName(sourcePath));

                InstallationProgressBar.Visibility = Visibility.Visible;
                InstallationProgressText.Visibility = Visibility.Visible;
                InstallationProgressText.Text = "Installing mod...";

                try
                {
                    await Task.Run(() =>
                    {
                        for (int i = 0; i <= 100; i += 10)
                        {
                            Dispatcher.Invoke(() => InstallationProgressBar.Value = i);
                            Thread.Sleep(100);
                        }
                    });

                    File.Copy(sourcePath, destinationPath, true);
                    var newMod = new Mod
                    {
                        ModName = Path.GetFileName(sourcePath),
                        ModSize = (new FileInfo(sourcePath).Length / 1024) + " KB",
                        InstallDate = DateTime.Now.ToString("g")
                    };

                    mods.Add(newMod);
                    allMods.Add(newMod);

                    MessageBox.Show("Mod installed successfully!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error installing mod: " + ex.Message);
                }
                finally
                {
                    InstallationProgressBar.Visibility = Visibility.Collapsed;
                    InstallationProgressText.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void RemoveSelectedMod(object sender, RoutedEventArgs e)
        {
            var selectedMod = ModListView.SelectedItem as Mod;
            if (selectedMod != null)
            {
                string modPath = Path.Combine(modDirectory, selectedMod.ModName);
                if (File.Exists(modPath))
                {
                    File.Delete(modPath);
                    mods.Remove(selectedMod);
                    allMods.Remove(selectedMod);
                    MessageBox.Show($"{selectedMod.ModName} has been removed successfully.");
                }
                else
                {
                    MessageBox.Show("Error: Mod file not found.");
                }
            }
            else
            {
                MessageBox.Show("Please select a mod to remove.");
            }
        }

        private void SearchMods(object sender, RoutedEventArgs e)
        {
            string searchText = SearchBox.Text.ToLower();
            var filteredMods = allMods.Where(mod => mod.ModName.ToLower().Contains(searchText)).ToList();

            mods.Clear();
            foreach (var mod in filteredMods)
            {
                mods.Add(mod);
            }
        }

        private void ResetSearch(object sender, RoutedEventArgs e)
        {
            SearchBox.Clear();
            mods.Clear();
            foreach (var mod in allMods)
            {
                mods.Add(mod);
            }
        }

        private void RefreshModList(object sender, RoutedEventArgs e)
        {
            LoadMods();
        }

        private void CheckForUpdates(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Checking for updates...");
            Task.Delay(2000).ContinueWith(t =>
            {
                Dispatcher.Invoke(() => MessageBox.Show("No updates available."));
            });
        }

        private void ViewModDetails(object sender, RoutedEventArgs e)
        {
            var selectedMod = ModListView.SelectedItem as Mod;
            if (selectedMod != null)
            {
                MessageBox.Show($"Mod Name: {selectedMod.ModName}\nSize: {selectedMod.ModSize}\nInstalled on: {selectedMod.InstallDate}", "Mod Details");
            }
            else
            {
                MessageBox.Show("Please select a mod to view details.");
            }
        }
    }

    public class Mod
    {
        public string ModName { get; set; }
        public string ModSize { get; set; }
        public string InstallDate { get; set; }
    }
}
