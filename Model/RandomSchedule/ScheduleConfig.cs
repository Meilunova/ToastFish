using System;
using System.ComponentModel;
using System.IO;
using Newtonsoft.Json;

namespace ToastFish.Model.RandomSchedule
{
    /// <summary>
    /// 随机抽背调度配置类
    /// </summary>
    public class ScheduleConfig : INotifyPropertyChanged
    {
        private bool _isEnabled = false;
        private TimeSpan _startTime = new TimeSpan(9, 0, 0);  // 9:00
        private TimeSpan _endTime = new TimeSpan(17, 0, 0);   // 17:00
        private int _minIntervalMinutes = 10;
        private int _maxIntervalMinutes = 60;
        private bool _isDoNotDisturbEnabled = false;
        private TimeSpan _doNotDisturbStart = new TimeSpan(12, 0, 0);  // 12:00
        private TimeSpan _doNotDisturbEnd = new TimeSpan(13, 0, 0);    // 13:00
        private int _wordCount = 1;  // 每次抽背的单词数量，默认1个

        /// <summary>
        /// 是否启用随机抽背
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                OnPropertyChanged(nameof(IsEnabled));
            }
        }

        /// <summary>
        /// 抽背开始时间
        /// </summary>
        public TimeSpan StartTime
        {
            get => _startTime;
            set
            {
                _startTime = value;
                OnPropertyChanged(nameof(StartTime));
            }
        }

        /// <summary>
        /// 抽背结束时间
        /// </summary>
        public TimeSpan EndTime
        {
            get => _endTime;
            set
            {
                _endTime = value;
                OnPropertyChanged(nameof(EndTime));
            }
        }

        /// <summary>
        /// 最小间隔时间（分钟）
        /// </summary>
        public int MinIntervalMinutes
        {
            get => _minIntervalMinutes;
            set
            {
                _minIntervalMinutes = Math.Max(1, value);
                OnPropertyChanged(nameof(MinIntervalMinutes));
            }
        }

        /// <summary>
        /// 最大间隔时间（分钟）
        /// </summary>
        public int MaxIntervalMinutes
        {
            get => _maxIntervalMinutes;
            set
            {
                _maxIntervalMinutes = Math.Max(_minIntervalMinutes, value);
                OnPropertyChanged(nameof(MaxIntervalMinutes));
            }
        }

        /// <summary>
        /// 是否启用勿扰模式
        /// </summary>
        public bool IsDoNotDisturbEnabled
        {
            get => _isDoNotDisturbEnabled;
            set
            {
                _isDoNotDisturbEnabled = value;
                OnPropertyChanged(nameof(IsDoNotDisturbEnabled));
            }
        }

        /// <summary>
        /// 勿扰开始时间
        /// </summary>
        public TimeSpan DoNotDisturbStart
        {
            get => _doNotDisturbStart;
            set
            {
                _doNotDisturbStart = value;
                OnPropertyChanged(nameof(DoNotDisturbStart));
            }
        }

        /// <summary>
        /// 勿扰结束时间
        /// </summary>
        public TimeSpan DoNotDisturbEnd
        {
            get => _doNotDisturbEnd;
            set
            {
                _doNotDisturbEnd = value;
                OnPropertyChanged(nameof(DoNotDisturbEnd));
            }
        }

        /// <summary>
        /// 每次抽背的单词数量
        /// </summary>
        public int WordCount
        {
            get => _wordCount;
            set
            {
                _wordCount = Math.Max(1, Math.Min(20, value)); // 限制在1-20之间
                OnPropertyChanged(nameof(WordCount));
            }
        }

        /// <summary>
        /// 检查当前时间是否在抽背时间段内
        /// </summary>
        public bool IsInActiveTimeRange()
        {
            var now = DateTime.Now.TimeOfDay;
            
            // 处理跨天的情况
            if (StartTime <= EndTime)
            {
                return now >= StartTime && now <= EndTime;
            }
            else
            {
                return now >= StartTime || now <= EndTime;
            }
        }

        /// <summary>
        /// 检查当前时间是否在勿扰时间段内
        /// </summary>
        public bool IsInDoNotDisturbTime()
        {
            if (!IsDoNotDisturbEnabled) return false;
            
            var now = DateTime.Now.TimeOfDay;
            
            // 处理跨天的情况
            if (DoNotDisturbStart <= DoNotDisturbEnd)
            {
                return now >= DoNotDisturbStart && now <= DoNotDisturbEnd;
            }
            else
            {
                return now >= DoNotDisturbStart || now <= DoNotDisturbEnd;
            }
        }

        /// <summary>
        /// 生成下一次抽背的随机间隔（毫秒）
        /// </summary>
        public int GetNextRandomInterval()
        {
            var random = new Random();
            var intervalMinutes = random.Next(MinIntervalMinutes, MaxIntervalMinutes + 1);
            return intervalMinutes * 60 * 1000; // 转换为毫秒
        }

        /// <summary>
        /// 获取到下一个有效抽背时间的延迟（毫秒）
        /// </summary>
        public int GetDelayToNextActiveTime()
        {
            var now = DateTime.Now;
            var today = now.Date;
            
            // 如果当前在有效时间段内且不在勿扰时间内，返回随机间隔
            if (IsInActiveTimeRange() && !IsInDoNotDisturbTime())
            {
                return GetNextRandomInterval();
            }
            
            // 计算下一个有效时间点
            DateTime nextActiveTime;
            
            if (IsInDoNotDisturbTime())
            {
                // 在勿扰时间内，等到勿扰结束
                nextActiveTime = today.Add(DoNotDisturbEnd);
                if (nextActiveTime <= now)
                {
                    nextActiveTime = nextActiveTime.AddDays(1);
                }
            }
            else if (now.TimeOfDay < StartTime)
            {
                // 在今天的开始时间之前
                nextActiveTime = today.Add(StartTime);
            }
            else if (now.TimeOfDay > EndTime)
            {
                // 在今天的结束时间之后，等到明天
                nextActiveTime = today.AddDays(1).Add(StartTime);
            }
            else
            {
                // 其他情况，使用随机间隔
                return GetNextRandomInterval();
            }
            
            var delay = (int)(nextActiveTime - now).TotalMilliseconds;
            return Math.Max(1000, delay); // 至少延迟1秒
        }

        /// <summary>
        /// 保存配置到文件
        /// </summary>
        public void SaveToFile(string filePath = "RandomScheduleConfig.json")
        {
            try
            {
                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存配置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 从文件加载配置
        /// </summary>
        public static ScheduleConfig LoadFromFile(string filePath = "RandomScheduleConfig.json")
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    return JsonConvert.DeserializeObject<ScheduleConfig>(json) ?? new ScheduleConfig();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载配置失败: {ex.Message}");
            }
            return new ScheduleConfig();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
