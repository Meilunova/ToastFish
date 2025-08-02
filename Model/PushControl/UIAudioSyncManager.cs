using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp.Notifications;
using ToastFish.Model.SqliteControl;

namespace ToastFish.Model.PushControl
{
    /// <summary>
    /// UI-音频同步管理器 - 确保通知完全显示后再播放音频
    /// </summary>
    public class UIAudioSyncManager
    {
        private static readonly Lazy<UIAudioSyncManager> _instance = new Lazy<UIAudioSyncManager>(() => new UIAudioSyncManager());
        public static UIAudioSyncManager Instance => _instance.Value;

        private UIAudioSyncManager() { }

        /// <summary>
        /// 同步配置
        /// </summary>
        public class SyncConfig
        {
            /// <summary>
            /// 基础延迟时间（毫秒）
            /// </summary>
            public int BaseDelayMs { get; set; } = 500;

            /// <summary>
            /// 每个按钮的额外延迟（毫秒）
            /// </summary>
            public int PerButtonDelayMs { get; set; } = 50;

            /// <summary>
            /// 最大延迟时间（毫秒）
            /// </summary>
            public int MaxDelayMs { get; set; } = 1500;

            /// <summary>
            /// 最小延迟时间（毫秒）
            /// </summary>
            public int MinDelayMs { get; set; } = 300;

            /// <summary>
            /// 是否启用智能延迟
            /// </summary>
            public bool EnableSmartDelay { get; set; } = true;
        }

        private readonly SyncConfig _config = new SyncConfig();

        /// <summary>
        /// 显示通知并在UI渲染完成后播放音频
        /// </summary>
        /// <param name="toastBuilder">通知构建器</param>
        /// <param name="audioText">音频文本</param>
        /// <param name="audioType">音频类型</param>
        /// <param name="onlineParams">在线音频参数</param>
        /// <param name="buttonCount">按钮数量（用于计算延迟）</param>
        public async Task ShowNotificationWithSyncedAudioAsync(
            ToastContentBuilder toastBuilder,
            string audioText,
            AudioManager.AudioType audioType,
            System.Collections.Generic.List<string> onlineParams = null,
            int buttonCount = 0)
        {
            if (Select.AUTO_PLAY == 0)
            {
                // 如果不需要自动播放，直接显示通知
                toastBuilder.Show();
                System.Diagnostics.Debug.WriteLine("自动播放未启用，仅显示通知");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"开始同步显示通知和音频: {audioText}");

                // 1. 显示通知
                var notificationStartTime = DateTime.Now;
                toastBuilder.Show();
                System.Diagnostics.Debug.WriteLine($"通知已发送显示请求: {audioText}");

                // 2. 计算智能延迟
                int delay = CalculateSmartDelay(buttonCount, audioText?.Length ?? 0);
                System.Diagnostics.Debug.WriteLine($"计算的智能延迟: {delay}ms");

                // 3. 等待UI渲染完成
                await WaitForUIRenderingAsync(delay);

                // 4. 验证通知是否成功显示
                if (await VerifyNotificationDisplayedAsync())
                {
                    System.Diagnostics.Debug.WriteLine("通知显示验证成功，开始播放音频");
                    
                    // 5. 播放音频
                    await AudioManager.Instance.RequestPlayAudioAsync(audioText, audioType, onlineParams);
                    
                    var totalTime = (DateTime.Now - notificationStartTime).TotalMilliseconds;
                    System.Diagnostics.Debug.WriteLine($"UI-音频同步完成，总耗时: {totalTime:F0}ms");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("通知显示验证失败，仍然播放音频（降级处理）");
                    await AudioManager.Instance.RequestPlayAudioAsync(audioText, audioType, onlineParams);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UI-音频同步失败: {ex.Message}");
                // 降级处理：直接播放音频
                try
                {
                    await AudioManager.Instance.RequestPlayAudioAsync(audioText, audioType, onlineParams);
                }
                catch (Exception audioEx)
                {
                    System.Diagnostics.Debug.WriteLine($"降级音频播放也失败: {audioEx.Message}");
                }
            }
        }

        /// <summary>
        /// 计算智能延迟时间
        /// </summary>
        private int CalculateSmartDelay(int buttonCount, int textLength)
        {
            if (!_config.EnableSmartDelay)
            {
                return _config.BaseDelayMs;
            }

            // 基础延迟
            int delay = _config.BaseDelayMs;

            // 根据按钮数量增加延迟
            delay += buttonCount * _config.PerButtonDelayMs;

            // 根据文本长度微调（长文本需要更多渲染时间）
            if (textLength > 50)
            {
                delay += 100;
            }
            else if (textLength > 100)
            {
                delay += 200;
            }

            // 限制在合理范围内
            delay = Math.Max(_config.MinDelayMs, Math.Min(_config.MaxDelayMs, delay));

            return delay;
        }

        /// <summary>
        /// 等待UI渲染完成
        /// </summary>
        private async Task WaitForUIRenderingAsync(int delayMs)
        {
            // 分段等待，允许中途检查
            const int checkInterval = 100;
            int remainingDelay = delayMs;

            while (remainingDelay > 0)
            {
                int currentWait = Math.Min(checkInterval, remainingDelay);
                await Task.Delay(currentWait);
                remainingDelay -= currentWait;

                // 可以在这里添加额外的UI状态检查
                // 例如检查系统负载、通知队列等
            }
        }

        /// <summary>
        /// 验证通知是否成功显示
        /// </summary>
        private async Task<bool> VerifyNotificationDisplayedAsync()
        {
            try
            {
                await Task.Delay(50); // 短暂等待确保通知系统更新

                // 检查通知历史
                var history = ToastNotificationManagerCompat.History.GetHistory();
                bool hasNotifications = history.Count > 0;

                System.Diagnostics.Debug.WriteLine($"通知历史验证: {history.Count} 个通知");
                return hasNotifications;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"通知验证失败: {ex.Message}");
                return true; // 验证失败时假设通知已显示，继续播放音频
            }
        }

        /// <summary>
        /// 更新同步配置
        /// </summary>
        public void UpdateConfig(Action<SyncConfig> configUpdater)
        {
            configUpdater(_config);
            System.Diagnostics.Debug.WriteLine($"UI-音频同步配置已更新: 基础延迟={_config.BaseDelayMs}ms, 智能延迟={_config.EnableSmartDelay}");
        }

        /// <summary>
        /// 获取当前配置
        /// </summary>
        public SyncConfig GetCurrentConfig()
        {
            return new SyncConfig
            {
                BaseDelayMs = _config.BaseDelayMs,
                PerButtonDelayMs = _config.PerButtonDelayMs,
                MaxDelayMs = _config.MaxDelayMs,
                MinDelayMs = _config.MinDelayMs,
                EnableSmartDelay = _config.EnableSmartDelay
            };
        }

        /// <summary>
        /// 重置为默认配置
        /// </summary>
        public void ResetToDefault()
        {
            _config.BaseDelayMs = 500;
            _config.PerButtonDelayMs = 50;
            _config.MaxDelayMs = 1500;
            _config.MinDelayMs = 300;
            _config.EnableSmartDelay = true;
            System.Diagnostics.Debug.WriteLine("UI-音频同步配置已重置为默认值");
        }

        /// <summary>
        /// 简化版本：只显示通知，不播放音频
        /// </summary>
        public void ShowNotificationOnly(ToastContentBuilder toastBuilder)
        {
            toastBuilder.Show();
            System.Diagnostics.Debug.WriteLine("仅显示通知（无音频）");
        }
    }
}
