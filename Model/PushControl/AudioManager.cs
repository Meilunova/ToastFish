using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Speech.Synthesis;
using System.Threading;
using System.Threading.Tasks;
using ToastFish.Model.SqliteControl;

namespace ToastFish.Model.PushControl
{
    /// <summary>
    /// 统一的音频播放管理器 - 防止重复播放和音频冲突
    /// </summary>
    public class AudioManager
    {
        private static readonly Lazy<AudioManager> _instance = new Lazy<AudioManager>(() => new AudioManager());
        public static AudioManager Instance => _instance.Value;

        private readonly ConcurrentQueue<AudioRequest> _audioQueue = new ConcurrentQueue<AudioRequest>();
        private readonly SemaphoreSlim _playingSemaphore = new SemaphoreSlim(1, 1);
        private volatile bool _isPlaying = false;
        private CancellationTokenSource _cancellationTokenSource;

        private AudioManager()
        {
            StartAudioProcessor();
        }

        /// <summary>
        /// 音频播放请求
        /// </summary>
        public class AudioRequest
        {
            public string Text { get; set; }
            public AudioType Type { get; set; }
            public List<string> OnlineAudioParams { get; set; }
            public DateTime RequestTime { get; set; } = DateTime.Now;
        }

        public enum AudioType
        {
            English,
            Japanese,
            Chinese
        }

        /// <summary>
        /// 请求播放音频（异步，防重复）
        /// </summary>
        public async Task<bool> RequestPlayAudioAsync(string text, AudioType type, List<string> onlineParams = null)
        {
            if (string.IsNullOrEmpty(text) || Select.AUTO_PLAY == 0)
            {
                System.Diagnostics.Debug.WriteLine($"跳过音频播放: text={text}, AUTO_PLAY={Select.AUTO_PLAY}");
                return false;
            }

            // 防重复播放：检查队列中是否已有相同内容
            if (IsAudioAlreadyQueued(text))
            {
                System.Diagnostics.Debug.WriteLine($"音频已在队列中，跳过重复请求: {text}");
                return false;
            }

            var request = new AudioRequest
            {
                Text = text,
                Type = type,
                OnlineAudioParams = onlineParams
            };

            _audioQueue.Enqueue(request);
            System.Diagnostics.Debug.WriteLine($"音频请求已加入队列: {text} (类型: {type})");
            return true;
        }

        /// <summary>
        /// 检查音频是否已在队列中
        /// </summary>
        private bool IsAudioAlreadyQueued(string text)
        {
            var queueArray = _audioQueue.ToArray();
            foreach (var item in queueArray)
            {
                if (item.Text == text && (DateTime.Now - item.RequestTime).TotalSeconds < 3)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 启动音频处理器
        /// </summary>
        private void StartAudioProcessor()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(async () => await ProcessAudioQueue(_cancellationTokenSource.Token));
        }

        /// <summary>
        /// 处理音频播放队列
        /// </summary>
        private async Task ProcessAudioQueue(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_audioQueue.TryDequeue(out AudioRequest request))
                    {
                        await _playingSemaphore.WaitAsync(cancellationToken);
                        try
                        {
                            _isPlaying = true;
                            await PlayAudioInternal(request);
                            
                            // 播放间隔，防止音频重叠
                            await Task.Delay(500, cancellationToken);
                        }
                        finally
                        {
                            _isPlaying = false;
                            _playingSemaphore.Release();
                        }
                    }
                    else
                    {
                        await Task.Delay(100, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"音频处理器错误: {ex.Message}");
                    await Task.Delay(1000, cancellationToken);
                }
            }
        }

        /// <summary>
        /// 内部音频播放实现
        /// </summary>
        private async Task PlayAudioInternal(AudioRequest request)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"开始播放音频: {request.Text} (类型: {request.Type})");

                bool onlineSuccess = false;

                // 尝试在线发音（仅英语）
                if (request.Type == AudioType.English && request.OnlineAudioParams != null)
                {
                    try
                    {
                        onlineSuccess = Download.DownloadMp3.PlayMp3(request.OnlineAudioParams);
                        if (onlineSuccess)
                        {
                            System.Diagnostics.Debug.WriteLine($"在线发音成功: {request.Text}");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"在线发音失败: {ex.Message}");
                    }
                }

                // 使用TTS备选方案
                await PlayTTSAsync(request);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"音频播放失败: {ex.Message}");
            }
        }

        /// <summary>
        /// TTS播放
        /// </summary>
        private async Task PlayTTSAsync(AudioRequest request)
        {
            await Task.Run(() =>
            {
                try
                {
                    using (var synth = new SpeechSynthesizer())
                    {
                        // 根据语言类型设置语音
                        ConfigureSynthesizerForLanguage(synth, request.Type);
                        
                        synth.Volume = 100;
                        synth.Rate = 0;

                        // 同步播放，确保完成后再继续
                        synth.Speak(request.Text);
                        System.Diagnostics.Debug.WriteLine($"TTS播放完成: {request.Text}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"TTS播放失败: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 根据语言配置语音合成器
        /// </summary>
        private void ConfigureSynthesizerForLanguage(SpeechSynthesizer synth, AudioType type)
        {
            try
            {
                switch (type)
                {
                    case AudioType.Japanese:
                        // 尝试设置日语语音
                        var voices = synth.GetInstalledVoices();
                        foreach (var voice in voices)
                        {
                            if (voice.VoiceInfo.Culture.Name.StartsWith("ja"))
                            {
                                synth.SelectVoice(voice.VoiceInfo.Name);
                                System.Diagnostics.Debug.WriteLine($"选择日语语音: {voice.VoiceInfo.Name}");
                                return;
                            }
                        }
                        System.Diagnostics.Debug.WriteLine("未找到日语语音，使用默认语音");
                        break;

                    case AudioType.English:
                        // 使用默认英语语音
                        break;

                    case AudioType.Chinese:
                        // 尝试设置中文语音
                        var chineseVoices = synth.GetInstalledVoices();
                        foreach (var voice in chineseVoices)
                        {
                            if (voice.VoiceInfo.Culture.Name.StartsWith("zh"))
                            {
                                synth.SelectVoice(voice.VoiceInfo.Name);
                                System.Diagnostics.Debug.WriteLine($"选择中文语音: {voice.VoiceInfo.Name}");
                                return;
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"配置语音失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 停止所有音频播放
        /// </summary>
        public async Task StopAllAudioAsync()
        {
            _cancellationTokenSource?.Cancel();
            
            // 清空队列
            while (_audioQueue.TryDequeue(out _)) { }
            
            await _playingSemaphore.WaitAsync();
            try
            {
                _isPlaying = false;
            }
            finally
            {
                _playingSemaphore.Release();
            }

            // 重启处理器
            StartAudioProcessor();
            System.Diagnostics.Debug.WriteLine("音频播放已停止并重置");
        }

        /// <summary>
        /// 获取当前播放状态
        /// </summary>
        public bool IsCurrentlyPlaying => _isPlaying;

        /// <summary>
        /// 获取队列中的音频数量
        /// </summary>
        public int QueueCount => _audioQueue.Count;
    }
}
