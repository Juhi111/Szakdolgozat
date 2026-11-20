using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SqlPlanVisualizer.Desktop.ViewModels;

namespace SqlPlanVisualizer.Desktop.Views
{
    public partial class DatabaseConnectionWindow : Window
    {
        private readonly DatabaseConnectionViewModel _viewModel;

        private CancellationTokenSource? _testCancellation;
        private bool _isClosed;
        private bool _profilesInitialized;

        public DatabaseConnectionWindow(DatabaseConnectionViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            InitializeComponent();

            _viewModel = viewModel;
            DataContext = viewModel;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_profilesInitialized)
                return;
            _profilesInitialized = true;
            await _viewModel.LoadProfilesAsync();
        }

        private void Profiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Másik profilhoz soha ne használjuk fel az előzőleg beírt jelszót.
            ConnectionPasswordBox?.Clear();
        }

        private void NewProfile_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.IsBusy)
                return;
            _viewModel.NewProfile();
            ConnectionPasswordBox.Clear();
        }

        private async void ReloadProfiles_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.IsBusy)
                return;
            ConnectionPasswordBox.Clear();
            await _viewModel.LoadProfilesAsync();
        }

        private async void SaveProfile_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.IsBusy || Validation.GetHasError(PortTextBox))
                return;
            await _viewModel.SaveProfileAsync();
        }

        private async void DeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            if (!_viewModel.CanDeleteProfile)
                return;
            string name = _viewModel.SelectedProfile!.Name;
            var answer = MessageBox.Show(this, $"Törlöd a(z) „{name}” mentett kapcsolatot?", "Kapcsolati profil törlése", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes)
                return;
            await _viewModel.DeleteProfileAsync();
            if (!_isClosed)
                ConnectionPasswordBox.Clear();
        }

        private async void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            await RunConnectionActionAsync(useConnection: false);
        }

        private async void UseConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            await RunConnectionActionAsync(useConnection: true);
        }        

        private async Task RunConnectionActionAsync(bool useConnection)
        {
            if (_isClosed ||
                _viewModel.IsBusy ||
                Validation.GetHasError(PortTextBox))
            {
                return;
            }

            using var cancellation = new CancellationTokenSource();
            _testCancellation = cancellation;

            try
            {
                string password = ConnectionPasswordBox.Password;
                bool success;

                if (useConnection)
                {
                    success = await _viewModel.UseConnectionAsync(
                        password,
                        cancellation.Token);
                }
                else
                {
                    success = await _viewModel.TestConnectionAsync(
                        password,
                        cancellation.Token);
                }

                if (success &&
                    useConnection &&
                    !_isClosed &&
                    !cancellation.IsCancellationRequested)
                {
                    DialogResult = true;
                }
            }
            finally
            {
                _testCancellation = null;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _isClosed = true;

            _testCancellation?.Cancel();
            ConnectionPasswordBox.Clear();

            base.OnClosed(e);
        }

    }
}