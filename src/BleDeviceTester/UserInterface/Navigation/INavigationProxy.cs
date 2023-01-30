using System.Collections.Generic;
using System.Threading.Tasks;

namespace RvLinkDeviceTester.ViewModels.Base.Navigation
{
    public enum GoBackRequestedResult
    {
        Succeeded,
        Failed,
        CanGoBack,
        CannotGoBack
    }

    public interface INavigationProxy
    {
        Task NavigateAsync(string path);
        Task NavigateAsync<TParameter>(string path, TParameter parameter, IEnumerable<KeyValuePair<string, object>> extraParameters = null);
        Task<TResult> NavigateAsync<TResult, TParameter>(string path, TParameter parameter, IEnumerable<KeyValuePair<string, object>> extraParameters = null);
        Task<TResult> NavigateAsync<TResult>(string path);
        Task<GoBackRequestedResult> OnGoBackRequested();
        Task<bool> GoBackToRootAsync();
    }

    /// <summary>
    /// Similar to <see cref="INavigationProxy"/>, the purpose of <see cref="INavigationInterceptor"/> is to allow for
    /// verification of navigation requests within unit tests.
    /// </summary>
    /// <remarks>
    /// The reason for introducing a new interface is because, even though <see cref="INavigationProxy"/> allows for testing
    /// various user-flow manager classes (e.g. <see cref="UserInterface.Onboarding.OnboardingManager">OnboardingManager</see>), it is still not possible to test navigation
    /// requests that originates from within classes the derive form <see cref="BaseViewModelPage"/>.
    /// The interceptor is intended to act as a proxy, but also allows us to test GoBackAsync, which is currently a protected
    /// function in <see cref="BaseViewModelPage"/>.
    /// An instance of <see cref="INavigationInterceptor"/> must be provided to <see cref="Services.CrossCuttingServices">CrossCuttingServices</see>
    /// in order to mock for unit testing.
    /// </remarks>
    public interface INavigationInterceptor : INavigationProxy
    {
        Task<bool> GoBackAsync();
        Task<bool> GoBackAsync<TResultOut>(TResultOut result);
    }
}
