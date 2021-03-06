﻿using LightBuzz.Vitruvius;
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
    public const int medianFilterRange = 6;
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

        //JointType _start1 = JointType.ShoulderRight;
        //JointType _center1 = JointType.ElbowRight;
        //JointType _end1 = JointType.WristRight;

        //JointType _start2 = JointType.ElbowLeft;
        //JointType _center2 = JointType.ShoulderLeft;
        //JointType _end2 = JointType.SpineShoulder;

        //JointType _start3 = JointType.AnkleRight;
        //JointType _center3 = JointType.KneeRight;
        //JointType _end3 = JointType.HipRight;

        List<double> arrayShoulder = new List<double>();
        List<double> arrayElbow = new List<double>();
        List<double> arrayWrist = new List<double>();
        List<double> arrayHand = new List<double>();
        List<double> arrayShoulder_median_filter = new List<double>();
        List<double> arrayElbow_median_filter = new List<double>();
        List<double> arrayWrist_median_filter = new List<double>();
        List<double> arrayShoulder_send = new List<double>();
        double hand_status;
        int lasso_counter;
        int shoulder_counter;
        List<long> runTime = new List<long>();
        int medStart;
        int medEnd;
        private TcAdsClient _tcClient;
        private AdsStream adsWriteStream;
        private AdsStream adsReadStream;

        public AnglePage()
        {
            InitializeComponent();
            ConnectAds();

            lasso_counter = 0;
            shoulder_counter = 0;
            medStart = 0;
            medEnd = medStart + Constants.medianFilterRange;
            
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

        private void adsSendShoulder(double shoulderLevelShift)
        {
            //adsReadStream = new AdsStream(40);
            adsWriteStream = new AdsStream(sizeof(double));
            AdsBinaryWriter binWriter = new AdsBinaryWriter(adsWriteStream);
            adsWriteStream.Position = 0;

            binWriter.Write(shoulderLevelShift);

            _tcClient.ReadWrite(0x1, 0x2, null, adsWriteStream);
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
                    //body.HandLeftState;

                    if (body != null)
                    {
                        viewer.DrawBody(body);

                        Joint spine = body.Joints[JointType.SpineShoulder];
                        Joint shoulder = body.Joints[JointType.ShoulderRight];
                        Joint elbow = body.Joints[JointType.ElbowRight];
                        Joint wrist = body.Joints[JointType.WristRight];
                        Joint hand = body.Joints[JointType.HandRight];

                        angle2.Update(shoulder, elbow, wrist, 50);
                        angle1.Update(spine, shoulder, elbow, 50);
                        angle3.Update(elbow, wrist, hand, 50);

                        double angleShoulder = shoulder.Angle(spine, elbow, Axis.Y);
                        double angleElbow = elbow.Angle(shoulder, wrist, Axis.Y);
                        double angleWrist = wrist.Angle(elbow, hand, Axis.Y);

                        tblAngle1.Text = ((int)angleShoulder).ToString();
                        tblAngle2.Text = ((int)angleElbow).ToString();
                        tblAngle3.Text = ((int)angleWrist).ToString();
                        tblAngle5.Text = body.HandRightState.ToString();

                        //原始数据
                        arrayShoulder.Add(angleShoulder);
                        arrayElbow.Add(angleElbow);
                        arrayWrist.Add(angleWrist);
                        arrayHand.Add(body.HandRightState.GetHashCode());
                        
                        if ((body.HandRightState == HandState.Open) ||
                            (body.HandRightState == HandState.Closed))
                        {
                            hand_status = body.HandRightState.GetHashCode();
                            tblAngle4.Text = body.HandRightState.ToString();
                        }
                        if (body.HandRightState == HandState.Lasso)
                        {
                            lasso_counter++;
                            if(lasso_counter >= 10)//TODO:考虑清零的问题
                            {
                                hand_status = body.HandRightState.GetHashCode();
                                tblAngle4.Text = body.HandRightState.ToString();
                                tblAngle6.Text = "L";
                                lasso_counter = 0;
                            }
                        }

                        runTime.Add(DateTime.Now.Ticks);//1Ticks = 0.0001毫秒

                        if (arrayShoulder.Count > Constants.medianFilterRange)
                        {
                            List<double> tempShoulder = arrayShoulder.GetRange(medStart, Constants.medianFilterRange);
                            List<double> tempElbow = arrayElbow.GetRange(medStart, Constants.medianFilterRange);
                            List<double> tempWrist = arrayWrist.GetRange(medStart, Constants.medianFilterRange);
                            ++medStart;
                            ++medEnd;

                            tempShoulder.Sort();
                            tempElbow.Sort();
                            tempWrist.Sort();

                            double shoulderData = tempShoulder[Constants.medianFilterRange / 2];
                            double elbowData = tempElbow[Constants.medianFilterRange / 2];
                            double wristData = tempWrist[Constants.medianFilterRange / 2];

                            if (arrayShoulder_median_filter.Count != 0)
                            {
                                double diff_shoulder_data = 0;
                                double diff_elbow_data = 0;
                                double diff_wrist_data = 0;

                                double last_shoulder = arrayShoulder_median_filter[arrayShoulder_median_filter.Count - 1];
                                double last_elbow = arrayElbow_median_filter[arrayElbow_median_filter.Count - 1];
                                double last_wrist = arrayWrist_median_filter[arrayWrist_median_filter.Count - 1];

                                diff_shoulder_data = shoulderData - last_shoulder;
                                diff_elbow_data = elbowData - last_elbow;
                                diff_wrist_data = wristData - last_wrist;

                                //对肘关节、腕关节差值。Kinect采集是30hz，机械臂是100hz.这里把采集角度变化四等分，弄成假120hz，有偏差。
                                for(int i = 1; i < 5; ++i)
                                {
                                    arrayElbow_median_filter.Add(last_elbow + diff_elbow_data * i/4);
                                    arrayWrist_median_filter.Add(last_wrist + diff_wrist_data * i/4);
                                }
                                //for (int i = 1; i < 11; ++i)
                                //{
                                //    arrayShoulder_median_filter.Add(last_shoulder + diff_shoulder_data * i / 4);
                                //}
                                arrayShoulder_median_filter.Add(shoulderData);
                            }
                            else//首次数据不等分
                            {
                                arrayShoulder_median_filter.Add(shoulderData);
                                arrayElbow_median_filter.Add(elbowData);
                                arrayWrist_median_filter.Add(wristData);
                            }

                            adsSendData(shoulderData, elbowData, wristData, hand_status);

                            if (arrayElbow_median_filter.Count == 1)
                            {
                                adsSendShoulder(shoulderData);
                                arrayShoulder_send.Add(shoulderData);
                            }

                            if (shoulder_counter == 30)
                            {
                                shoulder_counter = 0;
                                double last_angle = arrayShoulder_median_filter[arrayShoulder_median_filter.Count - 31];
                                double err_angle = arrayShoulder_median_filter[arrayShoulder_median_filter.Count - 1] - last_angle;
                                if (Math.Abs(err_angle) > 30)
                                {
                                    int piece = (int)Math.Abs(err_angle) / 30 + 1;    //分割份数
                                    for (int i = 0; i < piece; ++i)
                                    {
                                        adsSendShoulder(last_angle + err_angle/piece * (i + 1));
                                        arrayShoulder_send.Add(last_angle + err_angle / piece * (i + 1));
                                    }
                                }
                                else
                                {
                                    adsSendShoulder(shoulderData);
                                    arrayShoulder_send.Add(shoulderData);
                                }
                            }
                            ++shoulder_counter;
                        }
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
