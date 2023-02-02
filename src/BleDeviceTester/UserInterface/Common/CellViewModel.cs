#nullable disable

using IDS.UI.Interfaces;
using PrismExtensions.ViewModels;
using RvLinkDeviceTester.ViewModels.Base.Navigation;
using System;
using System.Windows.Input;
using static OneControl.UserInterface.Common.CellViewModel;

namespace OneControl.UserInterface.Common
{
    public class CellViewModel : BaseViewModel, ICellViewModel
    {
        private string _text;
        public string Text
        {
            get => _text;
            set => SetProperty(ref _text, value);
        }

        private string _description;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private bool _isEnabled = true;
        public virtual bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        private bool _showInformationIcon = false;
        public virtual bool ShowInformationIcon
        {
            get => _showInformationIcon;
            set => SetProperty(ref _showInformationIcon, value);
        }

        private ICommand _cellCommand;
        public ICommand CellCommand
        {
            get => _cellCommand;
            set => SetProperty(ref _cellCommand, value);
        }

        public ICommand? InfoCommand { get; }

        private bool _isFavorite;
        public bool IsFavorite
        {
            get => _isFavorite;
            set => SetProperty(ref _isFavorite, value);
        }

        public virtual string Category { get; } = string.Empty;

        public CellViewModel(INavigationProxy navigationProxy) : base((Prism.Navigation.INavigationService)navigationProxy) { }
        public CellViewModel(string text) : this(text, null, "") { }

        public CellViewModel(string text, string description, string category = "") : base(null)
        {
            _text = text;
            _description = description;
            Category = category;
        }

        //: this(null, text, description, category) { }

        //public CellViewModel(INavigationProxy navigationProxy) : this(navigationProxy, "", "") { }

        //public CellViewModel(INavigationProxy navigationProxy, string text) : this(navigationProxy, text, "") { }

        //public CellViewModel(INavigationProxy navigationProxy, string text, string description, string category = "")
        //    : base(navigationProxy)
        //{
        //    _text = text;
        //    _description = description;
        //    Category = category;
        //}

        public int CompareTo(object obj) => (obj is ICellViewModel other)
            ? CompareTo(other)
            : 1;

        public int CompareTo(ICellViewModel other)
        {
            if (other is null)
                return 1;

            var cp = string.Compare(Text, other.Text, StringComparison.CurrentCulture);

            return cp != 0
                ? cp
                : string.Compare(Description, other.Description, StringComparison.CurrentCulture);
        }
    }

    public class CellViewModel<TCustomData> : CellViewModel
    {
        public TCustomData CustomData { get; }

        public CellViewModel(TCustomData customData, INavigationProxy navigationProxy, string text, string description = "")
            : base(navigationProxy)
        {
            CustomData = customData;
            Text = text;
            Description = description;
        }

        public CellViewModel(TCustomData customData, string text, string description = "")
            : base(text, description)
        {
            CustomData = customData;
        }
    }
}
