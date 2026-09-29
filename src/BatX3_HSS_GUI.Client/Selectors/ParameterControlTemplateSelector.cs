using BatX3_HSS_GUI.Application.Parameters.UI;
using BatX3_HSS_GUI.Client.ViewModels.Parameters;
using System.Windows;
using System.Windows.Controls;

namespace BatX3_HSS_GUI.Client.Selectors
{
    public sealed class ParameterControlTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? NumericParameterTemplate { get; set; }

        public DataTemplate? SelectParameterTemplate { get; set; }

        public DataTemplate? ReadOnlyParameterTemplate { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            if (item is not ParameterItemViewModel parameter)
            {
                return base.SelectTemplate(item, container);
            }

            return parameter.ControlType switch
            {
                ParameterControlType.Numeric => NumericParameterTemplate,

                ParameterControlType.Select => SelectParameterTemplate,

                ParameterControlType.ReadOnly => ReadOnlyParameterTemplate,

                _ => base.SelectTemplate(item, container)
            };
        }
    }
}