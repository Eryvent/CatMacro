using System.Windows;
using System.Windows.Controls;
using CatMacro.ViewModels;

namespace CatMacro
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            try
            {
                InitializeComponent();
                this.DataContext = new MainViewModel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error");
            }
        }

        private void PlaybackSpeed_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is MainViewModel vm && sender is ComboBox cb && cb.SelectedItem is ComboBoxItem item)
            {
                string selected = item.Content?.ToString() ?? "1x";
                vm.PlaybackSpeed = selected switch
                {
                    "0.5x" => 0.5,
                    "1x" => 1.0,
                    "2x" => 2.0,
                    "5x" => 5.0,
                    "10x" => 10.0,
                    _ => 1.0
                };
            }
        }
    }
}
