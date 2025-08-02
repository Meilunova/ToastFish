using System;
using System.Collections.Generic;
using ToastFish.Model.SqliteControl;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp.Notifications;
using System.Speech.Synthesis;
using System.Threading;
using ToastFish.Model.Log;

namespace ToastFish.Model.PushControl
{
    class PushJpWords : PushWords
    {
        public JpWord GetRandomWord(List<JpWord> WordList)
        {
            Random Rd = new Random();
            int Index = Rd.Next(WordList.Count);
            return WordList[Index];
        }

        public string GetJapaneseVoiceName()
        {
            try
            {
                using (SpeechSynthesizer synth = new SpeechSynthesizer())
                {
                    System.Diagnostics.Debug.WriteLine("开始检查已安装的语音包:");
                    foreach (InstalledVoice voice in synth.GetInstalledVoices())
                    {
                        VoiceInfo info = voice.VoiceInfo;
                        System.Diagnostics.Debug.WriteLine($"发现语音: {info.Name} - 语言: {info.Culture.IetfLanguageTag} - 性别: {info.Gender}");

                        if (info.Culture.IetfLanguageTag == "ja-JP")
                        {
                            System.Diagnostics.Debug.WriteLine($"找到日语语音: {info.Name}");
                            return info.Name;
                        }
                    }
                    System.Diagnostics.Debug.WriteLine("未找到日语语音包 (ja-JP)");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取日语语音失败: {ex.Message}");
            }
            return "";
        }

        /// <summary>
        /// 测试日语发音功能
        /// </summary>
        public void TestJapaneseSpeech(string text)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"开始测试日语发音: {text}");

                using (SpeechSynthesizer synth = new SpeechSynthesizer())
                {
                    // 获取日语语音
                    string voiceName = GetJapaneseVoiceName();

                    if (!string.IsNullOrEmpty(voiceName))
                    {
                        System.Diagnostics.Debug.WriteLine($"使用日语语音: {voiceName}");
                        synth.SelectVoice(voiceName);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("未找到日语语音，使用默认语音");
                        // 尝试设置语言
                        try
                        {
                            synth.SelectVoiceByHints(VoiceGender.NotSet, VoiceAge.NotSet, 0, new System.Globalization.CultureInfo("ja-JP"));
                        }
                        catch (Exception ex2)
                        {
                            System.Diagnostics.Debug.WriteLine($"设置日语语言失败: {ex2.Message}");
                        }
                    }

                    // 设置语音参数
                    synth.Volume = 100;
                    synth.Rate = 0;

                    System.Diagnostics.Debug.WriteLine($"当前语音: {synth.Voice.Name}");
                    System.Diagnostics.Debug.WriteLine($"当前语言: {synth.Voice.Culture.IetfLanguageTag}");

                    // 播放发音
                    synth.SpeakAsync(text);
                    System.Diagnostics.Debug.WriteLine($"已发送发音请求: {text}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"日语发音测试失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 重写基类的自动发音方法 - 支持日语单词
        /// </summary>
        /// <param name="jpWord">要播放发音的日语单词</param>
        protected virtual void PlayAutoPronunciation(JpWord jpWord)
        {
            if (Select.AUTO_PLAY == 0)
            {
                System.Diagnostics.Debug.WriteLine($"日语自动播放未启用，AUTO_PLAY={Select.AUTO_PLAY}");
                return;
            }

            PlayJapanesePronunciation(jpWord);
        }

        /// <summary>
        /// 播放日语单词发音 - 使用和随机抽背相同的实现
        /// </summary>
        /// <param name="jpWord">要播放发音的日语单词</param>
        private void PlayJapanesePronunciation(JpWord jpWord)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"开始播放日语发音: {jpWord.hiragana}");
                System.Console.WriteLine($"[DEBUG] 开始播放日语发音: {jpWord.hiragana}");

                // 不使用using语句，避免过早dispose
                System.Speech.Synthesis.SpeechSynthesizer synth = new System.Speech.Synthesis.SpeechSynthesizer();

                try
                {
                    // 尝试设置日语语音 - 使用和随机抽背相同的逻辑
                    var voices = synth.GetInstalledVoices();
                    bool foundJapaneseVoice = false;

                    foreach (var voice in voices)
                    {
                        System.Diagnostics.Debug.WriteLine($"检查语音: {voice.VoiceInfo.Name} - 语言: {voice.VoiceInfo.Culture.Name}");
                        if (voice.VoiceInfo.Culture.Name.StartsWith("ja"))
                        {
                            synth.SelectVoice(voice.VoiceInfo.Name);
                            foundJapaneseVoice = true;
                            System.Diagnostics.Debug.WriteLine($"选择日语语音: {voice.VoiceInfo.Name}");
                            break;
                        }
                    }

                    if (!foundJapaneseVoice)
                    {
                        System.Diagnostics.Debug.WriteLine("未找到日语语音，使用默认语音");
                    }
                }
                catch (Exception voiceEx)
                {
                    System.Diagnostics.Debug.WriteLine($"设置语音失败: {voiceEx.Message}");
                }

                // 播放平假名发音
                synth.SpeakAsync(jpWord.hiragana);
                System.Diagnostics.Debug.WriteLine($"已发送日语发音请求: {jpWord.hiragana}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"播放日语发音失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
        }

        public void PushOneWord(JpWord CurrentWord)
        {
            PushOneWordWithStatus(CurrentWord, "新开", 0, 0, 0);
        }

        /// <summary>
        /// 推送单词但不自动发音（用于多个单词抽背，避免重复发音）
        /// </summary>
        public void PushOneWordWithStatusNoAudio(JpWord CurrentWord, string status, int numNewCards, int numLearingCards, int numReviewedCards)
        {
            ToastNotificationManagerCompat.History.Clear();

            // 构建主要内容 - 完全模仿英语单词的格式
            string WordPhonePosTran = CurrentWord.headWord + "  (" + CurrentWord.hiragana + ")";
            if (CurrentWord.phone != -1)
                WordPhonePosTran += "  重音：" + CurrentWord.phone.ToString();
            WordPhonePosTran += "\n" + CurrentWord.pos + ". " + CurrentWord.tranCN;

            // 第二行内容 - 日语没有例句，显示空白
            string SentenceTran = "";

            // 第三行内容 - 动态显示学习状态信息
            string HeadTile = $"状态：{status} 新:{numNewCards} 背:{numLearingCards} 复:{numReviewedCards}";

            // 只显示UI通知，不包含自动发音
            new ToastContentBuilder()
            .AddText(WordPhonePosTran)
            .AddText(SentenceTran)
            .AddText(HeadTile)
            .AddButton(new ToastButton()
                .SetContent("没有印象")
                .AddArgument("action", "again")
                .SetBackgroundActivation())
            .AddButton(new ToastButton()
                .SetContent("记忆模糊")
                .AddArgument("action", "hard")
                .SetBackgroundActivation())
            .AddButton(new ToastButton()
                .SetContent("暂时记住")
                .AddArgument("action", "good")
                .SetBackgroundActivation())
            .AddButton(new ToastButton()
                .SetContent("已经牢记")
                .AddArgument("action", "easy")
                .SetBackgroundActivation())
            .Show();
        }



        public void PushOneWordWithStatus(JpWord CurrentWord, string status, int numNewCards, int numLearingCards, int numReviewedCards)
        {
            ToastNotificationManagerCompat.History.Clear();

            // 构建主要内容 - 完全模仿英语单词的格式
            string WordPhonePosTran = CurrentWord.headWord + "  (" + CurrentWord.hiragana + ")";
            if (CurrentWord.phone != -1)
                WordPhonePosTran += "  重音：" + CurrentWord.phone.ToString();
            WordPhonePosTran += "\n" + CurrentWord.pos + ". " + CurrentWord.tranCN;

            // 第二行内容 - 日语没有例句，显示空白
            string SentenceTran = "";

            // 第三行内容 - 动态显示学习状态信息
            string HeadTile = $"状态：{status} 新:{numNewCards} 背:{numLearingCards} 复:{numReviewedCards}";

            // 先显示UI通知
            new ToastContentBuilder()
            .AddText(WordPhonePosTran)
            .AddText(SentenceTran)
            .AddText(HeadTile)
            .AddButton(new ToastButton()
                .SetContent("没有印象")
                .AddArgument("action", "again")
                .SetBackgroundActivation())
            .AddButton(new ToastButton()
                .SetContent("记忆模糊")
                .AddArgument("action", "hard")
                .SetBackgroundActivation())
            .AddButton(new ToastButton()
                .SetContent("暂时记住")
                .AddArgument("action", "good")
                .SetBackgroundActivation())
            .AddButton(new ToastButton()
                .SetContent("已经牢记")
                .AddArgument("action", "easy")
                .SetBackgroundActivation())
            .Show();

            // 注意：自动发音现在由统一的AudioManager处理，这里不再重复调用
        }

        public void PushOneTransQuestion(JpWord CurrentWord, string B, string C)
        {
            string Question = CurrentWord.tranCN;
            string A = CurrentWord.headWord;

            Random Rd = new Random();
            int AnswerIndex = Rd.Next(3);
            QUESTION_CURRENT_RIGHT_ANSWER = AnswerIndex;

            if (AnswerIndex == 0)
            {
                new ToastContentBuilder()
               .AddText("翻译\n" + Question)

               .AddButton(new ToastButton()
                   .SetContent("A." + A)
                   .AddArgument("action", "0")
                   .SetBackgroundActivation())

               .AddButton(new ToastButton()
                   .SetContent("B." + B)
                   .AddArgument("action", "1")
                   .SetBackgroundActivation())

               .AddButton(new ToastButton()
                   .SetContent("C." + C)
                   .AddArgument("action", "2")
                   .SetBackgroundActivation())

               .Show();
            }
            else if (AnswerIndex == 1)
            {
                new ToastContentBuilder()
                .AddText("翻译\n" + Question)

               .AddButton(new ToastButton()
                   .SetContent("A." + B)
                   .AddArgument("action", "0")
                   .SetBackgroundActivation())

               .AddButton(new ToastButton()
                   .SetContent("B." + A)
                   .AddArgument("action", "1")
                   .SetBackgroundActivation())

               .AddButton(new ToastButton()
                   .SetContent("C." + C)
                   .AddArgument("action", "2")
                   .SetBackgroundActivation())
               .Show();
            }
            else if (AnswerIndex == 2)
            {
                new ToastContentBuilder()
                .AddText("翻译\n" + Question)

               .AddButton(new ToastButton()
                   .SetContent("A." + C)
                   .AddArgument("action", "0")
                   .SetBackgroundActivation())

               .AddButton(new ToastButton()
                   .SetContent("B." + B)
                   .AddArgument("action", "1")
                   .SetBackgroundActivation())

               .AddButton(new ToastButton()
                   .SetContent("C." + A)
                   .AddArgument("action", "2")
                   .SetBackgroundActivation())
               .Show();
            }
        }

        public static new void Recitation(Object Words)
        {
            try
            {
                WordType WordList = (WordType)Words;
                PushJpWords pushJpWords = new PushJpWords();
                Select Query = new Select();

                // 确保加载全局配置，特别是AUTO_PLAY设置
                Query.LoadGlobalConfig();
                string configMsg = $"日语抽背流程中AUTO_PLAY设置: {Select.AUTO_PLAY}";
                System.Diagnostics.Debug.WriteLine(configMsg);

                // 显示配置调试通知
                pushJpWords.PushMessage($"调试: {configMsg}");

                List<JpWord> RandomList;
                bool ImportFlag = true;

                if (WordList.JpWordList == null)
                {
                    RandomList = Query.GetRandomJpWordList((int)WordList.Number);
                    ImportFlag = false;
                }
                else
                {
                    RandomList = WordList.JpWordList;
                }

                if (RandomList == null || RandomList.Count == 0)
                {
                    if (ImportFlag == false)
                    {
                        pushJpWords.PushMessage("好..好像词库里没有单词了，您就是摸鱼之王！");
                    }
                    else
                    {
                        pushJpWords.PushMessage("导入的日语词库为空！");
                    }
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"开始日语抽背，共 {RandomList.Count} 个单词");
                List<JpWord> CopyList = pushJpWords.Clone<JpWord>(RandomList);

                if (ImportFlag == false)
                {
                    CreateLog Log = new CreateLog();
                    String LogName = "Log\\" + DateTime.Now.ToString().Replace('/', '-').Replace(' ', '_').Replace(':', '-') + "_日语.xlsx";
                    Log.OutputExcel(LogName, RandomList, "日语");
                }

                JpWord CurrentWord = new JpWord();
                int totalWords = RandomList.Count;
                int completedWords = 0;

                while (CopyList.Count != 0)
                {
                    if (pushJpWords.WORD_CURRENT_STATUS != 3)
                        CurrentWord = pushJpWords.GetRandomWord(CopyList);

                    // 计算学习状态
                    completedWords = totalWords - CopyList.Count;
                    int remainingWords = CopyList.Count;
                    string currentStatus = completedWords == 0 ? "新开" : "学习中";

                    // 推送单词UI（不包含自动发音，避免重复）
                    pushJpWords.PushOneWordWithStatusNoAudio(CurrentWord, currentStatus, remainingWords, completedWords, 0);

                    // 智能延迟确保UI完全渲染后再播放音频
                    if (Select.AUTO_PLAY != 0)
                    {
                        string debugMsg = $"多个日语单词抽背中自动播放发音，单词: {CurrentWord.hiragana}";
                        System.Diagnostics.Debug.WriteLine(debugMsg);

                        // 智能延迟：基础延迟 + 根据内容长度和按钮数量调整
                        int baseDelay = 600; // 日语基础延迟600ms（比英语稍长，因为有更多按钮）
                        int contentLength = CurrentWord.headWord.Length + CurrentWord.hiragana.Length + CurrentWord.tranCN.Length;
                        int buttonDelay = 4 * 50; // 4个按钮，每个50ms
                        int smartDelay = baseDelay + Math.Min(contentLength * 8, 250) + buttonDelay; // 最多额外250ms内容延迟

                        System.Diagnostics.Debug.WriteLine($"计算的智能延迟: {smartDelay}ms");
                        System.Threading.Thread.Sleep(smartDelay);

                        // 播放音频，确保音量一致
                        using (var synth = new System.Speech.Synthesis.SpeechSynthesizer())
                        {
                            // 设置统一的音量和语速
                            synth.Volume = 100; // 设置最大音量，确保与其他地方一致
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
                                        System.Diagnostics.Debug.WriteLine($"选择日语语音: {voice.VoiceInfo.Name}");
                                        break;
                                    }
                                }
                            }
                            catch
                            {
                                System.Diagnostics.Debug.WriteLine("未找到日语语音，使用默认语音");
                            }

                            synth.SpeakAsync(CurrentWord.hiragana);
                            System.Diagnostics.Debug.WriteLine($"播放日语发音: {CurrentWord.hiragana}，音量: {synth.Volume}");
                        }
                    }

                    pushJpWords.WORD_CURRENT_STATUS = 2;
                    while (pushJpWords.WORD_CURRENT_STATUS == 2)
                    {
                        var task = pushJpWords.ProcessToastNotificationRecitationSM2();
                        if (task.Result == 1) // again - 没有印象
                        {
                            pushJpWords.WORD_CURRENT_STATUS = 0; // 重新学习
                        }
                        else if (task.Result == 2) // hard - 记忆模糊
                        {
                            pushJpWords.WORD_CURRENT_STATUS = 1; // 标记为已学
                        }
                        else if (task.Result == 3) // good - 暂时记住
                        {
                            pushJpWords.WORD_CURRENT_STATUS = 1; // 标记为已学
                        }
                        else if (task.Result == 4) // easy - 已经牢记
                        {
                            pushJpWords.WORD_CURRENT_STATUS = 1; // 标记为已学
                        }
                        else if (task.Result == 0) // voice - 发音
                        {
                            pushJpWords.WORD_CURRENT_STATUS = 3;
                            try
                            {
                                System.Diagnostics.Debug.WriteLine($"用户点击发音按钮，准备播放: {CurrentWord.hiragana}");
                                // 使用和随机抽背相同的发音实现
                                pushJpWords.PlayJapanesePronunciation(CurrentWord);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"日语发音播放失败: {ex.Message}");
                                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                            }
                        }
                    }
                    if (pushJpWords.WORD_CURRENT_STATUS == 1)
                    {
                        if (ImportFlag == false)
                        {
                            Query.UpdateWord(CurrentWord.wordRank);
                            Query.UpdateCount();
                        }
                        CopyList.Remove(CurrentWord);
                    }
                }
                // 检查是否启用抽背完成后的测试环节
                if (Select.ENABLE_TEST_AFTER_RECITATION == 1)
                {
                    pushJpWords.PushMessage("背完了！接下来开始测验！");
                    Thread.Sleep(3000);
                }
                else
                {
                    pushJpWords.PushMessage("抽背完成！测试环节已禁用，学习结束。");
                    System.Diagnostics.Debug.WriteLine("日语抽背完成后的测试环节已禁用");
                    return; // 直接结束，不进行测试
                }

                while (RandomList.Count != 0 && Select.ENABLE_TEST_AFTER_RECITATION == 1)
                {
                    ToastNotificationManagerCompat.History.Clear();
                    Thread.Sleep(500);
                    CurrentWord = pushJpWords.GetRandomWord(RandomList);
                    List<JpWord> FakeWordList = Query.GetRandomJpWords(2);

                    pushJpWords.PushOneTransQuestion(CurrentWord, FakeWordList[0].headWord, FakeWordList[1].headWord);

                    pushJpWords.QUESTION_CURRENT_STATUS = 2;
                    while (pushJpWords.QUESTION_CURRENT_STATUS == 2)
                    {
                        var task = pushJpWords.ProcessToastNotificationQuestion();
                        if (task.Result == 1)
                            pushJpWords.QUESTION_CURRENT_STATUS = 1;
                        else if (task.Result == 0)
                            pushJpWords.QUESTION_CURRENT_STATUS = 0;
                        else if (task.Result == -1)
                            pushJpWords.QUESTION_CURRENT_STATUS = -1;
                    }

                    if (pushJpWords.QUESTION_CURRENT_STATUS == 1)
                    {
                        RandomList.Remove(CurrentWord);
                        Thread.Sleep(500);
                    }
                    else if (pushJpWords.QUESTION_CURRENT_STATUS == 0)
                    {
                        //CopyList.Remove(CurrentWord);
                        new ToastContentBuilder()
                        .AddText("错误 正确答案：" + pushJpWords.AnswerDict[pushJpWords.QUESTION_CURRENT_RIGHT_ANSWER.ToString()] + '.' + CurrentWord.headWord)
                        .Show();
                        Thread.Sleep(3000);
                    }
                }

                ToastNotificationManagerCompat.History.Clear();
                pushJpWords.PushMessage("结束了！恭喜！");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"日语抽背过程中发生错误: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");

                try
                {
                    PushJpWords pushJpWords = new PushJpWords();
                    pushJpWords.PushMessage($"日语抽背出现错误: {ex.Message}");
                }
                catch
                {
                    // 如果连推送消息都失败了，至少记录到调试输出
                    System.Diagnostics.Debug.WriteLine("无法推送错误消息");
                }
            }
        }

        public static new void UnorderWord(Object Num)
        {
            int Number = (int)Num;
            Select Query = new Select();
            PushJpWords pushJpWords = new PushJpWords();
            List<JpWord> TestList = Query.GetRandomJpWords(Number);

            CreateLog Log = new CreateLog();
            String LogName = "Log\\" + DateTime.Now.ToString().Replace('/', '-').Replace(' ', '_').Replace(':', '-') + "_随机日语单词.xlsx";
            Log.OutputExcel(LogName, TestList, "日语");

            JpWord CurrentWord = new JpWord();

            while (TestList.Count != 0)
            {
                ToastNotificationManagerCompat.History.Clear();
                Thread.Sleep(500);
                CurrentWord = pushJpWords.GetRandomWord(TestList);
                List<JpWord> FakeWordList = Query.GetRandomJpWords(2);

                pushJpWords.PushOneTransQuestion(CurrentWord, FakeWordList[0].headWord, FakeWordList[1].headWord);

                pushJpWords.QUESTION_CURRENT_STATUS = 2;
                while (pushJpWords.QUESTION_CURRENT_STATUS == 2)
                {
                    var task = pushJpWords.ProcessToastNotificationQuestion();
                    if (task.Result == 1)
                        pushJpWords.QUESTION_CURRENT_STATUS = 1;
                    else if (task.Result == 0)
                        pushJpWords.QUESTION_CURRENT_STATUS = 0;
                    else if (task.Result == -1)
                        pushJpWords.QUESTION_CURRENT_STATUS = -1;
                }

                if (pushJpWords.QUESTION_CURRENT_STATUS == 1)
                {
                    TestList.Remove(CurrentWord);
                    Thread.Sleep(500);
                }
                else if (pushJpWords.QUESTION_CURRENT_STATUS == 0)
                {
                    //CopyList.Remove(CurrentWord);
                    new ToastContentBuilder()
                    .AddText("错误 正确答案：" + pushJpWords.AnswerDict[pushJpWords.QUESTION_CURRENT_RIGHT_ANSWER.ToString()] + '.' + CurrentWord.headWord)
                    .Show();
                    Thread.Sleep(3000);
                }
            }
            ToastNotificationManagerCompat.History.Clear();
            pushJpWords.PushMessage("结束了！恭喜！");
        }
    }
}
