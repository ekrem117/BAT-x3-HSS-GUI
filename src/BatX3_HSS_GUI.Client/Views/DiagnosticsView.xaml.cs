using BatX3_HSS_GUI.Client.ViewModels.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace BatX3_HSS_GUI.Client.Views
{
    /// <summary>
    /// Interaction logic for DiagnosticsView.xaml
    /// </summary>
    public partial class DiagnosticsView :
       UserControl
    {
        public DiagnosticsView()
        {
            InitializeComponent();
        }

        private void OnIsVisibleChanged(
            object sender,
            DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible)
            {
                return;
            }

            if (DataContext is not DiagnosticsViewModel viewModel)
            {
                return;
            }

            viewModel.RefreshConfiguration();
        }
    }
}
