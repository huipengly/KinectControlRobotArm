using LightBuzz.Vitruvius;
using Microsoft.Kinect;
using Microsoft.Kinect.Face;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LightBuzz.Vituvius.Samples.WPF
{
    /// <summary>
    /// Interaction logic for FacePage.xaml
    /// </summary>
    public partial class FacePage : Page
    {
        private KinectSensor _sensor = null;

        private InfraredFrameSource _infraredSource = null;

        private InfraredFrameReader _infraredReader = null;

        private BodyFrameSource _bodySource = null;

        private BodyFrameReader _bodyReader = null;

        private HighDefinitionFaceFrameSource _faceSource = null;

        private HighDefinitionFaceFrameReader _faceReader = null;

        private bool _showAllPoints = false;

        private List<Ellipse> _ellipses = new List<Ellipse>();

        public FacePage()
        {
            InitializeComponent();

            _sensor = KinectSensor.GetDefault();

            if (_sensor != null)
            {
                _infraredSource = _sensor.InfraredFrameSource;
                _infraredReader = _infraredSource.OpenReader();
                _infraredReader.FrameArrived += InfraredReader_FrameArrived;

                _bodySource = _sensor.BodyFrameSource;
                _bodyReader = _bodySource.OpenReader();
                _bodyReader.FrameArrived += BodyReader_FrameArrived; ;

                _faceSource = new HighDefinitionFaceFrameSource(_sensor);
                _faceReader = _faceSource.OpenReader();
                _faceReader.FrameArrived += FaceReader_FrameArrived; ;

                _sensor.Open();
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_faceReader != null)
            {
                _faceReader.Dispose();
            }

            if (_bodyReader != null)
            {
                _bodyReader.Dispose();
            }

            if (_infraredReader != null)
            {
                _infraredReader.Dispose();
            }

            if (_sensor != null)
            {
                _sensor.Close();
            }

            GC.SuppressFinalize(this);
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }

        private void InfraredReader_FrameArrived(object sender, InfraredFrameArrivedEventArgs args)
        {
            using (var frame = args.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    camera.Source = frame.ToBitmap();
                }
            }
        }

        private void BodyReader_FrameArrived(object sender, BodyFrameArrivedEventArgs args)
        {
            using (var frame = args.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    Body body = frame.Bodies().Closest();

                    if (!_faceSource.IsTrackingIdValid)
                    {
                        if (body != null)
                        {
                            _faceSource.TrackingId = body.TrackingId;
                        }
                    }
                }
            }
        }

        private void FaceReader_FrameArrived(object sender, HighDefinitionFaceFrameArrivedEventArgs args)
        {
            using (var frame = args.FrameReference.AcquireFrame())
            {
                if (frame != null && frame.IsFaceTracked)
                {
                    Face face = frame.Face();

                    if (_showAllPoints)
                    {
                        // Display all face points.
                        if (_ellipses.Count == 0)
                        {
                            for (int index = 0; index < face.Vertices.Count; index++)
                            {
                                Ellipse ellipse = new Ellipse
                                {
                                    Width = 2.0,
                                    Height = 2.0,
                                    Fill = new SolidColorBrush(Colors.Orange)
                                };

                                _ellipses.Add(ellipse);

                                canvas.Children.Add(ellipse);
                            }
                        }

                        for (int index = 0; index < face.Vertices.Count; index++)
                        {
                            Ellipse ellipse = _ellipses[index];

                            CameraSpacePoint vertex = face.Vertices[index];
                            PointF point = vertex.ToPoint(Visualization.Infrared);

                            Canvas.SetLeft(ellipse, point.X - ellipse.Width / 2.0);
                            Canvas.SetTop(ellipse, point.Y - ellipse.Height / 2.0);
                        }
                    }
                    else
                    {
                        // Display basic points only.
                        PointF pointEyeLeft = face.EyeLeft.ToPoint(Visualization.Infrared);
                        PointF pointEyeRight = face.EyeRight.ToPoint(Visualization.Infrared);
                        PointF pointCheekLeft = face.CheekLeft.ToPoint(Visualization.Infrared);
                        PointF pointCheekRight = face.CheekRight.ToPoint(Visualization.Infrared);
                        PointF pointNose = face.Nose.ToPoint(Visualization.Infrared);
                        PointF pointMouth = face.Mouth.ToPoint(Visualization.Infrared);
                        PointF pointChin = face.Chin.ToPoint(Visualization.Infrared);
                        PointF pointForehead = face.Forehead.ToPoint(Visualization.Infrared);

                        Canvas.SetLeft(eyeLeft, pointEyeLeft.X - eyeLeft.Width / 2.0);
                        Canvas.SetTop(eyeLeft, pointEyeLeft.Y - eyeLeft.Height / 2.0);

                        Canvas.SetLeft(eyeRight, pointEyeRight.X - eyeRight.Width / 2.0);
                        Canvas.SetTop(eyeRight, pointEyeRight.Y - eyeRight.Height / 2.0);

                        Canvas.SetLeft(cheekLeft, pointCheekLeft.X - cheekLeft.Width / 2.0);
                        Canvas.SetTop(cheekLeft, pointCheekLeft.Y - cheekLeft.Height / 2.0);

                        Canvas.SetLeft(cheekRight, pointCheekRight.X - cheekRight.Width / 2.0);
                        Canvas.SetTop(cheekRight, pointCheekRight.Y - cheekRight.Height / 2.0);

                        Canvas.SetLeft(nose, pointNose.X - nose.Width / 2.0);
                        Canvas.SetTop(nose, pointNose.Y - nose.Height / 2.0);

                        Canvas.SetLeft(mouth, pointMouth.X - mouth.Width / 2.0);
                        Canvas.SetTop(mouth, pointMouth.Y - mouth.Height / 2.0);

                        Canvas.SetLeft(chin, pointChin.X - chin.Width / 2.0);
                        Canvas.SetTop(chin, pointChin.Y - chin.Height / 2.0);

                        Canvas.SetLeft(forehead, pointForehead.X - forehead.Width / 2.0);
                        Canvas.SetTop(forehead, pointForehead.Y - forehead.Height / 2.0);
                    }
                }
            }
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            _showAllPoints = true;

            foreach (Ellipse ellipse in _ellipses)
            {
                ellipse.Visibility = Visibility.Visible;
            }

            eyeLeft.Visibility = Visibility.Collapsed;
            eyeRight.Visibility = Visibility.Collapsed;
            cheekLeft.Visibility = Visibility.Collapsed;
            cheekRight.Visibility = Visibility.Collapsed;
            nose.Visibility = Visibility.Collapsed;
            mouth.Visibility = Visibility.Collapsed;
            chin.Visibility = Visibility.Collapsed;
            forehead.Visibility = Visibility.Collapsed;
        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            _showAllPoints = false;

            foreach (Ellipse ellipse in _ellipses)
            {
                ellipse.Visibility = Visibility.Collapsed;
            }

            eyeLeft.Visibility = Visibility.Visible;
            eyeRight.Visibility = Visibility.Visible;
            cheekLeft.Visibility = Visibility.Visible;
            cheekRight.Visibility = Visibility.Visible;
            nose.Visibility = Visibility.Visible;
            mouth.Visibility = Visibility.Visible;
            chin.Visibility = Visibility.Visible;
            forehead.Visibility = Visibility.Visible;
        }
    }
}
