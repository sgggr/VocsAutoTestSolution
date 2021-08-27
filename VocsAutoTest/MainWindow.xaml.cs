﻿using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using VocsAutoTest.Pages;
using VocsAutoTestBLL.Interface;
using VocsAutoTestBLL.Impl;
using System.Windows.Threading;
using VocsAutoTestCOMM;
using VocsAutoTestBLL;
using VocsAutoTestBLL.Model;
using System.Text;
using VocsAutoTest.Tools;
using System.Collections.Generic;
using System.IO;
using MathWorks.MATLAB.NET.Arrays;
using System.Collections;
using GaussFit;
namespace VocsAutoTest
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        //页面*8
        private AlgoComOne algoPage;
        private AlgoGeneraControlPage algoControlPage;
        private SpecComOne specPage;
        private SpecMeasureControlPage specControlPage;
        private ConcentrationMeasurePage concentrationPage;
        private ConcentrationMeasureControlPage concentrationControlPage;
        private VocsMgmtPage vocsMgmtPage;
        private VocsControlPage vocsControlPage;
        private readonly SpecDataSave specDataSave;
        //日志栏折叠
        private bool isLogBoxOpen = true;
        //日志栏高度
        private double bottomHeight;
        private DispatcherTimer showTimer;
        //当前页面标识 1：光谱采集 2：浓度测量 3：算法生成
        private ushort pageFlag = 1;
        //连续测量
        private MeasureMgrImpl measureMgr;
        //设备号
        private string deviceNo = "";
        public MainWindow()
        {
            InitializeComponent();
            DataForward.Instance.StartService();
            InitBottomInfo();
            measureMgr = MeasureMgrImpl.Instance;
            specDataSave = SpecDataSave.Instance;
            VocsCollectBtn_Click(null, null);
            PassPortImpl.GetInstance().PassValueEvent += new PassPortDelegate(ReceievedValues);
            DataForward.Instance.ReadDeviceNo += new DataForwardDelegate(SetDeviceNo);
            ExceptionUtil.Instance.LogEvent += new ExceptionDelegate(ShowLogMsg);
            ExceptionUtil.Instance.ExceptionEvent += new ExceptionDelegate(ShowExceptionMsg);
            ExceptionUtil.Instance.ShowLoadingAction += ShowLoading;
        }

        /// <summary>
        /// 异常日志保存
        /// </summary>
        /// <param name="msg"></param>
        private void ShowExceptionMsg(string msg, bool isShow)
        {
            Console.WriteLine(msg);
            if (isShow)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    LogUtil.Error(msg, this);
                }));
            }
            else
            {
                Log4NetUtil.Error(msg);
            }
        }
        /// <summary>
        /// 显示日志
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="msg"></param>
        /// <param name="isShow"></param>
        private void ShowLogMsg(string msg, bool isShow)
        {
            Console.WriteLine(msg);
            Dispatcher.BeginInvoke(new Action(() =>
            {
                LogUtil.Debug(msg, this);
            }));
        }
        private void ShowLoading(bool isShow)
        {
            _loading.Visibility = Visibility.Collapsed;
            if (isShow)
                _loading.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 初始化底部信息
        /// 版本号/时间
        /// </summary>
        private void InitBottomInfo()
        {
            //版本号
            this.versionNum.Content = "V1.0";
            //时间
            showTimer = new DispatcherTimer();
            showTimer.Tick += ShowTimer_Tick;
            showTimer.Interval = new TimeSpan(0, 0, 0, 1, 0);
            showTimer.Start();
        }
        private void ShowTimer_Tick(object sender, EventArgs e)
        {
            this.currentTime.Content = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
        }
        /// <summary>
        /// 拖动窗体
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
        /// <summary>
        /// 串口设置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PortBtn_Click(object sender, RoutedEventArgs e)
        {
            PortSettingWindow portSettingWindow = new PortSettingWindow
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            portSettingWindow.Show();
        }
        /// <summary>
        /// 串口接收
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReceievedValues(object sender, PortModel e)
        {

            SuperSerialPort.Instance.Close();
            SuperSerialPort.Instance.SetPortInfo(e.Port, Convert.ToInt32(e.Baud), e.Parity, Convert.ToInt32(e.Data), Convert.ToInt32(e.Stop));
            if (SuperSerialPort.Instance.Open())
            {
                ExceptionUtil.Instance.LogMethod("修改串口信息为：串口号:" + e.Port + "，波特率:" + e.Baud + "，校检:" + e.Parity + "，数据位:" + e.Data + "，停止位:" + e.Stop);
            }
            else
            {
                ExceptionUtil.Instance.ExceptionMethod("修改串口信息失败！", true);
            }
        }
        /// <summary>
        /// 设置光谱仪设备号
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="command"></param>
        private void SetDeviceNo(object sender, Command command)
        {
            byte[] deviceNo = ByteStrUtil.HexToByte(command.Data);
            this.deviceNo = Encoding.Default.GetString(deviceNo).ToUpper();
            MessageBox.Show("Copyright © 天津津普利环保科技股份有限公司\n光谱仪软件版本：" + this.deviceNo, "关于系统", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        /// <summary>
        /// 关于系统
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AboutSysBtn_Click(object sender, RoutedEventArgs e)
        {
            SuperSerialPort.Instance.Send(new Command { Cmn = "25", ExpandCmn = "55", Data = "" });
        }
        /// <summary>
        /// 退出系统
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("是否确定退出系统？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                this.Close();
            }
        }
        /// <summary>
        /// 最大化与正常化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MaxButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Maximized;
            MaxButton.Visibility = Visibility.Collapsed;
            NormalButton.Visibility = Visibility.Visible;
        }
        private void NormalButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Normal;
            NormalButton.Visibility = Visibility.Collapsed;
            MaxButton.Visibility = Visibility.Visible;
        }
        /// <summary>
        /// 最小化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MiniButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        /// <summary>
        /// 连续测量次数可用控制
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MTsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            MTsTextBox.IsEnabled = true;
        }
        private void MTsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            MTsTextBox.IsEnabled = false;
        }
        /// <summary>
        /// 光谱仪管理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VocsMgmtBtn_Click(object sender, RoutedEventArgs e)
        {
            if (measureMgr.StartMeasure)
            {
                MessageBox.Show("请停止测量!");
                return;
            }
            if (vocsMgmtPage == null)
            {
                vocsMgmtPage = new VocsMgmtPage();
            }
            ControlPage.Content = new Frame()
            {
                Content = vocsMgmtPage
            };
            this.tempTextBox.Visibility = Visibility.Hidden;
            this.pressTextBox.Visibility = Visibility.Hidden;
        }
        private void VocsControlBtn_Click(object sender, RoutedEventArgs e)
        {
            if (measureMgr.StartMeasure)
            {
                MessageBox.Show("请停止测量!");
                return;
            }
            if (vocsControlPage == null)
            {
                vocsControlPage = new VocsControlPage();
            }
            ControlPage.Content = new Frame()
            {
                Content = vocsControlPage
            };
            this.tempTextBox.Visibility = Visibility.Hidden;
            this.tempTextBox.Visibility = Visibility.Hidden;
        }
        /// <summary>
        /// 光谱采集
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VocsCollectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (measureMgr.StartMeasure)
            {
                MessageBox.Show("请停止测量再切换页面!");
                return;
            }
            pageFlag = 1;
            if (specPage == null && specControlPage == null)
            {
                specPage = new SpecComOne();
                specControlPage = new SpecMeasureControlPage(specPage);
            }
            ChartPage.Content = new Frame()
            {
                Content = specPage
            };
            ControlPage.Content = new Frame()
            {
                Content = specControlPage
            };
            if (concentrationControlPage != null)
            {
                concentrationControlPage.Stop_Measure();
            }
            this.tempTextBox.Visibility = Visibility.Hidden;
            this.pressTextBox.Visibility = Visibility.Hidden;
        }
        /// <summary>
        /// 浓度测量
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConcentrationMeasureBtn_Click(object sender, RoutedEventArgs e)
        {
            if (measureMgr.StartMeasure)
            {
                MessageBox.Show("请停止测量再切换页面!");
                return;
            }
            pageFlag = 2;
            if (concentrationPage == null && concentrationControlPage == null)
            {
                concentrationPage = new ConcentrationMeasurePage();
                //concentrationPage = new ConcentrationComOne();
                concentrationControlPage = new ConcentrationMeasureControlPage(concentrationPage);
            }
            ChartPage.Content = new Frame()
            {
                Content = concentrationPage
            };
            ControlPage.Content = new Frame()
            {
                Content = concentrationControlPage
            };
            concentrationControlPage.Start_Measure();
            this.tempTextBox.Visibility = Visibility.Visible;
            this.pressTextBox.Visibility = Visibility.Visible;
        }
        /// <summary>
        /// 算法生成
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AlgoGeneraBtn_Click(object sender, RoutedEventArgs e)
        {
            if (measureMgr.StartMeasure)
            {
                MessageBox.Show("请停止测量再切换页面!");
                return;
            }
            pageFlag = 3;
            if (algoPage == null)
            {
                algoPage = new AlgoComOne();
            }
            if (algoControlPage == null)
            {
                algoControlPage = new AlgoGeneraControlPage(algoPage);
            }
            ChartPage.Content = new Frame()
            {
                Content = algoPage
            };
            ControlPage.Content = new Frame()
            {
                Content = algoControlPage
            };
            if (concentrationControlPage != null)
            {
                concentrationControlPage.Stop_Measure();
            }
            this.tempTextBox.Visibility = Visibility.Hidden;
            this.pressTextBox.Visibility = Visibility.Hidden;
        }
        /// <summary>
        /// 隐藏日志信息栏
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HideLogBoxBtn_Click(object sender, RoutedEventArgs e)
        {
            if (isLogBoxOpen)
            {
                bottomHeight = grid.RowDefinitions[4].Height.Value;
                grid.RowDefinitions[4].Height = new GridLength(0);
                isLogBoxOpen = false;
                Image img = (Image)HideLogBoxBtn.Template.FindName("LogBoxImg", HideLogBoxBtn);
                Thickness thickness = new Thickness
                {
                    Bottom = 0,
                    Left = 0,
                    Right = 0,
                    Top = 0
                };
                img.Margin = thickness;
            }
            else
            {
                grid.RowDefinitions[4].Height = new GridLength(bottomHeight);
                isLogBoxOpen = true;
                Image img = (Image)HideLogBoxBtn.Template.FindName("LogBoxImg", HideLogBoxBtn);
                Thickness thickness = new Thickness
                {
                    Bottom = 0,
                    Left = -30,
                    Right = 0,
                    Top = 0
                };
                img.Margin = thickness;
            }
        }
        /// <summary>
        /// 清除日志显示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearLogBoxBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("是否清除所有日志信息？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                this.LogBox.Document.Blocks.Clear();
            }
        }
        /// <summary>
        /// 单次测量
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SingleMeasure_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MTsCheckBox.IsChecked = false;
                measureMgr.measureTimes = 1;
                BeginMeasure(true);
            }
            catch
            {
                MessageBox.Show("参数错误！");
            }
        }
        /// <summary>
        /// 多次测量
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MultiMeasure_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (measureMgr.StartMeasure)
                {
                    measureMgr.StartMeasure = false;
                    SingleMeasure.IsEnabled = true;
                    MultiMeasure.Content = "连续测量";
                    //关闭连续保存
                    if (specDataSave.StartSave)
                    {
                        specControlPage.StartSave_Click(null, null);
                    }
                }
                else
                {
                    measureMgr.StartMeasure = true;
                    SingleMeasure.IsEnabled = false;
                    MultiMeasure.Content = "停止测量";
                    BeginMeasure(false);
                }
            }
            catch
            {
                MessageBox.Show("参数错误！");
                measureMgr.StartMeasure = false;
                SingleMeasure.IsEnabled = true;
                MultiMeasure.Content = "连续测量";
            }
        }
        private void BeginMeasure(bool single)
        {
            measureMgr.endAction += EndMeasure;
            measureMgr.pageFlag = pageFlag;
            measureMgr.measureTimes = 0;
            if (MTsTextBox.IsEnabled)
            {
                measureMgr.measureTimes = Convert.ToInt32(MTsTextBox.Text);
            }
            else if (single)
            {
                measureMgr.measureTimes = 1;
            }
            measureMgr.TimeInterval = int.Parse(ReadInterval.Text);
            switch (pageFlag)
            {
                case 1:
                    measureMgr.specType = DataType.SelectedIndex.ToString();
                    measureMgr.lightPath = lightPath.SelectedIndex.ToString();
                    break;
                case 2:
                    measureMgr.lightPath = lightPath.SelectedIndex.ToString();
                    byte[] tempValues = BitConverter.GetBytes(float.Parse(tempTextBox.Text));
                    byte[] pressValues = BitConverter.GetBytes(float.Parse(pressTextBox.Text));
                    Array.Reverse(tempValues);
                    Array.Reverse(pressValues);
                    measureMgr.tempValues = tempValues;
                    measureMgr.pressValues = pressValues;
                    break;
                case 3:
                    measureMgr.specType = DataType.SelectedIndex.ToString();
                    measureMgr.lightPath = lightPath.SelectedIndex.ToString();
                    break;
            }
            measureMgr.StartMultiMeasure();
        }
        private void EndMeasure(bool beEnd)
        {
            if (beEnd)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    SingleMeasure.IsEnabled = true;
                    MultiMeasure.Content = "连续测量";
                }));
            }
        }

        private void ForwardCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ExceptionUtil.Instance.LogMethod("转发：关");
            SuperSerialPort.Instance.isForward = false;
        }

        private void ForwardCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ExceptionUtil.Instance.LogMethod("转发：开");
            SuperSerialPort.Instance.isForward = true;
        }
    }
}
