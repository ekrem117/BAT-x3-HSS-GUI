using BatX3_HSS_GUI.Client.ViewModels.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BatX3_HSS_GUI.Client.Views
{
    /// <summary>
    /// Interaction logic for ParametersView.xaml
    /// </summary>
    public partial class ParametersView :
        UserControl
    {
        public ParametersView()
        {
            InitializeComponent();
        }

        private async void OnLoaded(
            object sender,
            RoutedEventArgs e)
        {
            if (DataContext is not
                ParametersViewModel viewModel)
            {
                return;
            }

            try
            {
                await viewModel.LoadConfigurationOnceAsync();
            }
            catch
            {
                
            }
        }
    }
}
