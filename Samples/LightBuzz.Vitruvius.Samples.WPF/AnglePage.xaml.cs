using LightBuzz.Vitruvius;
using Microsoft.Kinect;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Collections.Generic;
using System;
using TwinCAT.Ads;

static class Constants
{
    public const int shoulder = 1;
    public const int elbow = 2;
    public const int wrist = 3;
    public const int hand = 4;

}

namespace LightBuzz.Vituvius.Samples.WPF
{
    /// <summary>
    /// Interaction logic for AnglePage.xaml
    /// </summary>
    public partial class AnglePage : Page
    {
        KinectSensor _sensor;
        MultiSourceFrameReader _reader;
        PlayersController _playersController;

        JointType _start1 = JointType.ShoulderRight;
        JointType _center1 = JointType.ElbowRight;
        JointType _end1 = JointType.WristRight;

        JointType _start2 = JointType.ElbowLeft;
        JointType _center2 = JointType.ShoulderLeft;
        JointType _end2 = JointType.SpineShoulder;

        JointType _start3 = JointType.AnkleRight;
        JointType _center3 = JointType.KneeRight;
        JointType _end3 = JointType.HipRight;

        List<double> arrayShoulder = new List<double>();
        List<double> arrayElbow = new List<double>();
        List<double> arrayWrist = new List<double>();
        List<long> runTime = new List<long>();
        private TcAdsClient _tcClient;
        private AdsStream adsWriteStream;
        private AdsStream adsReadStream;

        public AnglePage()
        {
            InitializeComponent();
            ConnectAds();

            _sensor = KinectSensor.GetDefault();

            if (_sensor != null)
            {
                _sensor.Open();

                _reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.Infrared | FrameSourceTypes.Body);
                _reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

                _playersController = new PlayersController();
                _playersController.BodyEntered += UserReporter_BodyEntered;
                _playersController.BodyLeft += UserReporter_BodyLeft;
                _playersController.Start();
            }
        }

        private void ConnectAds()
        {
            _tcClient = new TcAdsClient();

            string netid = "5.39.221.128.1.1";
            int net_port = 0x8888;
            AmsAddress serverAddress = new AmsAddress(netid, net_port); //配置服务端地址
           _tcClient.Connect(serverAddress.NetId, serverAddress.Port);  //连接服务端
        }

        private void adsSendData(double shoulderLevelShift, double elbow, double wrist, double hand)
        {
            adsReadStream = new AdsStream(40);
            adsWriteStream = new AdsStream(sizeof(double)*4);
            AdsBinaryWriter binWriter = new AdsBinaryWriter(adsWriteStream);
            adsWriteStream.Position = 0;

            binWriter.Write(shoulderLevelShift);
            binWriter.Write(elbow);
            binWriter.Write(wrist);
            binWriter.Write(hand);

            _tcClient.ReadWrite(0x1, 0x1, null, adsWriteStream);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_playersController != null)
            {
                _playersController.Stop();
            }

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
                    if (viewer.Visualization == Visualization.Color)
                    {
                        viewer.Image = frame.ToBitmap();
                    }
                }
            }

            // Body
            using (var frame = reference.BodyFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    var bodies = frame.Bodies();

                    _playersController.Update(bodies);

                    Body body = bodies.Closest();

                    if (body != null)
                    {
                        viewer.DrawBody(body);

                        Joint spine = body.Joints[JointType.SpineShoulder];
                        Joint shoulder = body.Joints[JointType.ShoulderRight];
                        Joint elbow = body.Joints[JointType.ElbowRight];
                        Joint wrist = body.Joints[JointType.WristRight];
                        Joint hand = body.Joints[JointType.HandRight];

                        angle1.Update(shoulder, elbow, wrist, 50);
                        angle2.Update(spine, shoulder, elbow, 50);
                        angle3.Update(elbow, wrist, hand, 50);

                        double angleShoulder = shoulder.Angle(spine, elbow, Axis.Y);
                        double angleElbow = elbow.Angle(shoulder, wrist, Axis.Y);
                        double angleWrist = wrist.Angle(elbow, hand, Axis.Y);

                        tblAngle1.Text = ((int)angleElbow).ToString();
                        tblAngle2.Text = ((int)angleShoulder).ToString();
                        tblAngle3.Text = ((int)angleWrist).ToString();

                        arrayShoulder.Add(angleShoulder);
                        arrayElbow.Add(angleElbow);
                        arrayWrist.Add(angleWrist);

                        runTime.Add(DateTime.Now.Ticks);//1Ticks = 0.0001毫秒

                        adsSendData(angleShoulder, angleElbow, angleWrist, 1);
                    }
                }
            }
        }

        void UserReporter_BodyEntered(object sender, PlayersControllerEventArgs e)
        {
        }

        void UserReporter_BodyLeft(object sender, PlayersControllerEventArgs e)
        {
            viewer.Clear();
            angle1.Clear();
            angle2.Clear();
            angle3.Clear();

            tblAngle1.Text = "-";
            tblAngle2.Text = "-";
            tblAngle3.Text = "-";
        }
    }
}
