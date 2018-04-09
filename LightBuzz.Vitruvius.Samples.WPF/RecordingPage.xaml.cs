using LightBuzz.Vitruvius;
using Microsoft.Kinect;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace LightBuzz.Vituvius.Samples.WPF
{
    /// <summary>
    /// Interaction logic for RecordingPage.xaml
    /// </summary>
    public partial class RecordingPage : Page
    {
        readonly string FOLDER_PATH = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "video");

        KinectSensor _sensor = null;
        MultiSourceFrameReader _reader = null;

        InfraredBitmapGenerator _bitmapGenerator = new InfraredBitmapGenerator();
        VitruviusRecorder _recorder = new VitruviusRecorder();
        VitruviusPlayer _player = new VitruviusPlayer();

        public RecordingPage()
        {
            InitializeComponent();

            _sensor = KinectSensor.GetDefault();

            if (_sensor != null)
            {
                _sensor.Open();

                _reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.Infrared | FrameSourceTypes.Body);
                _reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;
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

        private void Record_Click(object sender, RoutedEventArgs e)
        {
            if (_recorder.IsRecording)
            {
                _recorder.Stop();
            }
            else
            {
                _recorder.Clear();

                _recorder.Visualization = Visualization.Infrared;
                _recorder.Folder = FOLDER_PATH;
                _recorder.Start();
            }
        }

        private void Playback_Click(object sender, RoutedEventArgs e)
        {
            if (_player.IsPlaying)
            {
                _player.FrameArrived -= Player_FrameArrived;
                _player.Stop();
            }
            else
            {
                _player.Folder = FOLDER_PATH;
                _player.FrameArrived += Player_FrameArrived;
                _player.Start();
            }
        }

        private void Player_FrameArrived(object sender, VitruviusFrame frame)
        {
            if (frame != null)
            {
                viewer.Image = frame.Image.ToBitmap(frame.Visualization, PixelFormats.Bgr32);
                viewer.DrawBody(frame.Body);
            }
        }

        void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            if (_recorder.IsRecording)
            {
                var reference = e.FrameReference.AcquireFrame();

                VitruviusFrame recordingFrame = new VitruviusFrame();

                // Infrared
                using (var frame = reference.InfraredFrameReference.AcquireFrame())
                {
                    if (frame != null)
                    {
                        _bitmapGenerator.Update(frame);

                        recordingFrame.Image = _bitmapGenerator.Pixels;

                        viewer.Image = _bitmapGenerator.Bitmap;
                    }
                }

                // Body
                using (var frame = reference.BodyFrameReference.AcquireFrame())
                {
                    if (frame != null)
                    {
                        var body = frame.Bodies().Closest();

                        if (body != null)
                        {
                            recordingFrame.Body = BodyWrapper.Create(body, _sensor.CoordinateMapper, Visualization.Infrared);

                            viewer.DrawBody(body);
                        }
                    }
                }

                _recorder.AddFrame(recordingFrame);
            }
        }
    }
}
