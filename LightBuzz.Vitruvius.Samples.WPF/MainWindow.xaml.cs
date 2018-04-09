using System.Windows;

namespace LightBuzz.Vituvius.Samples.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            frame.Navigate(new AnglePage());
        }

        private void Purchase_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://vitruviuskinect.com");
        }
    }
}
