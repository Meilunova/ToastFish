using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using ToastFish.ViewModel;
using ToastFish.Resources;
using System.Windows.Forms;
using ToastFish.Model.SqliteControl;
using System.Threading;
using ToastFish.Model.Mp3;
using System.Diagnostics;
using ToastFish.Model.PushControl;
using ToastFish.Model.Log;
using System.Speech.Synthesis;
using ToastFish.Model.StartWithWindows;
using System.IO;
using System.Windows.Xps.Packaging;
using System.Windows.Input;
using ToastFish.Model.RandomSchedule;
using ToastFish.Test;

namespace ToastFish
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        ToastFishModel Vm = new ToastFishModel();
        Select Se = new Select();
        PushWords pushWords = new PushWords();
        Thread thread = new Thread(new ParameterizedThreadStart(PushWords.Recitation));

        // 随机抽背功能
        private ScheduleConfig _scheduleConfig;
        private RandomScheduler _randomScheduler;
        Dictionary<string, string> TablelDictionary = new Dictionary<string, string>(){
        {"CET4_1", "四级核心词汇"},{"CET4_3", "四级完整词汇"},{"CET6_1", "六级核心词汇"},
        {"CET6_3", "六级完整词汇"},{"GMAT_3", "GMAT词汇"},{"GRE_2", "GRE词汇"},
        {"IELTS_3", "IELTS词汇"},{"TOEFL_2", "TOEFL词汇"},{"SAT_2", "SAT词汇"},
        {"KaoYan_1", "考研必考词汇"},{"KaoYan_2", "考研完整词汇"},{"Level4_1", "专四真题高频词"},
        {"Level4luan_2", "专四核心词汇"},{"Level8_1", "专八真题高频词"},{"Level8luan_2", "专八核心词汇"},
        {"Goin", "顺序五十音"},{"StdJp_Mid", "标准日本语中级词汇"} };
        // private NotifyIcon _notifyIcon = null;
        //HotKey _hotKey0, _hotKey1, _hotKey2, _hotKey3, _hotKey4;
        public MainWindow()
        {
            Form_Load();
            InitializeComponent();
            DataContext = Vm;
            SetNotifyIcon();
            this.Visibility = Visibility.Hidden;
            Se.LoadGlobalConfig();

            // 初始化随机抽背功能
            try
            {
                _scheduleConfig = ScheduleConfig.LoadFromFile();
                _randomScheduler = new RandomScheduler(_scheduleConfig);

                System.Diagnostics.Debug.WriteLine($"随机抽背配置加载完成: 启用={_scheduleConfig.IsEnabled}");

                // 如果配置启用了随机抽背，自动启动调度器
                if (_scheduleConfig.IsEnabled)
                {
                    _randomScheduler.Start();
                    System.Diagnostics.Debug.WriteLine("随机抽背调度器已自动启动");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("随机抽背功能未启用，调度器未启动");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初始化随机抽背功能失败: {ex.Message}");
                // 创建默认配置
                _scheduleConfig = new ScheduleConfig();
                _randomScheduler = new RandomScheduler(_scheduleConfig);
            }

            ContextMenu();
            new HotKey(Key.Oem3, KeyModifier.Alt, OnHotKeyHandler);
            new HotKey(Key.D1, KeyModifier.Alt, OnHotKeyHandler);
            new HotKey(Key.D2, KeyModifier.Alt, OnHotKeyHandler);
            new HotKey(Key.D3, KeyModifier.Alt, OnHotKeyHandler);
            new HotKey(Key.D4, KeyModifier.Alt, OnHotKeyHandler);
            new HotKey(Key.Q, KeyModifier.Alt, OnHotKeyHandler);

            // 谜之bug，如果不先播放一段音频，那么什么声音都播不出来。
            // 所以播个没声音的音频先。
            PlayMute();
            //this.WindowState = (WindowState)FormWindowState.Minimized;
        }

        private void OnHotKeyHandler(HotKey hotKey)
        {
            string key = hotKey.Key.ToString();
            Debug.WriteLine("key pressed:" + key);
            switch (key)
            {
                case "Q":
                    Begin_Click(null, null);
                    break;
                case "D1":
                    PushWords.HotKeytObservable.raiseEvent("1");
                    break;
                case "D2":
                    PushWords.HotKeytObservable.raiseEvent("2");
                    break;
                case "D3":
                    PushWords.HotKeytObservable.raiseEvent("3");
                    break;
                case "D4":
                    PushWords.HotKeytObservable.raiseEvent("4");
                    break;
                case "Oem3":
                    PushWords.HotKeytObservable.raiseEvent("S");
                    break;
                default:
                    break;
            }
        }

        private void Form_Load()

        {
            //获取当前活动进程的模块名称
            string moduleName = Process.GetCurrentProcess().MainModule.ModuleName;
            //返回指定路径字符串的文件名
            string processName = System.IO.Path.GetFileNameWithoutExtension(moduleName);
            //根据文件名创建进程资源数组
            Process[] processes = Process.GetProcessesByName(processName);
            //如果该数组长度大于1，说明多次运行
            if (processes.Length > 1)
            {
                System.Windows.Forms.MessageBox.Show("程序已经在运行了，不能运行两次。\n如果右下角软件已经退出，请在任务管理器中结束ToastFish任务。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);//弹出提示信息
                this.Close();//关闭当前窗体
            }
        }

        private void SetNotifyIcon()
        {
            Vm.notifyIcon = new NotifyIcon();
            Vm.notifyIcon.Text = "ToastFish";
            System.Drawing.Icon icon = IconChika.chika16;

            Vm.notifyIcon.Icon = icon;
            Vm.notifyIcon.Visible = true;
            Vm.notifyIcon.DoubleClick += Begin_Click;
            //Vm.notifyIcon.DoubleClick += NotifyIconDoubleClick;
        }

        public void PlayMute()
        {
            MUSIC Temp = new MUSIC();
            Temp.FileName = ".\\Resources\\mute.mp3";
            Temp.play();
        }

        private void NotifyIconDoubleClick(object sender, EventArgs e)
        {
            this.Activate();
            this.WindowState = WindowState.Normal;
            this.ShowInTaskbar = true;
            this.Topmost = true;
            this.Show();
        }

        #region 托盘右键菜单

        System.Windows.Forms.ToolStripMenuItem Begin = new System.Windows.Forms.ToolStripMenuItem();
        System.Windows.Forms.ToolStripMenuItem Settings = new System.Windows.Forms.ToolStripMenuItem();
        System.Windows.Forms.ToolStripMenuItem SetNumber = new System.Windows.Forms.ToolStripMenuItem();
        System.Windows.Forms.ToolStripMenuItem SetEngType = new System.Windows.Forms.ToolStripMenuItem();
        System.Windows.Forms.ToolStripMenuItem ImportWords = new System.Windows.Forms.ToolStripMenuItem();
        System.Windows.Forms.ToolStripMenuItem SelectBook = new System.Windows.Forms.ToolStripMenuItem();
        System.Windows.Forms.ToolStripMenuItem SelectJpBook = new System.Windows.Forms.ToolStripMenuItem();
        System.Windows.Forms.ToolStripMenuItem RandomTest = new System.Windows.Forms.ToolStripMenuItem();

        System.Windows.Forms.ToolStripMenuItem GotoHtml = new System.Windows.Forms.ToolStripMenuItem();
        System.Windows.Forms.ToolStripMenuItem Start = new System.Windows.Forms.ToolStripMenuItem();
        System.Windows.Forms.ToolStripMenuItem ExitMenuItem = new System.Windows.Forms.ToolStripMenuItem();

        System.Windows.Forms.ToolStripMenuItem SetAutoPlay = new System.Windows.Forms.ToolStripMenuItem();
        System.Windows.Forms.ToolStripMenuItem SetAutoLog = new System.Windows.Forms.ToolStripMenuItem();
        System.Windows.Forms.ToolStripMenuItem SetTestAfterRecitation = new System.Windows.Forms.ToolStripMenuItem();
        System.Windows.Forms.ToolStripMenuItem SetTestAfterStudy = new System.Windows.Forms.ToolStripMenuItem();
        System.Windows.Forms.ToolStripMenuItem RandomScheduleSettings = new System.Windows.Forms.ToolStripMenuItem();

        private new void ContextMenu()
        {
            ContextMenuStrip Cms = new ContextMenuStrip();

            Vm.notifyIcon.ContextMenuStrip = Cms;


            Begin.Text = "开始！";
            Begin.Click += new EventHandler(Begin_Click);
            Settings.Text = "参数设置";


            SetNumber.Text = "单词个数";
            SetNumber.Click += new EventHandler(SetNumber_Click);

            SetEngType.Text = "英标类型";
            SetEngType.Click += new EventHandler(SetEngType_Click);

            SetAutoPlay.Text = "自动播放";
            SetAutoPlay.Click += new EventHandler(AutoPlay_Click);
            if (Select.AUTO_PLAY != 0)
                SetAutoPlay.Checked = true;
            else
                SetAutoPlay.Checked = false;

            SetAutoLog.Text = "自动日志";
            SetAutoLog.Click += new EventHandler(AutoLog_Click);
            if (Select.AUTO_LOG != 0)
                SetAutoLog.Checked = true;
            else
                SetAutoLog.Checked = false;

            SetTestAfterRecitation.Text = "抽背后测试";
            SetTestAfterRecitation.Click += new EventHandler(TestAfterRecitation_Click);
            if (Select.ENABLE_TEST_AFTER_RECITATION != 0)
                SetTestAfterRecitation.Checked = true;
            else
                SetTestAfterRecitation.Checked = false;

            SetTestAfterStudy.Text = "背诵后测试";
            SetTestAfterStudy.Click += new EventHandler(TestAfterStudy_Click);
            if (Select.ENABLE_TEST_AFTER_STUDY != 0)
                SetTestAfterStudy.Checked = true;
            else
                SetTestAfterStudy.Checked = false;

            RandomScheduleSettings.Text = "随机抽背设置";
            RandomScheduleSettings.Click += new EventHandler(RandomScheduleSettings_Click);


            ImportWords.Text = "导入单词";
            ImportWords.Click += new EventHandler(ImportWords_Click);

            SelectBook.Text = "英语词汇";

            SelectJpBook.Text = "日语词汇";

            RandomTest.Text = "随机测试";

            GotoHtml.Text = "使用说明";
            GotoHtml.Click += new EventHandler(HowToUse_Click);

            Start.Text = "开机启动";
            Start.Click += new EventHandler(Start_Click);
            if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "ToastFish.lnk")))
                Start.Checked = true;
            else
                Start.Checked = false;

            ExitMenuItem.Text = "退出";
            ExitMenuItem.Click += new EventHandler(ExitApp_Click);

            ToolStripItem CET4_1 = new ToolStripMenuItem("四级核心词汇");
            CET4_1.Click += new EventHandler(SelectBook_Click);
            ToolStripItem CET4_3 = new ToolStripMenuItem("四级完整词汇");
            CET4_3.Click += new EventHandler(SelectBook_Click);
            ToolStripItem CET6_1 = new ToolStripMenuItem("六级核心词汇");
            CET6_1.Click += new EventHandler(SelectBook_Click);
            ToolStripItem CET6_3 = new ToolStripMenuItem("六级完整词汇");
            CET6_3.Click += new EventHandler(SelectBook_Click);
            ToolStripItem GMAT_3 = new ToolStripMenuItem("GMAT词汇");
            GMAT_3.Click += new EventHandler(SelectBook_Click);
            ToolStripItem GRE_2 = new ToolStripMenuItem("GRE词汇");
            GRE_2.Click += new EventHandler(SelectBook_Click);
            ToolStripItem IELTS_3 = new ToolStripMenuItem("IELTS词汇");
            IELTS_3.Click += new EventHandler(SelectBook_Click);
            ToolStripItem TOEFL_2 = new ToolStripMenuItem("TOEFL词汇");
            TOEFL_2.Click += new EventHandler(SelectBook_Click);
            ToolStripItem SAT_2 = new ToolStripMenuItem("SAT词汇");
            SAT_2.Click += new EventHandler(SelectBook_Click);
            ToolStripItem KaoYan_1 = new ToolStripMenuItem("考研必考词汇");
            KaoYan_1.Click += new EventHandler(SelectBook_Click);
            ToolStripItem KaoYan_2 = new ToolStripMenuItem("考研完整词汇");
            KaoYan_2.Click += new EventHandler(SelectBook_Click);
            ToolStripItem Level4_1 = new ToolStripMenuItem("专四真题高频词");
            Level4_1.Click += new EventHandler(SelectBook_Click);
            ToolStripItem Level4luan_2 = new ToolStripMenuItem("专四核心词汇");
            Level4luan_2.Click += new EventHandler(SelectBook_Click);
            ToolStripItem Level8_1 = new ToolStripMenuItem("专八真题高频词");
            Level8_1.Click += new EventHandler(SelectBook_Click);
            ToolStripItem Level8luan_2 = new ToolStripMenuItem("专八核心词汇");
            Level8luan_2.Click += new EventHandler(SelectBook_Click);
            ToolStripItem Goin = new ToolStripMenuItem("顺序五十音");
            Goin.Click += new EventHandler(SelectBook_Click);
            ToolStripItem StdJp_Mid = new ToolStripMenuItem("标准日本语中级词汇");
            StdJp_Mid.Click += new EventHandler(SelectBook_Click);
            ToolStripItem RandomWord = new ToolStripMenuItem("随机单词测试");
            RandomWord.Click += new EventHandler(RandomWordTest_Click);
            ToolStripItem RandomGoin = new ToolStripMenuItem("随机五十音测试");
            RandomGoin.Click += new EventHandler(RandomGoinTest_Click);
            ToolStripItem RandomJpWord = new ToolStripMenuItem("随机日语单词测试");
            RandomJpWord.Click += new EventHandler(RandomJpWordTest_Click);
            ToolStripItem Pdf = new ToolStripMenuItem("Star!!");
            Pdf.Click += new EventHandler(OpenPdf_Click);
            ToolStripItem Use = new ToolStripMenuItem("使用说明(必读)");
            Use.Click += new EventHandler(HowToUse_Click);
            ToolStripItem Site = new ToolStripMenuItem("官方网站");
            Site.Click += new EventHandler(Site_Click);
            ToolStripItem Shortcuts = new ToolStripMenuItem("快捷方式");
            Shortcuts.Click += new EventHandler(ShortCuts_Click);
            ToolStripItem ResetLearingStatus = new ToolStripMenuItem("重置进度");
            ResetLearingStatus.Click += new EventHandler(ResetLearingStatus_Click);
            ToolStripItem TestNotification = new ToolStripMenuItem("测试通知");
            TestNotification.Click += new EventHandler(TestNotification_Click);







            if (Select.TABLE_NAME == "CET4_1")
                CET4_1.PerformClick();
            else if (Select.TABLE_NAME == "CET4_3")
                CET4_3.PerformClick();
            else if (Select.TABLE_NAME == "CET6_1")
                CET6_1.PerformClick();
            else if (Select.TABLE_NAME == "CET6_3")
                CET6_3.PerformClick();
            else if (Select.TABLE_NAME == "GMAT_3")
                GMAT_3.PerformClick();
            else if (Select.TABLE_NAME == "GRE_2")
                GRE_2.PerformClick();
            else if (Select.TABLE_NAME == "IELTS_3")
                IELTS_3.PerformClick();
            else if (Select.TABLE_NAME == "TOEFL_2")
                TOEFL_2.PerformClick();
            else if (Select.TABLE_NAME == "SAT_2")
                SAT_2.PerformClick();
            else if (Select.TABLE_NAME == "KaoYan_1")
                KaoYan_1.PerformClick();
            else if (Select.TABLE_NAME == "KaoYan_2")
                KaoYan_2.PerformClick();
            else if (Select.TABLE_NAME == "Level4_1")
                Level4_1.PerformClick();
            else if (Select.TABLE_NAME == "Level4luan_2")
                Level4luan_2.PerformClick();
            else if (Select.TABLE_NAME == "Level8_1")
                Level8_1.PerformClick();
            else if (Select.TABLE_NAME == "Level8luan_2")
                Level8luan_2.PerformClick();
            else if (Select.TABLE_NAME == "Goin")
                Goin.PerformClick();

            Cms.Items.Add(Begin);
            //Cms.Items.Add(SetNumber);
            //Cms.Items.Add(SetEngType);
            Cms.Items.Add(ImportWords);
            Cms.Items.Add(SelectBook);
            Cms.Items.Add(SelectJpBook);
            Cms.Items.Add(RandomTest);
            Cms.Items.Add(Settings);
            Cms.Items.Add(GotoHtml);
            Cms.Items.Add(Start);
            Cms.Items.Add(ExitMenuItem);

            ((ToolStripDropDownItem)Cms.Items[2]).DropDownItems.Add(CET4_1);
            ((ToolStripDropDownItem)Cms.Items[2]).DropDownItems.Add(CET4_3);
            ((ToolStripDropDownItem)Cms.Items[2]).DropDownItems.Add(CET6_1);
            ((ToolStripDropDownItem)Cms.Items[2]).DropDownItems.Add(CET6_3);
            ((ToolStripDropDownItem)Cms.Items[2]).DropDownItems.Add(GMAT_3);
            ((ToolStripDropDownItem)Cms.Items[2]).DropDownItems.Add(GRE_2);
            ((ToolStripDropDownItem)Cms.Items[2]).DropDownItems.Add(IELTS_3);
            ((ToolStripDropDownItem)Cms.Items[2]).DropDownItems.Add(TOEFL_2);
            ((ToolStripDropDownItem)Cms.Items[2]).DropDownItems.Add(SAT_2);
            ((ToolStripDropDownItem)Cms.Items[2]).DropDownItems.Add(KaoYan_1);
            ((ToolStripDropDownItem)Cms.Items[2]).DropDownItems.Add(KaoYan_2);
            ((ToolStripDropDownItem)Cms.Items[2]).DropDownItems.Add(Level4_1);
            ((ToolStripDropDownItem)Cms.Items[2]).DropDownItems.Add(Level4luan_2);
            ((ToolStripDropDownItem)Cms.Items[2]).DropDownItems.Add(Level8_1);
            ((ToolStripDropDownItem)Cms.Items[2]).DropDownItems.Add(Level8luan_2);
            ((ToolStripDropDownItem)Cms.Items[3]).DropDownItems.Add(Goin);
            ((ToolStripDropDownItem)Cms.Items[3]).DropDownItems.Add(StdJp_Mid);
            ((ToolStripDropDownItem)Cms.Items[4]).DropDownItems.Add(RandomWord);
            ((ToolStripDropDownItem)Cms.Items[4]).DropDownItems.Add(RandomGoin);
            ((ToolStripDropDownItem)Cms.Items[4]).DropDownItems.Add(RandomJpWord);
            ((ToolStripDropDownItem)Cms.Items[5]).DropDownItems.Add(SetNumber);
            ((ToolStripDropDownItem)Cms.Items[5]).DropDownItems.Add(SetEngType);
            ((ToolStripDropDownItem)Cms.Items[5]).DropDownItems.Add(SetAutoPlay);
            ((ToolStripDropDownItem)Cms.Items[5]).DropDownItems.Add(SetAutoLog);
            ((ToolStripDropDownItem)Cms.Items[5]).DropDownItems.Add(SetTestAfterRecitation);
            ((ToolStripDropDownItem)Cms.Items[5]).DropDownItems.Add(SetTestAfterStudy);
            ((ToolStripDropDownItem)Cms.Items[5]).DropDownItems.Add(RandomScheduleSettings);
            ((ToolStripDropDownItem)Cms.Items[5]).DropDownItems.Add(ResetLearingStatus);
            ((ToolStripDropDownItem)Cms.Items[5]).DropDownItems.Add(TestNotification);

            ((ToolStripDropDownItem)Cms.Items[6]).DropDownItems.Add(Shortcuts);
            ((ToolStripDropDownItem)Cms.Items[6]).DropDownItems.Add(Use);
            ((ToolStripDropDownItem)Cms.Items[6]).DropDownItems.Add(Site);
            ((ToolStripDropDownItem)Cms.Items[6]).DropDownItems.Add(Pdf);
        }

        private void Begin_Click(object sender, EventArgs e)
        {
            if (!System.IO.Directory.Exists("Log"))
            {
                System.IO.Directory.CreateDirectory("Log");
            }
            // System.IO.Directory.CreateDirectory("Log");

            var state = thread.ThreadState;

            WordType Words = new WordType();
            Words.Number = Select.WORD_NUMBER;

            if (state == System.Threading.ThreadState.WaitSleepJoin || state == System.Threading.ThreadState.Stopped)
            {
                thread.Abort();
                while (thread.ThreadState != System.Threading.ThreadState.Aborted)
                {
                    Thread.Sleep(100);
                }
                if (Select.TABLE_NAME == "Goin")
                    thread = new Thread(new ParameterizedThreadStart(PushGoinWords.OrderGoin));
                else if (Select.TABLE_NAME == "StdJp_Mid")
                {
                    // 先测试日语词库功能
                    try
                    {
                        System.Diagnostics.Debug.WriteLine("开始日语词库功能测试...");
                        TestJpWordDatabase();
                        thread = new Thread(new ParameterizedThreadStart(PushJpWords.Recitation));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"日语词库测试失败: {ex.Message}");
                        string errorMsg = $"日语词库初始化失败:\n{ex.Message}";
                        if (ex.InnerException != null)
                        {
                            errorMsg += $"\n详细错误: {ex.InnerException.Message}";
                        }
                        pushWords.PushMessage(errorMsg);

                        // 显示详细错误对话框
                        System.Windows.Forms.MessageBox.Show(
                            errorMsg + $"\n\n堆栈跟踪:\n{ex.StackTrace}",
                            "日语词库错误",
                            System.Windows.Forms.MessageBoxButtons.OK,
                            System.Windows.Forms.MessageBoxIcon.Error);
                        return;
                    }
                }
                //else if (Select.TABLE_NAME == "自定义英语")
                //    thread = new Thread(new ParameterizedThreadStart(PushWords.Recitation));
                else
                    thread = new Thread(new ParameterizedThreadStart(PushWords.RecitationSM2));

                thread.Start(Words);
            }
            else
            {
                if (Select.TABLE_NAME == "Goin")
                    thread = new Thread(new ParameterizedThreadStart(PushGoinWords.OrderGoin));
                else if (Select.TABLE_NAME == "StdJp_Mid")
                {
                    // 先测试日语词库功能
                    try
                    {
                        System.Diagnostics.Debug.WriteLine("开始日语词库功能测试...");
                        TestJpWordDatabase();
                        thread = new Thread(new ParameterizedThreadStart(PushJpWords.Recitation));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"日语词库测试失败: {ex.Message}");
                        string errorMsg = $"日语词库初始化失败:\n{ex.Message}";
                        if (ex.InnerException != null)
                        {
                            errorMsg += $"\n详细错误: {ex.InnerException.Message}";
                        }
                        pushWords.PushMessage(errorMsg);

                        // 显示详细错误对话框
                        System.Windows.Forms.MessageBox.Show(
                            errorMsg + $"\n\n堆栈跟踪:\n{ex.StackTrace}",
                            "日语词库错误",
                            System.Windows.Forms.MessageBoxButtons.OK,
                            System.Windows.Forms.MessageBoxIcon.Error);
                        return;
                    }
                }
                //else if (Select.TABLE_NAME == "自定义英语")
                //    thread = new Thread(new ParameterizedThreadStart(PushWords.Recitation));
                else
                    thread = new Thread(new ParameterizedThreadStart(PushWords.RecitationSM2));

                thread.Start(Words);
            }
        }

        private void SetNumber_Click(object sender, EventArgs e)
        {
            Thread thread = new Thread(new ThreadStart(pushWords.SetWordNumber));
            thread.Start();
        }

        private void SetEngType_Click(object sender, EventArgs e)
        {
            Thread thread = new Thread(new ThreadStart(pushWords.SetEngType));
            thread.Start();
        }




        private void ImportWords_Click(object sender, EventArgs e)
        {
            OpenFileDialog Dialog = new OpenFileDialog();
            Dialog.Filter = "xlsx files (*.xlsx)|*.xlsx|xls files (*.xls)|*.xls";
            if (Dialog.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
            {
                return;
            }
            String FileName = Dialog.FileName;
            CreateLog Log = new CreateLog();
            WordType Words = new WordType();
            Words.Number = Select.WORD_NUMBER;
            object lstObj = Log.ImportExcel(FileName);
            string typeObj = lstObj.ToString();
            string typeWord = typeof(List<Word>).ToString();
            string typeJpWord = typeof(List<JpWord>).ToString();
            string typeCustWord = typeof(List<CustomizeWord>).ToString();
            try
            {
                if (typeObj == typeWord)
                {
                    Words.WordList = (List<Word>)lstObj;
                    Select.TABLE_NAME = "GRE_2";
                }
                else if (typeObj == typeJpWord)
                {
                    Words.JpWordList = (List<JpWord>)lstObj;
                    Select.TABLE_NAME = "StdJp_Mid";
                }
                else if (typeObj == typeCustWord)
                {
                    Words.CustWordList = (List<CustomizeWord>)lstObj;
                    Select.TABLE_NAME = "自定义";
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("导入文件出错！");
                    return;
                }
            }
            catch
            {
                System.Windows.Forms.MessageBox.Show("导入文件出错！");
                return;
            }

            if (!Directory.Exists("Log"))
            {
                System.IO.Directory.CreateDirectory("Log");
            }


            var state = thread.ThreadState;

            if (state == System.Threading.ThreadState.WaitSleepJoin || state == System.Threading.ThreadState.Stopped)
            {
                thread.Abort();
                while (thread.ThreadState != System.Threading.ThreadState.Aborted)
                {
                    Thread.Sleep(100);
                }
                if (Select.TABLE_NAME == "Goin")
                    thread = new Thread(new ParameterizedThreadStart(PushGoinWords.OrderGoin));
                else if (Select.TABLE_NAME == "StdJp_Mid")
                    thread = new Thread(new ParameterizedThreadStart(PushJpWords.Recitation));
                //else if (Select.TABLE_NAME == "自定义英语")
                //    thread = new Thread(new ParameterizedThreadStart(PushWords.Recitation));
                else if (Select.TABLE_NAME == "自定义")
                    thread = new Thread(new ParameterizedThreadStart(PushCustomizeWords.Recitation));
                else
                    thread = new Thread(new ParameterizedThreadStart(PushWords.Recitation));

                thread.Start(Words);
            }
            else
            {
                if (Select.TABLE_NAME == "Goin")
                    thread = new Thread(new ParameterizedThreadStart(PushGoinWords.OrderGoin));
                else if (Select.TABLE_NAME == "StdJp_Mid")
                    thread = new Thread(new ParameterizedThreadStart(PushJpWords.Recitation));
                //else if (Select.TABLE_NAME == "自定义英语")
                //    thread = new Thread(new ParameterizedThreadStart(PushWords.Recitation));
                else if (Select.TABLE_NAME == "自定义")
                    thread = new Thread(new ParameterizedThreadStart(PushCustomizeWords.Recitation));
                else
                    thread = new Thread(new ParameterizedThreadStart(PushWords.Recitation));

                thread.Start(Words);
            }
        }

        private void SelectBook_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem curitem = sender as ToolStripMenuItem;
            if (curitem != null && curitem.OwnerItem != null)
            {
                var Cms = (curitem.OwnerItem as ToolStripMenuItem).Owner as ContextMenuStrip;
                //int index = (curitem.OwnerItem as ToolStripMenuItem).DropDownItems.IndexOf(item);
                foreach (var itemi in ((ToolStripDropDownItem)Cms.Items[2]).DropDownItems)
                {
                    (itemi as ToolStripMenuItem).Checked = false;
                }
                foreach (var itemi in ((ToolStripDropDownItem)Cms.Items[3]).DropDownItems)
                {
                    (itemi as ToolStripMenuItem).Checked = false;
                }
            }
            curitem.Checked = true;
            // (sender as ToolStripMenuItem).Checked = !(sender as ToolStripMenuItem).Checked;
            string TempName = "";
            if (sender.ToString() == "四级核心词汇")
                TempName = "CET4_1";
            else if (sender.ToString() == "四级完整词汇")
                TempName = "CET4_3";
            else if (sender.ToString() == "六级核心词汇")
                TempName = "CET6_1";
            else if (sender.ToString() == "六级完整词汇")
                TempName = "CET6_3";
            else if (sender.ToString() == "GMAT词汇")
                TempName = "GMAT_3";
            else if (sender.ToString() == "GRE词汇")
                TempName = "GRE_2";
            else if (sender.ToString() == "IELTS词汇")
                TempName = "IELTS_3";
            else if (sender.ToString() == "TOEFL词汇")
                TempName = "TOEFL_2";
            else if (sender.ToString() == "SAT词汇")
                TempName = "SAT_2";
            else if (sender.ToString() == "考研必考词汇")
                TempName = "KaoYan_1";
            else if (sender.ToString() == "考研完整词汇")
                TempName = "KaoYan_2";
            else if (sender.ToString() == "专四真题高频词")
                TempName = "Level4_1";
            else if (sender.ToString() == "专四核心词汇")
                TempName = "Level4luan_2";
            else if (sender.ToString() == "专八真题高频词")
                TempName = "Level8_1";
            else if (sender.ToString() == "专八核心词汇")
                TempName = "Level8luan_2";
            else if (sender.ToString() == "顺序五十音")
                TempName = "Goin";
            else if (sender.ToString() == "标准日本语中级词汇")
            {
                TempName = "StdJp_Mid";
                bool Flag = false;
                SpeechSynthesizer synth = new SpeechSynthesizer();
                foreach (InstalledVoice voice in synth.GetInstalledVoices())
                {
                    VoiceInfo info = voice.VoiceInfo;
                    if (info.Culture.IetfLanguageTag == "ja-JP")
                        Flag = true;
                }
                if (Flag == false)
                    System.Windows.Forms.MessageBox.Show("检测到您未安装日语语音包，请去“设置”->“时间和语言”->“语音”->“添加语音”中安装日本语，以免影响正常使用。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (sender.ToString() == "随机五十音测试")
                TempName = "Goin";
            Select.TABLE_NAME = TempName;
            Se.UpdateBookName(TempName);
            Se.UpdateTableCount();
            //if (sender.ToString() == "顺序五十音")
            //{
            //     int Progress = Se.GetGoinProgress();
            //     PushWords.PushMessage("当前词库：" + sender.ToString() + "\n当前进度：" + Progress.ToString() + "/104");
            // }
            // else
            //{
            List<int> res = Se.SelectCount();
            pushWords.PushMessage("当前词库：" + sender.ToString() + "\n当前进度：" + res[0].ToString() + "/" + res[1].ToString());
            // }
        }

        private void RandomWordTest_Click(object sender, EventArgs e)
        {
            var state = thread.ThreadState;
            if (state == System.Threading.ThreadState.WaitSleepJoin || state == System.Threading.ThreadState.Stopped)
            {
                thread.Abort();
                while (thread.ThreadState != System.Threading.ThreadState.Aborted)
                {
                    Thread.Sleep(100);
                }
            }
            if (Select.TABLE_NAME == "StdJp_Mid" || Select.TABLE_NAME == "Goin")
                Select.TABLE_NAME = "GRE_2";
            thread = new Thread(new ParameterizedThreadStart(pushWords.UnorderWord));
            thread.Start(Select.WORD_NUMBER);
        }

        private void RandomGoinTest_Click(object sender, EventArgs e)
        {
            Select.TABLE_NAME = "Goin";
            Se.UpdateBookName("Goin");
            var state = thread.ThreadState;
            if (state == System.Threading.ThreadState.WaitSleepJoin || state == System.Threading.ThreadState.Stopped)
            {
                thread.Abort();
                while (thread.ThreadState != System.Threading.ThreadState.Aborted)
                {
                    Thread.Sleep(100);
                }
                if (Select.TABLE_NAME == "Goin")
                    thread = new Thread(new ParameterizedThreadStart(PushGoinWords.UnorderGoin));
                thread.Start(Select.WORD_NUMBER);
            }
            else
            {
                if (Select.TABLE_NAME == "Goin")
                    thread = new Thread(new ParameterizedThreadStart(PushGoinWords.UnorderGoin));
                thread.Start(Select.WORD_NUMBER);
            }
        }

        private void RandomJpWordTest_Click(object sender, EventArgs e)
        {
            var state = thread.ThreadState;
            if (state == System.Threading.ThreadState.WaitSleepJoin || state == System.Threading.ThreadState.Stopped)
            {
                thread.Abort();
                while (thread.ThreadState != System.Threading.ThreadState.Aborted)
                {
                    Thread.Sleep(100);
                }
            }
            Select.TABLE_NAME = "StdJp_Mid";
            thread = new Thread(new ParameterizedThreadStart(PushJpWords.UnorderWord));
            thread.Start(Select.WORD_NUMBER);
        }

        public void ResetLearingStatus_Click(object sender, EventArgs e)
        {
            string TableName;
            bool isok = TablelDictionary.TryGetValue(Select.TABLE_NAME, out TableName);
            if (!isok)
            {
                return;
            }

            DialogResult result = System.Windows.Forms.MessageBox.Show(
            $"是否要重置“{TableName}”的学习进度?", $"进度重置：{Select.TABLE_NAME}",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == System.Windows.Forms.DialogResult.Yes)
            {

                try
                {
                    Se.ResetTableCount();
                    pushWords.PushMessage($"重置{TableName}完成！");
                    //System.Windows.Forms.MessageBox.Show($"重置{TableName}完成！");

                }
                catch
                {
                    pushWords.PushMessage($"重置{TableName}出错！");
                    // System.Windows.Forms.MessageBox.Show($"重置{TableName}出错！");
                }
            }
            //this.Se. 
        }
        private void ShortCuts_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.MessageBox.Show("ALT+Q     ：开始内置单词学习\nALT+~     ：英语单词发音\nALT+1到4：对应点击按钮1到4", "版本号：2.3.3");
        }

        private void AutoPlay_Click(object sender, EventArgs e)
        {
            //sender as ToolStripMenuItem).Checked = !(sender as ToolStripMenuItem).Checked;
            if (Select.AUTO_PLAY == 0)
            {
                Select.AUTO_PLAY = 1;
                (sender as ToolStripMenuItem).Checked = true;
            }
            else
            {
                Select.AUTO_PLAY = 0;
                (sender as ToolStripMenuItem).Checked = false;
            }
            Se.UpdateGlobalConfig();
        }

        private void AutoLog_Click(object sender, EventArgs e)
        {
            //(sender as ToolStripMenuItem).Checked = !(sender as ToolStripMenuItem).Checked;
            if (Select.AUTO_LOG == 0)
            {
                Select.AUTO_LOG = 1;
                (sender as ToolStripMenuItem).Checked = true;
            }
            else
            {
                Select.AUTO_LOG = 0;
                (sender as ToolStripMenuItem).Checked = false;
            }
            Se.UpdateGlobalConfig();
        }

        private void TestAfterRecitation_Click(object sender, EventArgs e)
        {
            if (Select.ENABLE_TEST_AFTER_RECITATION == 0)
            {
                Select.ENABLE_TEST_AFTER_RECITATION = 1;
                (sender as ToolStripMenuItem).Checked = true;
                pushWords.PushMessage("抽背后测试环节已启用");
            }
            else
            {
                Select.ENABLE_TEST_AFTER_RECITATION = 0;
                (sender as ToolStripMenuItem).Checked = false;
                pushWords.PushMessage("抽背后测试环节已禁用");
            }
            Se.UpdateGlobalConfig();
            System.Diagnostics.Debug.WriteLine($"抽背后测试环节设置: {Select.ENABLE_TEST_AFTER_RECITATION}");
        }

        private void TestAfterStudy_Click(object sender, EventArgs e)
        {
            if (Select.ENABLE_TEST_AFTER_STUDY == 0)
            {
                Select.ENABLE_TEST_AFTER_STUDY = 1;
                (sender as ToolStripMenuItem).Checked = true;
                pushWords.PushMessage("背诵后测试环节已启用");
            }
            else
            {
                Select.ENABLE_TEST_AFTER_STUDY = 0;
                (sender as ToolStripMenuItem).Checked = false;
                pushWords.PushMessage("背诵后测试环节已禁用");
            }
            Se.UpdateGlobalConfig();
            System.Diagnostics.Debug.WriteLine($"背诵后测试环节设置: {Select.ENABLE_TEST_AFTER_STUDY}");
        }

        private void HowToUse_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(".\\Resources\\使用说明.html");
        }
        private void Site_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://lab.magiconch.com/toast-fish/");
        }
        private void OpenPdf_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(".\\Resources\\Star.pdf");
        }
        private void ExitApp_Click(object sender, EventArgs e)
        {
            ToastNotificationManagerCompat.History.Clear();
            _randomScheduler?.Dispose();
            Environment.Exit(0);
        }

        private void RandomScheduleSettings_Click(object sender, EventArgs e)
        {
            try
            {
                var settingsWindow = new ToastFish.View.RandomScheduleSettings(_scheduleConfig, _randomScheduler);
                settingsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"打开随机抽背设置失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Start_Click(object sender, EventArgs e)
        {
            //StartWithWindows.SetMeStart(true);
            String startupPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "ToastFish.lnk");
            (sender as ToolStripMenuItem).Checked = !(sender as ToolStripMenuItem).Checked;
            StartWithWindows.CreateShortcut(startupPath);
        }

        /// <summary>
        /// 测试日语词库功能
        /// </summary>
        private void TestJpWordDatabase()
        {
            System.Diagnostics.Debug.WriteLine("=== 开始测试日语词库功能 ===");

            // 首先测试通知系统
            System.Diagnostics.Debug.WriteLine("0. 测试通知系统...");
            TestNotificationSystem();

            // 检查并修复日语表名
            System.Diagnostics.Debug.WriteLine("1. 检查日语表名...");
            string correctTableName = FindCorrectJpTableName();
            if (!string.IsNullOrEmpty(correctTableName) && correctTableName != Select.TABLE_NAME)
            {
                System.Diagnostics.Debug.WriteLine($"修正表名: {Select.TABLE_NAME} -> {correctTableName}");
                Select.TABLE_NAME = correctTableName;
            }

            // 测试数据库连接
            System.Diagnostics.Debug.WriteLine("2. 测试数据库连接...");
            Select query = new Select();
            System.Diagnostics.Debug.WriteLine("数据库连接成功");

            // 测试词库查询
            System.Diagnostics.Debug.WriteLine("3. 测试词库查询...");
            query.SelectJpWordList();
            System.Diagnostics.Debug.WriteLine("词库查询成功");

            // 测试获取随机单词
            System.Diagnostics.Debug.WriteLine("4. 测试获取随机单词...");
            var randomWords = query.GetRandomJpWordList(3);
            System.Diagnostics.Debug.WriteLine($"成功获取 {randomWords.Count} 个随机单词");

            // 显示单词信息
            for (int i = 0; i < Math.Min(2, randomWords.Count); i++)
            {
                var word = randomWords[i];
                System.Diagnostics.Debug.WriteLine($"单词 {i + 1}: {word.headWord} ({word.hiragana}) - {word.tranCN}");
            }

            // 测试语音功能
            System.Diagnostics.Debug.WriteLine("5. 测试语音功能...");
            PushJpWords pushJp = new PushJpWords();
            string voiceName = pushJp.GetJapaneseVoiceName();
            if (string.IsNullOrEmpty(voiceName))
            {
                System.Diagnostics.Debug.WriteLine("警告: 未找到日语语音包");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"找到日语语音: {voiceName}");
            }

            System.Diagnostics.Debug.WriteLine("=== 日语词库功能测试完成 ===");
        }

        /// <summary>
        /// 测试通知按钮点击事件
        /// </summary>
        private void TestNotification_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("用户点击了测试通知按钮");

                // 显示数据库表信息
                string tableInfo = GetDatabaseTableInfo();
                System.Windows.Forms.MessageBox.Show(
                    tableInfo,
                    "数据库表信息",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Information);

                // 测试日语发音功能
                TestJapaneseSpeechFunction();

                TestNotificationSystem();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"测试通知按钮处理失败: {ex.Message}");
                System.Windows.Forms.MessageBox.Show(
                    $"测试通知失败: {ex.Message}",
                    "错误",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 查找数据库中正确的日语表名
        /// </summary>
        private string FindCorrectJpTableName()
        {
            try
            {
                string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string exeDir = System.IO.Path.GetDirectoryName(strExeFilePath);
                string dbFilePath = System.IO.Path.Combine(exeDir, "Resources", "inami.db");
                string databasePath = @"Data Source=" + dbFilePath + ";Version=3";

                using (System.Data.SQLite.SQLiteConnection connection = new System.Data.SQLite.SQLiteConnection(databasePath))
                {
                    connection.Open();

                    // 查询所有表名
                    string sql = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;";
                    using (System.Data.SQLite.SQLiteCommand command = new System.Data.SQLite.SQLiteCommand(sql, connection))
                    {
                        using (System.Data.SQLite.SQLiteDataReader reader = command.ExecuteReader())
                        {
                            System.Diagnostics.Debug.WriteLine("数据库中的所有表:");
                            while (reader.Read())
                            {
                                string tableName = reader["name"].ToString();
                                System.Diagnostics.Debug.WriteLine($"- {tableName}");

                                // 检查是否是日语相关的表
                                if (tableName.ToLower().Contains("jp") ||
                                    tableName.ToLower().Contains("japan") ||
                                    tableName.ToLower().Contains("std") ||
                                    tableName.ToLower().Contains("mid"))
                                {
                                    System.Diagnostics.Debug.WriteLine($"  *** 找到可能的日语表: {tableName} ***");
                                    return tableName; // 返回第一个匹配的表名
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"查找日语表名失败: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 获取数据库表信息
        /// </summary>
        private string GetDatabaseTableInfo()
        {
            try
            {
                string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string exeDir = System.IO.Path.GetDirectoryName(strExeFilePath);
                string dbFilePath = System.IO.Path.Combine(exeDir, "Resources", "inami.db");
                string databasePath = @"Data Source=" + dbFilePath + ";Version=3";

                var tableInfo = new System.Text.StringBuilder();
                tableInfo.AppendLine("数据库中的所有表:");
                tableInfo.AppendLine("==================");

                using (System.Data.SQLite.SQLiteConnection connection = new System.Data.SQLite.SQLiteConnection(databasePath))
                {
                    connection.Open();

                    // 查询所有表名
                    string sql = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;";
                    using (System.Data.SQLite.SQLiteCommand command = new System.Data.SQLite.SQLiteCommand(sql, connection))
                    {
                        using (System.Data.SQLite.SQLiteDataReader reader = command.ExecuteReader())
                        {
                            int count = 0;
                            while (reader.Read())
                            {
                                string tableName = reader["name"].ToString();
                                count++;
                                tableInfo.AppendLine($"{count}. {tableName}");

                                // 检查是否是日语相关的表
                                if (tableName.ToLower().Contains("jp") ||
                                    tableName.ToLower().Contains("japan") ||
                                    tableName.ToLower().Contains("std") ||
                                    tableName.ToLower().Contains("mid"))
                                {
                                    tableInfo.AppendLine("   *** 可能的日语表 ***");

                                    // 显示表结构
                                    try
                                    {
                                        string structSql = $"PRAGMA table_info({tableName});";
                                        using (System.Data.SQLite.SQLiteCommand structCmd = new System.Data.SQLite.SQLiteCommand(structSql, connection))
                                        {
                                            using (System.Data.SQLite.SQLiteDataReader structReader = structCmd.ExecuteReader())
                                            {
                                                tableInfo.AppendLine("   字段:");
                                                while (structReader.Read())
                                                {
                                                    string colName = structReader["name"].ToString();
                                                    string colType = structReader["type"].ToString();
                                                    tableInfo.AppendLine($"     - {colName} ({colType})");
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        tableInfo.AppendLine($"   无法读取表结构: {ex.Message}");
                                    }
                                }
                            }

                            if (count == 0)
                            {
                                tableInfo.AppendLine("没有找到任何表！");
                            }
                        }
                    }
                }

                return tableInfo.ToString();
            }
            catch (Exception ex)
            {
                return $"获取数据库信息失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 测试日语发音功能
        /// </summary>
        private void TestJapaneseSpeechFunction()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("开始测试日语发音功能...");

                var pushJpWords = new ToastFish.Model.PushControl.PushJpWords();

                // 测试简单的日语发音
                string testText = "こんにちは";  // 你好
                System.Diagnostics.Debug.WriteLine($"测试发音文本: {testText}");

                pushJpWords.TestJapaneseSpeech(testText);

                System.Diagnostics.Debug.WriteLine("日语发音测试完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"日语发音测试失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 测试通知系统是否正常工作（已禁用测试通知）
        /// </summary>
        private void TestNotificationSystem()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("通知系统检查（不显示测试通知）");

                // 清除历史通知
                ToastNotificationManagerCompat.History.Clear();
                System.Diagnostics.Debug.WriteLine("清除历史通知完成");

                // 不再发送测试通知，只进行基本检查
                System.Diagnostics.Debug.WriteLine("通知系统基本检查完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"通知系统检查失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
        }
        #endregion
    }
}

