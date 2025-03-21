using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using ThuCuaThangMay.Comm;
using ThuCuaThangMay.DataObjects;

namespace ThuCuaThangMay
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SettingsDO Settings = new SettingsDO();

        private readonly S71200MoPhong1 _s7conn = new("127.0.0.1");
        private bool _isConnect = true;
        private CommStates _state0 = CommStates.Closed;

        private DispatcherTimer _UITimer;
        private long _t0;

        private PlotModel _plotModel;
        private LineSeries _lineSeries;
        private long _data_t0;
        private bool _isStart0 = false;
        private bool _isForward0 = true;
        private bool _docDuLieu = false;

        public MainWindow()
        {
            InitializeComponent();

            _plotModel = new PlotModel { Title = "Real-Time Data" };
            _lineSeries = new LineSeries { Title = "Weight", Color = OxyColors.Red };
            InitChart();

            _UITimer = new()
            {
                Interval = TimeSpan.FromMilliseconds(100),
            };
            _UITimer.Tick += _UITimer_Tick;

            _s7conn.StateChanged += _s7conn_StateChanged;

            lblIPAddr.Text = _s7conn.IPAddress;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _t0 = DateTime.Now.Ticks;
            _UITimer.Start();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _UITimer?.Stop();
            _s7conn.Disconnect();
        }

        private void _s7conn_StateChanged(object? sender, EventArgs e)
        {
            if (_s7conn.State != _state0)
            {
                _state0 = _s7conn.State;
                if (_isConnect)
                {
                    switch (_state0)
                    {
                        case CommStates.Closed:
                            lblConnStatus.Text = "Không kết nối";
                            bdrConnStatus.Background = Brushes.OrangeRed;
                            break;
                        case CommStates.Openning:
                            lblConnStatus.Text = "Đang kết nối";
                            bdrConnStatus.Background = Brushes.Yellow;
                            break;
                        case CommStates.Opened:
                            lblConnStatus.Text = "Kết nối";
                            bdrConnStatus.Background = Brushes.LightGreen;
                            break;
                    }
                }
                else
                {
                    lblConnStatus.Text = "Dừng kết nối";
                    bdrConnStatus.Background = Brushes.Wheat;
                }
            }
            if (_s7conn.Db.IsStart)
            {
                BtStart.Background = Brushes.LightGreen;
            }
            else
            {
                BtStart.Background = Brushes.LightGray;
            }
        }

        private void _UITimer_Tick(object? sender, EventArgs e)
        {
            long t = DateTime.Now.Ticks;
            double delta = (t - _t0) / 10000;
            _t0 = t;

            if (_isConnect)
            {
                _s7conn.Connect(delta);
                if (_s7conn.IsCommunicating)
                {
                    lblReadTime.Text = $"{_s7conn.ReadTime / 10000} / {_s7conn.RealCycleTime / 10000} ms";

                    #region Hiển thị giao diện
                    var db = _s7conn.Db;

                    lblSpeed.Text = db.Spd.ToString();
                    lblDistance.Text = db.Distance.ToString();

                    LEDStart.IsOn = db.IsStart;
                    LEDStop.IsOn = db.IsStop;
                    LEDForward.IsOn = db.IsForward;

                    LEDRunning.IsOn = db.IsRunning;

                    if (_isStart0 != db.IsStart)
                    {
                        if (db.IsStart)
                        {
                            _lineSeries.Points.Clear();
                            _data_t0 = 0;
                        }
                        _docDuLieu = db.IsStart;
                        _isStart0 = db.IsStart;
                    }

                    if (_isForward0 != db.IsForward)
                    {
                        BtBackward.Content = db.IsForward ? "Lùi" : "Tiến";
                        _isForward0 = db.IsForward;
                    }

                    if (_docDuLieu)
                    {
                        if (db.T > _data_t0)
                        {
                            _lineSeries.Points.Add(new DataPoint(db.T / 10000000d, db.Distance));
                            _t0 = db.T;
                            _plotModel.InvalidatePlot(true);
                        }
                    }
                    #endregion
                }
                else
                {
                    if (_s7conn.State == CommStates.Opened)
                    {
                        _s7conn.StartComm();
                    }
                }
            }
        }

        private void BtConnect_Click(object sender, RoutedEventArgs e)
        {
            _isConnect = !_isConnect;
            _s7conn.DelayConnect = 0;
            if (!_isConnect && _s7conn.State == CommStates.Closed)
            {
                lblConnStatus.Text = "Dừng kết nối";
                bdrConnStatus.Background = Brushes.Wheat;
            }
            else if (_s7conn.State == CommStates.Opened)
            {
                _s7conn.Disconnect();
            }
        }

        private void BtStart_Click(object sender, RoutedEventArgs e)
        {
            _s7conn.PushStart();
        }

        private void BtStop_Click(object sender, RoutedEventArgs e)
        {
            _s7conn.PushStop();
        }

        #region OxyPlot
        private void InitChart()
        {
            // X-Axis (Time in Seconds)
            _plotModel.Axes.Add(new TimeSpanAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Time (Seconds)",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                Maximum = 600,
            });

            // Y-Axis (Numeric Value)
            _plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Sensor Value",
                Minimum = -1,  // Example range for sine wave
                Maximum = 15
            });

            _plotModel.Series.Add(_lineSeries);
            Plot1.Model = _plotModel;
        }
        #endregion

        private void BtBackward_Click(object sender, RoutedEventArgs e)
        {
            _s7conn.PushBackward();
        }
    }
}