using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ToastFish.Model.RandomSchedule;

namespace ToastFish.View
{
    /// <summary>
    /// RandomScheduleSettings.xaml 的交互逻辑
    /// </summary>
    public partial class RandomScheduleSettings : Window, INotifyPropertyChanged
    {
        private ScheduleConfig _config;
        private RandomScheduler _scheduler;
        private DispatcherTimer _statusUpdateTimer;

        public RandomScheduleSettings(ScheduleConfig config, RandomScheduler scheduler)
        {
            InitializeComponent();
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            
            DataContext = this;
            InitializeStatusTimer();
            UpdateStatus();
        }

        #region 属性绑定

        public new bool IsEnabled
        {
            get => _config.IsEnabled;
            set
            {
                _config.IsEnabled = value;
                OnPropertyChanged(nameof(IsEnabled));
                UpdateStatus();
            }
        }

        public string StartTimeString
        {
            get => _config.StartTime.ToString(@"hh\:mm");
            set
            {
                if (TimeSpan.TryParseExact(value, @"hh\:mm", CultureInfo.InvariantCulture, out var time))
                {
                    _config.StartTime = time;
                    OnPropertyChanged(nameof(StartTimeString));
                }
            }
        }

        public string EndTimeString
        {
            get => _config.EndTime.ToString(@"hh\:mm");
            set
            {
                if (TimeSpan.TryParseExact(value, @"hh\:mm", CultureInfo.InvariantCulture, out var time))
                {
                    _config.EndTime = time;
                    OnPropertyChanged(nameof(EndTimeString));
                }
            }
        }

        public int MinIntervalMinutes
        {
            get => _config.MinIntervalMinutes;
            set
            {
                _config.MinIntervalMinutes = value;
                OnPropertyChanged(nameof(MinIntervalMinutes));
            }
        }

        public int MaxIntervalMinutes
        {
            get => _config.MaxIntervalMinutes;
            set
            {
                _config.MaxIntervalMinutes = value;
                OnPropertyChanged(nameof(MaxIntervalMinutes));
            }
        }

        public bool IsDoNotDisturbEnabled
        {
            get => _config.IsDoNotDisturbEnabled;
            set
            {
                _config.IsDoNotDisturbEnabled = value;
                OnPropertyChanged(nameof(IsDoNotDisturbEnabled));
            }
        }

        public string DoNotDisturbStartString
        {
            get => _config.DoNotDisturbStart.ToString(@"hh\:mm");
            set
            {
                if (TimeSpan.TryParseExact(value, @"hh\:mm", CultureInfo.InvariantCulture, out var time))
                {
                    _config.DoNotDisturbStart = time;
                    OnPropertyChanged(nameof(DoNotDisturbStartString));
                }
            }
        }

        public string DoNotDisturbEndString
        {
            get => _config.DoNotDisturbEnd.ToString(@"hh\:mm");
            set
            {
                if (TimeSpan.TryParseExact(value, @"hh\:mm", CultureInfo.InvariantCulture, out var time))
                {
                    _config.DoNotDisturbEnd = time;
                    OnPropertyChanged(nameof(DoNotDisturbEndString));
                }
            }
        }

        public int WordCount
        {
            get => _config.WordCount;
            set
            {
                _config.WordCount = value;
                OnPropertyChanged(nameof(WordCount));
            }
        }

        #endregion

        private void InitializeStatusTimer()
        {
            _statusUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _statusUpdateTimer.Tick += (s, e) => UpdateStatus();
            _statusUpdateTimer.Start();
        }

        private void UpdateStatus()
        {
            try
            {
                // 更新当前时间
                CurrentTimeTextBlock.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                if (_config.IsEnabled && _scheduler.IsRunning)
                {
                    StatusTextBlock.Text = "运行中";
                    StatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                    StartStopButton.Content = "停止";
                    StartStopButton.IsEnabled = true;
                    TestButton.IsEnabled = true;

                    var nextTime = _scheduler.GetNextScheduledTime();
                    if (nextTime.HasValue)
                    {
                        NextTimeTextBlock.Text = nextTime.Value.ToString("MM-dd HH:mm:ss");

                        // 显示倒计时
                        var remaining = nextTime.Value - DateTime.Now;
                        if (remaining.TotalSeconds > 0)
                        {
                            NextTimeTextBlock.Text += $" (还有 {remaining.TotalMinutes:F0} 分钟)";
                        }
                    }
                    else
                    {
                        NextTimeTextBlock.Text = "计算中...";
                    }
                }
                else if (_config.IsEnabled && !_scheduler.IsRunning)
                {
                    StatusTextBlock.Text = "已启用但未运行";
                    StatusTextBlock.Foreground = System.Windows.Media.Brushes.Orange;
                    StartStopButton.Content = "启动";
                    StartStopButton.IsEnabled = true;
                    TestButton.IsEnabled = true;
                    NextTimeTextBlock.Text = "--";
                }
                else
                {
                    StatusTextBlock.Text = "未启用";
                    StatusTextBlock.Foreground = System.Windows.Media.Brushes.Gray;
                    StartStopButton.Content = "启动";
                    StartStopButton.IsEnabled = false;
                    TestButton.IsEnabled = false;
                    NextTimeTextBlock.Text = "--";
                }

                TestButton.IsEnabled = _config.IsEnabled;
                StartStopButton.IsEnabled = _config.IsEnabled;

                // 显示时间段状态
                if (_config.IsEnabled)
                {
                    var inActiveTime = _config.IsInActiveTimeRange();
                    var inDoNotDisturb = _config.IsInDoNotDisturbTime();

                    if (inDoNotDisturb)
                    {
                        StatusTextBlock.Text += " (勿扰中)";
                    }
                    else if (!inActiveTime)
                    {
                        StatusTextBlock.Text += " (非活动时间)";
                    }
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"错误: {ex.Message}";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                NextTimeTextBlock.Text = "--";
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 手动更新配置（确保UI的值被保存）
                UpdateConfigFromUI();

                // 验证输入
                if (!ValidateInputs())
                {
                    return;
                }

                // 保存配置到文件
                _config.SaveToFile();

                System.Diagnostics.Debug.WriteLine($"配置已保存: 启用={_config.IsEnabled}");

                // 重启调度器以应用新配置
                _scheduler.Restart();

                MessageBox.Show("设置已保存！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存设置时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateConfigFromUI()
        {
            // 确保所有UI控件的值都更新到配置对象
            _config.IsEnabled = EnableCheckBox.IsChecked ?? false;
            _config.IsDoNotDisturbEnabled = DoNotDisturbCheckBox.IsChecked ?? false;

            // 更新时间设置
            if (TimeSpan.TryParseExact(StartTimeTextBox.Text, @"hh\:mm", CultureInfo.InvariantCulture, out var startTime))
                _config.StartTime = startTime;

            if (TimeSpan.TryParseExact(EndTimeTextBox.Text, @"hh\:mm", CultureInfo.InvariantCulture, out var endTime))
                _config.EndTime = endTime;

            // 更新间隔设置
            if (int.TryParse(MinIntervalTextBox.Text, out var minInterval))
                _config.MinIntervalMinutes = minInterval;

            if (int.TryParse(MaxIntervalTextBox.Text, out var maxInterval))
                _config.MaxIntervalMinutes = maxInterval;

            // 更新勿扰时间
            if (TimeSpan.TryParseExact(DoNotDisturbStartTextBox.Text, @"hh\:mm", CultureInfo.InvariantCulture, out var dndStart))
                _config.DoNotDisturbStart = dndStart;

            if (TimeSpan.TryParseExact(DoNotDisturbEndTextBox.Text, @"hh\:mm", CultureInfo.InvariantCulture, out var dndEnd))
                _config.DoNotDisturbEnd = dndEnd;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 显示调试信息
                var debugInfo = $"调度器状态: {(_scheduler.IsRunning ? "运行中" : "已停止")}\n";
                debugInfo += $"配置启用: {_config.IsEnabled}\n";
                debugInfo += $"当前时间: {DateTime.Now:HH:mm:ss}\n";
                debugInfo += $"活动时间段: {_config.StartTime:hh\\:mm} - {_config.EndTime:hh\\:mm}\n";
                debugInfo += $"在活动时间: {_config.IsInActiveTimeRange()}\n";
                debugInfo += $"在勿扰时间: {_config.IsInDoNotDisturbTime()}\n";
                debugInfo += $"当前词库: {ToastFish.Model.SqliteControl.Select.TABLE_NAME ?? "未设置"}\n";

                System.Diagnostics.Debug.WriteLine("=== 测试抽背调试信息 ===");
                System.Diagnostics.Debug.WriteLine(debugInfo);

                _scheduler.TriggerManualRecitation();
                MessageBox.Show($"测试抽背已触发！\n\n调试信息:\n{debugInfo}", "测试结果", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"测试抽背失败: {ex.Message}\n\n堆栈跟踪:\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_scheduler.IsRunning)
                {
                    _scheduler.Stop();
                    MessageBox.Show("随机抽背已停止", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    if (!_config.IsEnabled)
                    {
                        MessageBox.Show("请先启用随机抽背功能", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    _scheduler.Start();
                    MessageBox.Show("随机抽背已启动", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                UpdateStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"操作失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInputs()
        {
            // 验证时间格式
            if (!TimeSpan.TryParseExact(StartTimeTextBox.Text, @"hh\:mm", CultureInfo.InvariantCulture, out _))
            {
                MessageBox.Show("开始时间格式不正确，请使用 HH:mm 格式（如 09:00）", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                StartTimeTextBox.Focus();
                return false;
            }

            if (!TimeSpan.TryParseExact(EndTimeTextBox.Text, @"hh\:mm", CultureInfo.InvariantCulture, out _))
            {
                MessageBox.Show("结束时间格式不正确，请使用 HH:mm 格式（如 17:00）", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                EndTimeTextBox.Focus();
                return false;
            }

            // 验证间隔时间
            if (!int.TryParse(MinIntervalTextBox.Text, out var minInterval) || minInterval < 1)
            {
                MessageBox.Show("最小间隔必须是大于0的整数", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                MinIntervalTextBox.Focus();
                return false;
            }

            if (!int.TryParse(MaxIntervalTextBox.Text, out var maxInterval) || maxInterval < minInterval)
            {
                MessageBox.Show("最大间隔必须大于等于最小间隔", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                MaxIntervalTextBox.Focus();
                return false;
            }

            // 验证勿扰时间
            if (IsDoNotDisturbEnabled)
            {
                if (!TimeSpan.TryParseExact(DoNotDisturbStartTextBox.Text, @"hh\:mm", CultureInfo.InvariantCulture, out _))
                {
                    MessageBox.Show("勿扰开始时间格式不正确", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    DoNotDisturbStartTextBox.Focus();
                    return false;
                }

                if (!TimeSpan.TryParseExact(DoNotDisturbEndTextBox.Text, @"hh\:mm", CultureInfo.InvariantCulture, out _))
                {
                    MessageBox.Show("勿扰结束时间格式不正确", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    DoNotDisturbEndTextBox.Focus();
                    return false;
                }
            }

            return true;
        }

        private void DebugButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var debugInfo = GetDetailedDebugInfo();

                // 创建一个简单的调试信息窗口
                var debugWindow = new Window
                {
                    Title = "随机抽背调试信息",
                    Width = 600,
                    Height = 400,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Content = new ScrollViewer
                    {
                        Content = new TextBlock
                        {
                            Text = debugInfo,
                            Margin = new Thickness(10),
                            FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                            FontSize = 12
                        }
                    }
                };

                debugWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"显示调试信息失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetDetailedDebugInfo()
        {
            var info = new System.Text.StringBuilder();
            info.AppendLine("=== ToastFish 随机抽背调试信息 ===");
            info.AppendLine($"时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            info.AppendLine();

            info.AppendLine("【配置信息】");
            info.AppendLine($"功能启用: {_config.IsEnabled}");
            info.AppendLine($"抽背时间段: {_config.StartTime:hh\\:mm} - {_config.EndTime:hh\\:mm}");
            info.AppendLine($"随机间隔: {_config.MinIntervalMinutes} - {_config.MaxIntervalMinutes} 分钟");
            info.AppendLine($"勿扰模式: {_config.IsDoNotDisturbEnabled}");
            if (_config.IsDoNotDisturbEnabled)
            {
                info.AppendLine($"勿扰时间: {_config.DoNotDisturbStart:hh\\:mm} - {_config.DoNotDisturbEnd:hh\\:mm}");
            }
            info.AppendLine();

            info.AppendLine("【运行状态】");
            info.AppendLine($"调度器运行: {_scheduler.IsRunning}");
            info.AppendLine($"当前在活动时间段: {_config.IsInActiveTimeRange()}");
            info.AppendLine($"当前在勿扰时间段: {_config.IsInDoNotDisturbTime()}");

            var nextTime = _scheduler.GetNextScheduledTime();
            if (nextTime.HasValue)
            {
                info.AppendLine($"下次抽背时间: {nextTime.Value:yyyy-MM-dd HH:mm:ss}");
                var remaining = nextTime.Value - DateTime.Now;
                info.AppendLine($"剩余时间: {remaining.TotalMinutes:F1} 分钟");
            }
            else
            {
                info.AppendLine("下次抽背时间: 未安排");
            }
            info.AppendLine();

            info.AppendLine("【词库信息】");
            info.AppendLine($"当前词库: {ToastFish.Model.SqliteControl.Select.TABLE_NAME ?? "未设置"}");
            info.AppendLine();

            info.AppendLine("【系统信息】");
            info.AppendLine($"操作系统: {Environment.OSVersion}");
            info.AppendLine($"程序版本: ToastFish v2.1.1 - 自定义版本");
            info.AppendLine($"配置文件: RandomScheduleConfig.json");

            return info.ToString();
        }

        protected override void OnClosed(EventArgs e)
        {
            _statusUpdateTimer?.Stop();
            base.OnClosed(e);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
