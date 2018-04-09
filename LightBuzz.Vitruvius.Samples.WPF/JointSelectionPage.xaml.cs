using Microsoft.Kinect;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace LightBuzz.Vituvius.Samples.WPF
{
    /// <summary>
    /// Interaction logic for JointSelectionPage.xaml
    /// </summary>
    public partial class JointSelectionPage : Page
    {
        public JointSelectionPage()
        {
            InitializeComponent();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }

        private void JointSelector_JointSelected(object sender, JointType e)
        {
            tblJoint.Text = e.ToString();
        }
    }
}
