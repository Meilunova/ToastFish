using System;
using System.Threading;
using System.Diagnostics;
using ToastFish.Model.RandomSchedule;

namespace ToastFish.Test
{
    /// <summary>
    /// 随机抽背功能测试类
    /// </summary>
    public class RandomScheduleTest
    {
        /// <summary>
        /// 测试基本配置功能
        /// </summary>
        public static void TestBasicConfiguration()
        {
            Debug.WriteLine("=== 测试基本配置功能 ===");
            
            var config = new ScheduleConfig();
            
            // 测试默认值
            Debug.WriteLine($"默认启用状态: {config.IsEnabled}");
            Debug.WriteLine($"默认开始时间: {config.StartTime}");
            Debug.WriteLine($"默认结束时间: {config.EndTime}");
            Debug.WriteLine($"默认最小间隔: {config.MinIntervalMinutes}分钟");
            Debug.WriteLine($"默认最大间隔: {config.MaxIntervalMinutes}分钟");
            
            // 测试时间范围检查
            var now = DateTime.Now;
            Debug.WriteLine($"当前时间: {now:HH:mm:ss}");
            Debug.WriteLine($"是否在活动时间段: {config.IsInActiveTimeRange()}");
            Debug.WriteLine($"是否在勿扰时间段: {config.IsInDoNotDisturbTime()}");
            
            // 测试随机间隔生成
            for (int i = 0; i < 5; i++)
            {
                var interval = config.GetNextRandomInterval();
                Debug.WriteLine($"随机间隔 {i + 1}: {interval / 1000}秒 ({interval / 60000.0:F1}分钟)");
            }
        }

        /// <summary>
        /// 测试时间段逻辑
        /// </summary>
        public static void TestTimeRangeLogic()
        {
            Debug.WriteLine("\n=== 测试时间段逻辑 ===");
            
            var config = new ScheduleConfig();
            
            // 测试正常时间段 (9:00-17:00)
            config.StartTime = new TimeSpan(9, 0, 0);
            config.EndTime = new TimeSpan(17, 0, 0);
            TestTimeRange(config, "正常时间段 (9:00-17:00)");
            
            // 测试跨天时间段 (22:00-06:00)
            config.StartTime = new TimeSpan(22, 0, 0);
            config.EndTime = new TimeSpan(6, 0, 0);
            TestTimeRange(config, "跨天时间段 (22:00-06:00)");
            
            // 测试勿扰模式
            config.IsDoNotDisturbEnabled = true;
            config.DoNotDisturbStart = new TimeSpan(12, 0, 0);
            config.DoNotDisturbEnd = new TimeSpan(13, 0, 0);
            TestTimeRange(config, "启用勿扰模式 (12:00-13:00)");
        }

        private static void TestTimeRange(ScheduleConfig config, string description)
        {
            Debug.WriteLine($"\n{description}:");
            
            // 测试几个关键时间点
            var testTimes = new[]
            {
                new TimeSpan(8, 0, 0),   // 8:00
                new TimeSpan(10, 0, 0),  // 10:00
                new TimeSpan(12, 30, 0), // 12:30
                new TimeSpan(15, 0, 0),  // 15:00
                new TimeSpan(18, 0, 0),  // 18:00
                new TimeSpan(23, 0, 0),  // 23:00
                new TimeSpan(2, 0, 0),   // 2:00
            };

            var originalTime = DateTime.Now.TimeOfDay;
            
            foreach (var testTime in testTimes)
            {
                // 模拟当前时间
                var testDateTime = DateTime.Today.Add(testTime);
                var isActive = IsInTimeRange(testTime, config.StartTime, config.EndTime);
                var isDoNotDisturb = config.IsDoNotDisturbEnabled && 
                                   IsInTimeRange(testTime, config.DoNotDisturbStart, config.DoNotDisturbEnd);
                
                Debug.WriteLine($"  {testTime:hh\\:mm} - 活动: {isActive}, 勿扰: {isDoNotDisturb}");
            }
        }

        private static bool IsInTimeRange(TimeSpan current, TimeSpan start, TimeSpan end)
        {
            if (start <= end)
            {
                return current >= start && current <= end;
            }
            else
            {
                return current >= start || current <= end;
            }
        }

        /// <summary>
        /// 测试调度器基本功能
        /// </summary>
        public static void TestSchedulerBasics()
        {
            Debug.WriteLine("\n=== 测试调度器基本功能 ===");
            
            var config = new ScheduleConfig
            {
                IsEnabled = true,
                StartTime = new TimeSpan(0, 0, 0),   // 全天
                EndTime = new TimeSpan(23, 59, 59),
                MinIntervalMinutes = 1,  // 1分钟最小间隔用于测试
                MaxIntervalMinutes = 2   // 2分钟最大间隔用于测试
            };
            
            var scheduler = new RandomScheduler(config);
            
            Debug.WriteLine("启动调度器...");
            scheduler.Start();
            Debug.WriteLine($"调度器运行状态: {scheduler.IsRunning}");
            
            var nextTime = scheduler.GetNextScheduledTime();
            if (nextTime.HasValue)
            {
                Debug.WriteLine($"下次调度时间: {nextTime.Value:HH:mm:ss}");
            }
            
            // 等待几秒钟
            Debug.WriteLine("等待5秒钟...");
            Thread.Sleep(5000);
            
            Debug.WriteLine("停止调度器...");
            scheduler.Stop();
            Debug.WriteLine($"调度器运行状态: {scheduler.IsRunning}");
            
            scheduler.Dispose();
        }

        /// <summary>
        /// 测试手动触发功能
        /// </summary>
        public static void TestManualTrigger()
        {
            Debug.WriteLine("\n=== 测试手动触发功能 ===");
            
            var config = new ScheduleConfig
            {
                IsEnabled = true
            };
            
            var scheduler = new RandomScheduler(config);
            
            Debug.WriteLine("手动触发抽背...");
            scheduler.TriggerManualRecitation();
            
            scheduler.Dispose();
        }

        /// <summary>
        /// 运行所有测试
        /// </summary>
        public static void RunAllTests()
        {
            try
            {
                Debug.WriteLine("开始随机抽背功能测试");
                Debug.WriteLine("============================");
                
                TestBasicConfiguration();
                TestTimeRangeLogic();
                TestSchedulerBasics();
                TestManualTrigger();
                
                Debug.WriteLine("\n============================");
                Debug.WriteLine("所有测试完成");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"测试过程中出现错误: {ex.Message}");
                Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
        }
    }
}
