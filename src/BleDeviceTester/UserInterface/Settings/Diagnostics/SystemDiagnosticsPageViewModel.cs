using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using RvLinkDeviceTester.Resources;
using RvLinkDeviceTester.Services;
using IDS.UI.Interfaces;
using OneControl.UserInterface.Common;
using Prism.Commands;
using Prism.Navigation;
using PrismExtensions.ViewModels;
using Xamarin.CommunityToolkit.ObjectModel;
using Xamarin.Forms;

namespace RvLinkDeviceTester.UserInterface.Settings.Diagnostics
{
    public class SystemDiagnosticsPageViewModel: BaseViewModel, IViewModelConfirmNavigation
    {
        public string Title => Strings.diagnostics_title;

        public ObservableCollection<ICellViewModel>? SystemDiagnosticsCellViewModels { get; private set; }

        private bool _canGoBack = true;
        public bool CanGoBack
        {
            get => _canGoBack;
            set => SetProperty(ref _canGoBack, value);
        }

        private ICommand? _name;
        public ICommand GoBack => _name ??= new AsyncCommand(async () => await NavigationService.GoBackAsync());

        private readonly CellViewModel _emailLogsCellViewModel;
        private readonly CellViewModel _uploadLogsCellViewModel;

        public SystemDiagnosticsPageViewModel(INavigationService navigationService) : base(navigationService)
        {
            SystemDiagnosticsCellViewModels = new ObservableCollection<ICellViewModel>();

            _emailLogsCellViewModel = new CellViewModel(text: Strings.email_logs_title, description: Strings.email_logs_description)
            {
                CellCommand = EmailLogsCommand
            };

            _uploadLogsCellViewModel = new CellViewModel(text: Strings.upload_logs_title, description: Strings.upload_logs_description)
            {
                CellCommand = UploadLogsCommand
            };

            SystemDiagnosticsCellViewModels.Add(_emailLogsCellViewModel);
            SystemDiagnosticsCellViewModels.Add(_uploadLogsCellViewModel);
        }

        public bool ConfirmNavigation(INavigationParameters? parameters) => CanGoBack;

        private ICommand? _emailLogs;
        public ICommand EmailLogsCommand => _emailLogs ??= new Command(() => OnEmailLogsSelected());        

        private void OnEmailLogsSelected()
        {
            DiagnosticReportManager.Instance.SendEmail((state, ex) =>
            {
                if (ex != null)
                {
                    // todo: Shouldn't expose raw ex.Message to users, but this is for developer mode only right now
                    _emailLogsCellViewModel.Description = $"Unable to send {ex.Message}";
                    return;
                }

                switch (state)
                {
                    case DiagnosticReportManager.State.Ready:
                        _emailLogsCellViewModel.Description = "Send logs via e-mail";
                        break;

                    case DiagnosticReportManager.State.GeneratingLog:
                        _emailLogsCellViewModel.Description = "Generating Log File...";
                        break;

                    case DiagnosticReportManager.State.SendingLog:
                        _emailLogsCellViewModel.Description = $"Sending Email";  // NOTE: E-mail doesn't support percent complete!
                        break;
                }
            });
        }

        private ICommand? _uploadLogs;
        public ICommand UploadLogsCommand => _uploadLogs ??= new AsyncCommand(async () => await OnUploadLogsSelectedAsync());
        private async Task OnUploadLogsSelectedAsync()
        {
            await DiagnosticReportManager.Instance.UploadLogAsync((state, percent, ex) =>
            {
                if (ex != null)
                {
                    // todo: Shouldn't expose raw ex.Message to users, but this is for developer mode only right now
                    _uploadLogsCellViewModel.Description = $"Unable to send";
                    return;
                }

                switch (state)
                {
                    case DiagnosticReportManager.State.Ready:
                        _uploadLogsCellViewModel.Description = "Upload logs to LCI";
                        break;

                    case DiagnosticReportManager.State.GeneratingLog:
                        _uploadLogsCellViewModel.Description = "Generating Log File...";
                        break;

                    case DiagnosticReportManager.State.SendingLog:
                        _uploadLogsCellViewModel.Description = $"Sending Log {percent}%";
                        break;
                }
            });
        }
    }
}

