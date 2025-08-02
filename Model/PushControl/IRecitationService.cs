using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ToastFish.Model.SqliteControl;

namespace ToastFish.Model.PushControl
{
    /// <summary>
    /// 统一的抽背服务接口
    /// </summary>
    public interface IRecitationService
    {
        /// <summary>
        /// 推送单个单词并自动发音
        /// </summary>
        Task PushWordWithAutoAudioAsync(object word);

        /// <summary>
        /// 获取语言类型
        /// </summary>
        AudioManager.AudioType GetLanguageType();

        /// <summary>
        /// 准备在线发音参数
        /// </summary>
        List<string> PrepareOnlineAudioParams(object word);

        /// <summary>
        /// 获取发音文本
        /// </summary>
        string GetPronunciationText(object word);
    }

    /// <summary>
    /// 英语抽背服务
    /// </summary>
    public class EnglishRecitationService : IRecitationService
    {
        private readonly PushWords _pushWords;

        public EnglishRecitationService(PushWords pushWords)
        {
            _pushWords = pushWords;
        }

        public async Task PushWordWithAutoAudioAsync(object word)
        {
            if (!(word is Word englishWord))
            {
                throw new ArgumentException("Expected Word object for English recitation");
            }

            // 推送单词UI
            _pushWords.PushOneWord(englishWord);

            // 请求自动发音
            if (Select.AUTO_PLAY != 0)
            {
                var audioParams = PrepareOnlineAudioParams(englishWord);
                var pronunciationText = GetPronunciationText(englishWord);

                await AudioManager.Instance.RequestPlayAudioAsync(
                    pronunciationText, 
                    GetLanguageType(), 
                    audioParams
                );

                System.Diagnostics.Debug.WriteLine($"英语单词发音请求已提交: {englishWord.headWord}");
            }
        }

        public AudioManager.AudioType GetLanguageType()
        {
            return AudioManager.AudioType.English;
        }

        public List<string> PrepareOnlineAudioParams(object word)
        {
            if (!(word is Word englishWord))
                return null;

            string word_pron, word_save_name;
            switch (Select.ENG_TYPE)
            {
                case 1:
                    word_save_name = englishWord.headWord + "_us";
                    word_pron = englishWord.headWord + "&type=1";
                    break;
                default:
                    word_save_name = englishWord.headWord + "_uk";
                    word_pron = englishWord.headWord + "&type=2";
                    break;
            }

            return new List<string> { word_save_name, word_pron };
        }

        public string GetPronunciationText(object word)
        {
            if (!(word is Word englishWord))
                return string.Empty;

            return englishWord.headWord;
        }
    }

    /// <summary>
    /// 日语抽背服务
    /// </summary>
    public class JapaneseRecitationService : IRecitationService
    {
        private readonly PushJpWords _pushJpWords;

        public JapaneseRecitationService(PushJpWords pushJpWords)
        {
            _pushJpWords = pushJpWords;
        }

        public async Task PushWordWithAutoAudioAsync(object word)
        {
            if (!(word is JpWord japaneseWord))
            {
                throw new ArgumentException("Expected JpWord object for Japanese recitation");
            }

            // 推送单词UI（不包含自动发音）
            _pushJpWords.PushOneWord(japaneseWord);

            // 请求自动发音
            if (Select.AUTO_PLAY != 0)
            {
                var pronunciationText = GetPronunciationText(japaneseWord);

                await AudioManager.Instance.RequestPlayAudioAsync(
                    pronunciationText, 
                    GetLanguageType()
                );

                System.Diagnostics.Debug.WriteLine($"日语单词发音请求已提交: {japaneseWord.hiragana}");
            }
        }

        public AudioManager.AudioType GetLanguageType()
        {
            return AudioManager.AudioType.Japanese;
        }

        public List<string> PrepareOnlineAudioParams(object word)
        {
            // 日语暂不支持在线发音
            return null;
        }

        public string GetPronunciationText(object word)
        {
            if (!(word is JpWord japaneseWord))
                return string.Empty;

            return japaneseWord.hiragana;
        }
    }

    /// <summary>
    /// 统一的抽背管理器
    /// </summary>
    public class RecitationManager
    {
        private static readonly Lazy<RecitationManager> _instance = new Lazy<RecitationManager>(() => new RecitationManager());
        public static RecitationManager Instance => _instance.Value;

        private RecitationManager() { }

        /// <summary>
        /// 创建适当的抽背服务
        /// </summary>
        public IRecitationService CreateRecitationService(string language, object pushInstance)
        {
            switch (language.ToLower())
            {
                case "english":
                case "en":
                    if (pushInstance is PushWords pushWords)
                        return new EnglishRecitationService(pushWords);
                    break;

                case "japanese":
                case "jp":
                case "ja":
                    if (pushInstance is PushJpWords pushJpWords)
                        return new JapaneseRecitationService(pushJpWords);
                    break;
            }

            throw new NotSupportedException($"Language '{language}' is not supported");
        }

        /// <summary>
        /// 统一的多个单词抽背方法
        /// </summary>
        public async Task ProcessMultipleWordsAsync<T>(List<T> words, IRecitationService service, Func<T, Task<bool>> userInteractionHandler)
        {
            if (words == null || words.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("单词列表为空，跳过抽背");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"开始多个单词抽背，共 {words.Count} 个单词");

            foreach (var word in words)
            {
                try
                {
                    // 推送单词并自动发音
                    await service.PushWordWithAutoAudioAsync(word);

                    // 等待用户交互
                    bool shouldContinue = await userInteractionHandler(word);
                    if (!shouldContinue)
                    {
                        System.Diagnostics.Debug.WriteLine("用户中断抽背流程");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"处理单词时出错: {ex.Message}");
                }
            }

            System.Diagnostics.Debug.WriteLine("多个单词抽背完成");
        }
    }
}
