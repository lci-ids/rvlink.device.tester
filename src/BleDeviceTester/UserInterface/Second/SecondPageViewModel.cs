using System.Windows.Input;
using Prism.Navigation;
using PrismExtensions.ViewModels;
using Xamarin.CommunityToolkit.ObjectModel;

namespace RvLinkDeviceTester.UserInterface.Second
{
    public class SecondPageViewModel : BaseViewModel, IViewModelConfirmNavigation
    {
        private bool _canGoBack = true;
        public bool CanGoBack
        {
            get => _canGoBack;
            set => SetProperty(ref _canGoBack, value);
        }

        private ICommand? _name;
        public ICommand GoBack => _name ??= new AsyncCommand(async () => await NavigationService.GoBackAsync());

        public SecondPageViewModel(INavigationService navigationService) : base(navigationService)
        {
        }

        public bool ConfirmNavigation(INavigationParameters? parameters) => CanGoBack;
    }
}
