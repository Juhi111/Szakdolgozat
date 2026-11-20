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

        public DatabaseConnectionWindow(
            DatabaseConnectionViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            InitializeComponent();

            _viewModel = viewModel;
            DataContext = viewModel;
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