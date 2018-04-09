using LightBuzz.Vitruvius;
using Microsoft.Kinect;
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

namespace LightBuzz.Vituvius.Samples.WPF
{
    /// <summary>
    /// Interaction logic for PoseMatchingPage.xaml
    /// </summary>
    public partial class PoseMatchingPage : Page
    {
        private KinectSensor _sensor;
        private MultiSourceFrameReader _reader;
        private PoseMatching _matching;
        private ViewMode _mode;

        private Body _currentBody;
        private BodyWrapper _capturedBody;

        public PoseMatchingPage()
        {
            InitializeComponent();

            _sensor = KinectSensor.GetDefault();

            if (_sensor != null)
            {
                _sensor.Open();

                _reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.Infrared | FrameSourceTypes.Body);
                _reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

                _matching = new PoseMatching
                {
                    CheckHead = false,
                    CheckLegLeft = false,
                    CheckLegRight = false,
                    CheckArmLeft = true,
                    CheckArmRight = true,
                    CheckSpine = false
                };

                _mode = ViewMode.Capture;
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_reader != null)
            {
                _reader.Dispose();
            }

            if (_sensor != null)
            {
                _sensor.Close();
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }

        void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var reference = e.FrameReference.AcquireFrame();

            // Color
            using (var frame = reference.ColorFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if (_mode == ViewMode.Capture)
                    {
                        viewer1.Image = frame.ToBitmap();
                    }
                    else
                    {
                        viewer1.Image.ClearValue(Image.SourceProperty);
                        viewer2.Image = frame.ToBitmap();
                    }
                }
            }

            // Body
            using (var frame = reference.BodyFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    _currentBody = frame.Bodies().Closest();

                    if (_currentBody != null)
                    {
                        if (_mode == ViewMode.Capture)
                        {
                            viewer1.DrawBody(_currentBody, 4.0, Brushes.White, 4.0, Brushes.White);
                        }
                        else
                        {
                            var match = _matching.Matches(_capturedBody, _currentBody);
                            var brush = match ? Brushes.Green : Brushes.Red;

                            viewer2.Clear();
                            viewer2.DrawBody(_currentBody, 4.0, brush, 4.0, brush);
                        }
                    }
                }
            }
        }

        private void Capture_Click(object sender, RoutedEventArgs e)
        {
            if (_mode == ViewMode.Capture && _currentBody != null)
            {
                _capturedBody = _currentBody.ToBodyWrapper();

                _mode = ViewMode.Compare;
            }
        }
    }

    enum ViewMode
    {
        Capture,
        Compare
    }
}
