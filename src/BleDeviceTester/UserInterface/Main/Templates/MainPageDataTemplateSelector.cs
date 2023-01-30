#nullable enable
using System;
using System.Diagnostics;
using Xamarin.Forms;

namespace RvLinkDeviceTester.UserInterface
{
    public class MainPageDataTemplateSelector : DataTemplateSelector
    {
        // We should not be instantiating data templates on each call to OnSelectTemplate
        // https://docs.microsoft.com/en-us/xamarin/xamarin-forms/app-fundamentals/templates/data-templates/selector
        // 
        // The DataTemplateSelector subclass must not return new instances of a DataTemplate on each call.
        // Instead, the same instance must be returned. Failure to do so will create a memory leak and will disable virtualization.
        // 

        private readonly DataTemplate _defaultCellTemplate = new(() => new DefaultCell());

        protected override DataTemplate? OnSelectTemplate(object item, BindableObject container)
        {
            try
            {
                return item switch
                {
                    _ => _defaultCellTemplate //Placeholder so we see items in the collection until we get other templates in place
                };
            }
            catch (Exception ex)
            {
                Debug.Write(ex.Message);
            }

            return new DataTemplate();
        }
    }
}