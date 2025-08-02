using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using ToastFish.Model.PushControl;
using ToastFish.Model.SqliteControl;

namespace ToastFish.Model.RandomSchedule
{
    /// <summary>
    /// 随机抽背调度器
    /// </summary>
    public class RandomScheduler : IDisposable
    {
        private Timer _timer;
        private ScheduleConfig _config;
        private PushWords _pushWords;
        private Select _select;
        private volatile bool _isRunning = false;
        private volatile bool _disposed = false;
        private readonly object _lockObject = new object();
        private DateTime? _nextScheduledTime;

        public RandomScheduler(ScheduleConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _pushWords = new PushWords();
            _select = new Select();
        }

        /// <summary>
        /// 调度器是否正在运行
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// 启动随机调度器
        /// </summary>
        public void Start()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(RandomScheduler));

            lock (_lockObject)
            {
                if (_isRunning) return;

                if (!_config.IsEnabled)
                {
                    Debug.WriteLine("随机抽背功能未启用");
                    return;
                }

                _isRunning = true;
                Debug.WriteLine($"随机抽背调度器已启动 @{DateTime.Now}");
                Debug.WriteLine($"配置信息: 时间段 {_config.StartTime}-{_config.EndTime}, 间隔 {_config.MinIntervalMinutes}-{_config.MaxIntervalMinutes}分钟");
                ScheduleNext();
            }
        }

        /// <summary>
        /// 停止随机调度器
        /// </summary>
        public void Stop()
        {
            lock (_lockObject)
            {
                if (!_isRunning) return;

                _timer?.Dispose();
                _timer = null;
                _isRunning = false;
                Debug.WriteLine($"随机抽背调度器已停止 @{DateTime.Now}");
            }
        }

        /// <summary>
        /// 重启调度器（配置更改后调用）
        /// </summary>
        public void Restart()
        {
            Stop();
            if (_config.IsEnabled)
            {
                Start();
            }
        }

        /// <summary>
        /// 安排下一次抽背
        /// </summary>
        private void ScheduleNext()
        {
            if (!_isRunning || _disposed) return;

            try
            {
                var delay = _config.GetDelayToNextActiveTime();

                lock (_lockObject)
                {
                    if (!_isRunning || _disposed) return;

                    _timer?.Dispose();
                    _timer = new Timer(OnTimerCallback, null, delay, Timeout.Infinite);
                    _nextScheduledTime = DateTime.Now.AddMilliseconds(delay);
                }

                Debug.WriteLine($"下一次随机抽背安排在: {_nextScheduledTime:yyyy-MM-dd HH:mm:ss} (延迟 {delay / 1000.0:F1} 秒)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"安排下一次抽背时出错: {ex.Message}");
                // 出错时等待1分钟后重试
                lock (_lockObject)
                {
                    if (!_isRunning || _disposed) return;
                    _timer?.Dispose();
                    _timer = new Timer(OnTimerCallback, null, 60000, Timeout.Infinite);
                    _nextScheduledTime = DateTime.Now.AddMinutes(1);
                }
            }
        }

        /// <summary>
        /// 定时器回调方法
        /// </summary>
        private void OnTimerCallback(object state)
        {
            if (!_isRunning || _disposed) return;

            try
            {
                // 再次检查是否在有效时间段内
                if (!_config.IsInActiveTimeRange() || _config.IsInDoNotDisturbTime())
                {
                    Debug.WriteLine("当前不在有效抽背时间段内，重新安排");
                    ScheduleNext();
                    return;
                }

                // 执行单词抽背
                ExecuteRandomRecitation();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"执行随机抽背时出错: {ex.Message}");
            }
            finally
            {
                // 安排下一次抽背
                if (_isRunning && !_disposed)
                {
                    ScheduleNext();
                }
            }
        }

        /// <summary>
        /// 执行随机单词抽背
        /// </summary>
        private void ExecuteRandomRecitation()
        {
            try
            {
                Debug.WriteLine($"开始执行随机抽背 @{DateTime.Now}");

                // 检查是否有选定的词库
                if (string.IsNullOrEmpty(Select.TABLE_NAME))
                {
                    Debug.WriteLine("未选择词库，尝试使用默认词库");
                    // 尝试加载默认词库
                    _select.LoadGlobalConfig();
                    if (string.IsNullOrEmpty(Select.TABLE_NAME))
                    {
                        Debug.WriteLine("无法获取词库信息，跳过本次抽背");
                        return;
                    }
                }

                // 根据当前选择的词库类型获取随机单词
                if (Select.TABLE_NAME == "Goin")
                {
                    // 五十音
                    var goinWords = _select.GetGainWordList();
                    if (goinWords == null || goinWords.Count == 0)
                    {
                        Debug.WriteLine("无法获取五十音单词，跳过本次抽背");
                        return;
                    }
                    var random = new Random();
                    var goinWord = goinWords[random.Next(goinWords.Count)];
                    // 这里需要特殊处理五十音的推送
                    _pushWords.PushMessage($"五十音随机抽背\n{goinWord.hiragana} - {goinWord.katakana}\n{goinWord.romaji}");
                    Debug.WriteLine($"已推送随机五十音: {goinWord.hiragana}");
                }
                else if (Select.TABLE_NAME == "StdJp_Mid")
                {
                    // 日语单词 - 支持多个单词抽背
                    int wordCount = _config.WordCount;
                    var jpWords = _select.GetRandomJpWords(wordCount);
                    if (jpWords == null || jpWords.Count == 0)
                    {
                        Debug.WriteLine("无法获取日语单词，跳过本次抽背");
                        return;
                    }

                    if (wordCount == 1)
                    {
                        // 单个单词，使用原有的简单显示方式
                        var jpWord = jpWords[0];
                        _pushWords.PushMessage($"日语随机抽背\n{jpWord.headWord} ({jpWord.hiragana})\n{jpWord.tranCN}");
                        PlayJapanesePronunciation(jpWord);
                        Debug.WriteLine($"已推送随机日语单词: {jpWord.headWord}");
                    }
                    else
                    {
                        // 多个单词，启动完整的抽背流程
                        StartJapaneseWordRecitation(jpWords);
                        Debug.WriteLine($"已启动日语随机抽背，共 {jpWords.Count} 个单词");
                    }
                }
                else
                {
                    // 英语单词 - 支持多个单词抽背
                    _select.SelectWordList(); // 确保加载了单词列表
                    int wordCount = _config.WordCount;
                    var randomWords = _select.GetRandomWords(wordCount);
                    if (randomWords == null || randomWords.Count == 0)
                    {
                        Debug.WriteLine("无法获取英语单词，跳过本次抽背");
                        return;
                    }

                    if (wordCount == 1)
                    {
                        // 单个单词，使用原有的交互方式
                        var word = randomWords[0];
                        PushWordWithInteraction(word);
                        Debug.WriteLine($"已推送随机英语单词: {word.headWord}");
                    }
                    else
                    {
                        // 多个单词，启动完整的抽背流程
                        StartEnglishWordRecitation(randomWords);
                        Debug.WriteLine($"已启动英语随机抽背，共 {randomWords.Count} 个单词");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"执行随机抽背失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 手动触发一次抽背（用于测试）
        /// </summary>
        public void TriggerManualRecitation()
        {
            if (!_config.IsEnabled)
            {
                Debug.WriteLine("随机抽背功能未启用");
                return;
            }

            try
            {
                ExecuteRandomRecitation();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"手动触发抽背失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取下一次抽背的预计时间
        /// </summary>
        public DateTime? GetNextScheduledTime()
        {
            if (!_isRunning || _disposed) return null;
            return _nextScheduledTime;
        }

        /// <summary>
        /// 播放单词发音
        /// </summary>
        /// <param name="word">要播放发音的单词</param>
        private void PlayWordPronunciation(ToastFish.Model.SqliteControl.Word word)
        {
            try
            {
                // 检查是否启用了自动播放
                if (ToastFish.Model.SqliteControl.Select.AUTO_PLAY == 0)
                {
                    Debug.WriteLine("自动播放功能未启用，跳过发音");
                    return;
                }

                string word_pron, word_save_name;
                switch (ToastFish.Model.SqliteControl.Select.ENG_TYPE)
                {
                    case 1:
                        word_save_name = word.headWord + "_us";
                        word_pron = word.headWord + "&type=1";
                        break;
                    default:
                        word_save_name = word.headWord + "_uk";
                        word_pron = word.headWord + "&type=2";
                        break;
                }

                List<string> words = new List<string>();
                words.Add(word_save_name);
                words.Add(word_pron);

                bool isOK = ToastFish.Model.Download.DownloadMp3.PlayMp3(words);
                if (!isOK)
                {
                    // 如果在线发音失败，使用系统TTS，确保音量一致
                    System.Speech.Synthesis.SpeechSynthesizer synth = new System.Speech.Synthesis.SpeechSynthesizer();
                    synth.Volume = 100; // 设置最大音量，与其他地方保持一致
                    synth.Rate = 0;     // 设置正常语速
                    synth.SpeakAsync(word.headWord);
                    Debug.WriteLine($"使用TTS播放: {word.headWord}，音量: {synth.Volume}");
                }
                else
                {
                    Debug.WriteLine($"播放在线发音: {word.headWord}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"播放发音失败: {ex.Message}");
                // 发音失败时使用系统TTS作为备选
                try
                {
                    System.Speech.Synthesis.SpeechSynthesizer synth = new System.Speech.Synthesis.SpeechSynthesizer();
                    synth.SpeakAsync(word.headWord);
                }
                catch (Exception ttsEx)
                {
                    Debug.WriteLine($"系统TTS也失败: {ttsEx.Message}");
                }
            }
        }

        /// <summary>
        /// 播放日语单词发音
        /// </summary>
        /// <param name="jpWord">要播放发音的日语单词</param>
        private void PlayJapanesePronunciation(ToastFish.Model.SqliteControl.JpWord jpWord)
        {
            try
            {
                System.Speech.Synthesis.SpeechSynthesizer synth = new System.Speech.Synthesis.SpeechSynthesizer();

                // 设置统一的音量和语速，确保与其他地方一致
                synth.Volume = 100; // 设置最大音量
                synth.Rate = 0;     // 设置正常语速

                try
                {
                    // 尝试设置日语语音
                    var voices = synth.GetInstalledVoices();
                    foreach (var voice in voices)
                    {
                        if (voice.VoiceInfo.Culture.Name.StartsWith("ja"))
                        {
                            synth.SelectVoice(voice.VoiceInfo.Name);
                            Debug.WriteLine($"选择日语语音: {voice.VoiceInfo.Name}");
                            break;
                        }
                    }
                }
                catch
                {
                    // 如果没有日语语音，使用默认语音
                    Debug.WriteLine("未找到日语语音，使用默认语音");
                }

                // 播放平假名发音
                synth.SpeakAsync(jpWord.hiragana);
                Debug.WriteLine($"播放日语发音: {jpWord.hiragana}，音量: {synth.Volume}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"播放日语发音失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 推送单词并处理用户交互（包括发音按钮）
        /// </summary>
        /// <param name="word">要推送的单词</param>
        private async void PushWordWithInteraction(ToastFish.Model.SqliteControl.Word word)
        {
            try
            {
                // 推送单词通知
                _pushWords.PushOneWord(word);

                // 自动播放发音
                PlayWordPronunciation(word);

                // 处理用户交互
                await HandleWordInteraction(word);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"推送单词交互失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理单词通知的用户交互
        /// </summary>
        /// <param name="word">当前单词</param>
        private async System.Threading.Tasks.Task HandleWordInteraction(ToastFish.Model.SqliteControl.Word word)
        {
            try
            {
                // 等待用户交互
                var result = await _pushWords.ProcessToastNotificationRecitation();

                // 处理不同的用户操作
                switch (result)
                {
                    case 2: // 发音按钮 (voice action 返回 2)
                        Debug.WriteLine("用户点击了发音按钮");
                        PlayWordPronunciation(word);
                        // 继续等待用户交互
                        await HandleWordInteraction(word);
                        break;
                    case 0: // 记住了 (succeed action)
                        Debug.WriteLine("用户点击了记住了");
                        break;
                    case 1: // 跳过 (fail action)
                        Debug.WriteLine("用户点击了跳过");
                        break;
                    default:
                        Debug.WriteLine($"用户完成了单词交互: {result}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"处理单词交互失败: {ex.Message}");
            }
        }



        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 启动日语单词完整抽背流程
        /// </summary>
        /// <param name="jpWords">要抽背的日语单词列表</param>
        private void StartJapaneseWordRecitation(List<ToastFish.Model.SqliteControl.JpWord> jpWords)
        {
            try
            {
                // 确保配置已加载
                _select.LoadGlobalConfig();
                Debug.WriteLine($"AUTO_PLAY设置: {ToastFish.Model.SqliteControl.Select.AUTO_PLAY}");

                // 创建WordType对象
                var wordType = new ToastFish.Model.PushControl.WordType
                {
                    Number = jpWords.Count,
                    JpWordList = jpWords
                };

                // 在新线程中启动抽背流程，避免阻塞定时器
                System.Threading.Thread recitationThread = new System.Threading.Thread(() =>
                {
                    try
                    {
                        Debug.WriteLine($"开始日语完整抽背流程，共 {jpWords.Count} 个单词");
                        ToastFish.Model.PushControl.PushJpWords.Recitation(wordType);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"日语随机抽背流程执行失败: {ex.Message}");
                        Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                    }
                })
                {
                    IsBackground = true
                };
                recitationThread.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"启动日语随机抽背流程失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 启动英语单词完整抽背流程
        /// </summary>
        /// <param name="words">要抽背的英语单词列表</param>
        private void StartEnglishWordRecitation(List<ToastFish.Model.SqliteControl.Word> words)
        {
            try
            {
                // 确保配置已加载
                _select.LoadGlobalConfig();
                Debug.WriteLine($"AUTO_PLAY设置: {ToastFish.Model.SqliteControl.Select.AUTO_PLAY}");

                // 创建WordType对象
                var wordType = new ToastFish.Model.PushControl.WordType
                {
                    Number = words.Count,
                    WordList = words
                };

                // 在新线程中启动抽背流程，避免阻塞定时器
                System.Threading.Thread recitationThread = new System.Threading.Thread(() =>
                {
                    try
                    {
                        Debug.WriteLine($"开始英语完整抽背流程，共 {words.Count} 个单词");
                        ToastFish.Model.PushControl.PushWords.Recitation(wordType);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"英语随机抽背流程执行失败: {ex.Message}");
                        Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                    }
                })
                {
                    IsBackground = true
                };
                recitationThread.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"启动英语随机抽背流程失败: {ex.Message}");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Stop();
                }
                _disposed = true;
            }
        }
    }
}
