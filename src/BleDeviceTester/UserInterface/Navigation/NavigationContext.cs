using System;
using System.Collections.Generic;
using System.Text;
using RvLinkDeviceTester.ViewModels.Base.Navigation;
using IDS.Portable.Common;

namespace RvLinkDeviceTester.UserInterface.Base.Navigation
{
    public interface INavigationContext
    {
        /// <summary>
        /// Navigation proxy of the last resumed page
        /// </summary>
        INavigationProxy CurrentPageNavigationProxy { get; set; }
    }

    public class NavigationContext : Singleton<NavigationContext>, INavigationContext
    {
        private NavigationContext()
        {
        }
        
        public INavigationProxy CurrentPageNavigationProxy { get; set; }
    }
}
