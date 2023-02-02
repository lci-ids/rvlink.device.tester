#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using IDS.Portable.Common;
using Prism.Mvvm;

namespace RvLinkDeviceTester.UserInterface.Common
{
    public abstract class BaseViewModel : BindableBase
    {
        protected virtual string LogTag => GetType().Name;

        private readonly bool _verbose = false;

        protected void RaisePropertyChangedOnMainThread(string propertyName)
        {
            MainThread.RequestMainThreadAction(() => {
                RaisePropertyChanged(propertyName);
            });
        }

        #region Notification/Backing field
        protected bool SetBackingField<TValue>(ref TValue field, TValue value, [CallerMemberName] string notifyPropertyName = "", params string[] notifyPropertyNameEnumeration)
        {
            if (EqualityComparer<TValue>.Default.Equals(field, value))
                return false;

            field = value;

            if (!string.IsNullOrEmpty(notifyPropertyName))
                RaisePropertyChangedOnMainThread(notifyPropertyName);

            if (notifyPropertyNameEnumeration == null)
                return true;

            MainThread.RequestMainThreadAction(() => {
                foreach (var notifyAdditionalPropertyName in notifyPropertyNameEnumeration)
                {
                    if (!string.IsNullOrEmpty(notifyAdditionalPropertyName))
                        RaisePropertyChanged(notifyAdditionalPropertyName);
                }
            });

            return true;
        }

        protected void RaiseAllPropertiesChangedOnMainThread()
            => RaiseAllPropertiesChanged(this, true);

        protected void RaiseAllPropertiesChanged()
            => RaiseAllPropertiesChanged(this, false);

        private static readonly Dictionary<Type, List<string>> Properties = new Dictionary<Type, List<string>>();
        private static void RaiseAllPropertiesChanged(BaseViewModel vm, bool raiseOnMainThread)
        {
            var type = vm.GetType();
            List<string> properties;
            lock (Properties)
            {
                if (!Properties.TryGetValue(type, out properties))
                {
                    properties = type.GetProperties()
                        .Select(pi => pi.Name)
                        .ToList();

                    Properties.Add(type, properties);
                }
            }

            if (raiseOnMainThread)
                MainThread.RequestMainThreadAction(() => RaiseAllPropertiesChanged(vm, properties));
            else
                RaiseAllPropertiesChanged(vm, properties);
        }

        private static void RaiseAllPropertiesChanged(BaseViewModel vm, List<string> properties)
        {
            foreach (var property in properties)
                vm.RaisePropertyChanged(property);
        }
        #endregion

        #region Debug/Error Messages
        protected void VerboseDebug(string message)
        {
            if (_verbose)
                TaggedLog.Debug(LogTag, message);
        }

        protected void VerboseDebug(string source, string message)
        {
            if (_verbose)
                TaggedLog.Debug(LogTag, $"{source}: {message}");
        }

        protected void Debug(string message)
            => TaggedLog.Debug(LogTag, message);

        protected void Debug(string source, string message)
            => TaggedLog.Debug(LogTag, $"{source}: {message}");

        protected void Information(string message)
            => TaggedLog.Information(LogTag, message);

        protected void Information(string source, string message)
            => TaggedLog.Information(LogTag, $"{source}: {message}");

        protected void Warning(string message)
            => TaggedLog.Warning(LogTag, message);

        protected void Warning(string source, string message)
            => TaggedLog.Warning(LogTag, $"{source}: {message}");

        protected void Error(string message)
            => TaggedLog.Error(LogTag, message);

        protected void Error(string source, string message)
            => TaggedLog.Error(LogTag, $"{source}: {message}");
        #endregion
    }
}
