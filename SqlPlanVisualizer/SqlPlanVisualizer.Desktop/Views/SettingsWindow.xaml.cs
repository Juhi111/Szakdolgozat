using Microsoft.Win32;
using SqlPlanVisualizer.Core.Models;
using SqlPlanVisualizer.Infrastructure.FileSystem;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace SqlPlanVisualizer.Desktop.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly UserSettingsService settingsService = new();

        public SettingsWindow()
        {
            InitializeComponent();
        }

        private async void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                DataContext = await settingsService.LoadAsync();
                SettingsPanel.IsEnabled = true;
                SaveButton.IsEnabled = true;
            }
            catch (Exception ex) when (
                ex is IOException or UnauthorizedAccessException or JsonException)
            {
                ResetButton.Visibility = Visibility.Visible;
                MessageBox.Show(this,
                    $"Nem sikerült betölteni a beállításokat.\n\n{ex.Message}",
                    "Betöltési hiba",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(this,
                "Visszaállítod az alapbeállításokat?\n\n" +
                "A jelenlegi beállításfájlról biztonsági másolat készül. " +
                "Az automatikus mentés kikapcsol, a mentési mappa az alapértelmezett lesz. " +
                "A korábbi előzményfájlok megmaradnak.",
                "Beállítások helyreállítása", MessageBoxButton.YesNo,
                MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes)
                return;

            ResetButton.IsEnabled = false;
            try
            {
                var result = await settingsService.ResetAsync();
                DataContext = result.Settings;
                SettingsPanel.IsEnabled = true;
                SaveButton.IsEnabled = true;
                ResetButton.Visibility = Visibility.Collapsed;

                string backup = result.BackupPath is null
                    ? string.Empty
                    : $"\n\nBiztonsági másolat: {result.BackupPath}";
                MessageBox.Show(this, "Az alapbeállítások visszaállítása elkészült." + backup,
                    "Beállítások helyreállítása", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) when (
                ex is IOException or UnauthorizedAccessException or JsonException)
            {
                MessageBox.Show(this,
                    $"Nem sikerült visszaállítani a beállításokat.\n\n{ex.Message}",
                    "Helyreállítási hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ResetButton.IsEnabled = true;
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not UserSettings settings)
                return;

            settings.HistoryDirectory = settings.HistoryDirectory.Trim();

            if (string.IsNullOrWhiteSpace(settings.HistoryDirectory) ||
                !Path.IsPathFullyQualified(settings.HistoryDirectory))
            {
                MessageBox.Show(this,
                    "Adj meg egy teljes elérési utat az előzmények mentéséhez.",
                    "Hiányos beállítás",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            SettingsPanel.IsEnabled = false;
            SaveButton.IsEnabled = false;

            try
            {
                await settingsService.SaveAsync(settings);
                DialogResult = true;
            }
            catch (Exception ex) when (
                ex is IOException or UnauthorizedAccessException or JsonException)
            {
                MessageBox.Show(this,
                    $"Nem sikerült elmenteni a beállításokat.\n\n{ex.Message}",
                    "Mentési hiba",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                SettingsPanel.IsEnabled = true;
                SaveButton.IsEnabled = true;
            }
        }

        private void BrowseHistoryDirectory_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not UserSettings settings)
                return;

            var dialog = new OpenFolderDialog
            {
                Title = "Előzmények mentési mappájának kiválasztása",
                Multiselect = false
            };

            if (Directory.Exists(settings.HistoryDirectory))
                dialog.InitialDirectory = settings.HistoryDirectory;

            if (dialog.ShowDialog(this) != true)
                return;

            HistoryDirectoryTextBox.SetCurrentValue(
                System.Windows.Controls.TextBox.TextProperty,
                dialog.FolderName);

            HistoryDirectoryTextBox
                .GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)
                ?.UpdateSource();
        }
    }
}