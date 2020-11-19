using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using FoxTeleBo.Properties;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Reflection;

namespace FoxTeleBo
    {

    /// <summary>
    /// Use this class for interface user messages and its direction
    /// </summary>
    public class UserMes
        {
        public string MesText { get; set; }
        public byte MesDirect { get; set; }

        public string MesDateTime
            {
            get
                {
                //время в человеческом формате
                return string.Format("{0:t}", DateTime.Now);
                //return DateTime.Now.ToString("hh:mm"); 
                }
            }

        public override string ToString()
            {
            return MesText;
            }
        }

    /// <summary>
    /// Use this class for realization all users and messages
    /// </summary>
    public class TelegramUser : INotifyPropertyChanged, IEquatable<TelegramUser>
        {

        public TelegramUser(string Nickname, long ChatId)
            {
            this.nick = Nickname;
            this.id = ChatId;
            Messages = new ObservableCollection<string>();
            UserMessages = new ObservableCollection<UserMes>();

            }
        private string nick;
        public string Nick
            {
            get { return this.nick; }
            set
                {
                this.nick = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Nick)));
                }
            }
        private long id;
        public long Id
            {
            get { return this.id; }
            set
                {
                this.id = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Id)));
                }
            }

        public event PropertyChangedEventHandler PropertyChanged;
        public bool Equals(TelegramUser other) => other.Id == this.id;
        public ObservableCollection<string> Messages { get; set; }
        public void AddMessage(string Text)
            {
            if (Messages != null) Messages.Add(Text);
            }
        public ObservableCollection<UserMes> UserMessages { get; set; }
        public void AddUserMes(string Text, byte Direct)
            {
            UserMessages.Add(new UserMes { MesText = Text, MesDirect = Direct });
            }

        }


    public partial class MainWindow : Window
        {
        private static readonly string RegistryKey = Settings.Default.RegistryKey;
        private static readonly TelegramBotClient bot = new TelegramBotClient(RegistryKey/*, WebProxy*/);
        private static readonly string[] m_hello = { "hello", "hi", "привет", "приветствую", "приветик", "здраствуй", "здравствуй", "здравствуйте", "здрасте", "здрасти", "здорово", "доброе утро",
            "доброго утречка", "доброго времени суток", "добрый день", "салют", "здрав буди, боярин", "добро пожаловать", "приятно видеть тебя снова", "однако, здравствуйте"};
        private static readonly string[] m_prosto = { "Рад тебя видеть", "Глазам не верю: ты - прекрасный сон", "Дружище, рад тебе больше, чем солнцу в небе", "Мои тебе поцелуи", "Тысяча реверансов",
            "Я пришёл с миром", "Благословен будь твой день", "Ты архитектор своей жизни", "Я верю в то, что у тебя должно все наладиться!", "Солнце взойдет и завтра, несмотря ни на что",
            "Дорогу осилит идущий", "Я рад это слышать от вас", "Я очень хочу получить ваш совет по этому поводу", "Спасибо за то, что вы находитесь на связи", "Я на днях вспоминал"};
        private static readonly Random rnd = new Random();
        public ObservableCollection<TelegramUser> Users;
        public static Message pu_msg;
        public static int need_delete_msg_id;
        public bool MenuIsOpen = false;
        public Dictionary<long, byte> StepOfChat = new Dictionary<long, byte>();
        public static int DelayAnswer = 1000;
        public static DateTime prev_db_file_time;
        public static string SignalFile = "";
        public static System.Windows.Threading.DispatcherTimer timer;
        public static string UnlockingZakNumber;
        public static int SignalFreq = 15;
        public static long FoxGroupIdChat;
        public static long CreatorIdChat;
        public static WindowState PrevState;

        public MainWindow()
            {

            InitializeComponent();
            //таймер слежения за файлом
            timer = new System.Windows.Threading.DispatcherTimer();
            timer.Tick += new EventHandler(TimerTick);
            timer.Interval = new TimeSpan(0, 0, SignalFreq);
            timer.Start();

            //класс пользователей, которые с нами общаются
            Users = new ObservableCollection<TelegramUser>();
            usersList.ItemsSource = Users;
            bot.OnMessage += delegate (object sender, Telegram.Bot.Args.MessageEventArgs e)
                {
                    string msg = $"{DateTime.Now}: {e.Message.Chat.FirstName} {e.Message.Chat.Id} {e.Message.Text}";
                    //File.AppendAllText("data.log", $"{msg}\n");
                    //Debug.WriteLine();
                    this.Dispatcher.Invoke(() =>
                        {
                            //добавление юзера в список, если его там нет
                            string person_name = e.Message.Chat.FirstName;
                            if ((person_name == null) && (e.Message.Chat.Id == FoxGroupIdChat)) person_name = "FoxGroup";
                            if (person_name == null) person_name = "Группа №" + Convert.ToString(e.Message.Chat.Id);
                            var person = new TelegramUser(person_name, e.Message.Chat.Id);
                            if (!Users.Contains(person)) Users.Add(person);
                            //добавление сообщения от пользователя
                            //Users[Users.IndexOf(person)].AddMessage($"<{person.Nick}: {e.Message.Text}");
                            Users[Users.IndexOf(person)].AddUserMes(e.Message.Text, 1);

                        });
                    };
            bot.StartReceiving();
            //создание автоответа
            bot.OnMessage += Bot_OnMessage;
            //ответ из формы от оператора; либо нажимаем кнопку с самолётиком, либо нажмаем Enter 
            Button_Send.Click += delegate { SendFromOperator(); };
            TextBox_Send.KeyDown += (s, e) => { if (e.Key == Key.Enter) { SendFromOperator(); } };
            //обратотка кнопок
            bot.OnCallbackQuery += BotOnCallbackQueryReceived;
            }

        #region BotMessageCreate

        private async void Bot_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
            {
            Message msg = e.Message;
            if (msg == null) return;
            //если будем слать из другого метода, то запомним msg
            pu_msg = msg;
            string m = msg.Text;
            // так можно удалить сообщение:
            //await bot.DeleteMessageAsync(msg.Chat.Id, need_delete_msg_id);
            if (m == null) return;
            //задержка бота на размышление
            Thread.Sleep(DelayAnswer);
            //StepOfChat[msg.Chat.Id] = 0;
            //если команды
            if ((m != "") && (msg.Text.ToLower() == "/...") && ((msg.Chat.Id == FoxGroupIdChat) || (msg.Chat.Id == CreatorIdChat)))
                {
                var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(new[]
                {
                            new [] // одна строка
                            {
                                InlineKeyboardButton.WithCallbackData("Приветствие", "/hello"),
                                InlineKeyboardButton.WithCallbackData("Фразы", "/frases")

                            },
                            new [] // вторая строка
                            {
                                InlineKeyboardButton.WithCallbackData("Обшее", "/common"),
                                InlineKeyboardButton.WithCallbackData("Клавиатуры","/keyboard")
                            }
                        });
                await bot.SendTextMessageAsync(msg.Chat.Id, "Что я умею:", replyMarkup: keyboard);
                need_delete_msg_id = e.Message.MessageId;
                return;
                }

            //раздел "..."
            if (m.ToLower() == "...")
                {
                var replyKeyboardMarkup = new ReplyKeyboardMarkup(
                                    new KeyboardButton[][]
                                    {
                                        new KeyboardButton[] { "...", "..." },
                                        new KeyboardButton[] { "..." },
                                    },
                                    oneTimeKeyboard: true, resizeKeyboard: true
                                );

                await bot.SendTextMessageAsync(chatId: msg.Chat.Id, text: "...", replyMarkup: replyKeyboardMarkup);
                //Users[Users.IndexOf(usersList.SelectedItem as TelegramUser)].AddUserMes(TextBox_Send.Text, 2);
                //шаг в многоступенчатом диалоге
                SetStep(msg.Chat.Id, 1);
                return;
                }
            else if ((m == "...") && (GetStep(msg.Chat.Id) == 1))
                {
                var replyKeyboardMarkup = new ReplyKeyboardMarkup(
                                    new KeyboardButton[][]
                                    {
                                        new KeyboardButton[] { "...", "..." },
                                        new KeyboardButton[] { "...", "...", "..." },
                                        new KeyboardButton[] { "..." },
                                    },
                                    oneTimeKeyboard: true, resizeKeyboard: true
                                );

                await bot.SendTextMessageAsync(chatId: msg.Chat.Id, text: "...", replyMarkup: replyKeyboardMarkup);
                SetStep(msg.Chat.Id, 2);
                return;
                }
            else if (((m == "...") || (m == "...")) && (GetStep(msg.Chat.Id) == 2))
                {
                byte Resultat = 0;
                //выполнение
                if (m.Substring(0, 1) == "З") Class_DB_Operations.EditAccess(0, ref Resultat); else Class_DB_Operations.EditAccess(1, ref Resultat);
                //обработка результата
                if (Resultat == 1)
                    await bot.SendTextMessageAsync(pu_msg.Chat.Id, "Выполнено!" + char.ConvertFromUtf32(0x1F64B), replyMarkup: new ReplyKeyboardRemove());
                else if (Resultat == 0)
                    await bot.SendTextMessageAsync(pu_msg.Chat.Id, "Не удалось выполнить изменения!" + char.ConvertFromUtf32(0x1F602), replyMarkup: new ReplyKeyboardRemove());
                else
                    await bot.SendTextMessageAsync(pu_msg.Chat.Id, "Ошибка при обращении к процедуре! " + char.ConvertFromUtf32(0x1F602), replyMarkup: new ReplyKeyboardRemove());
                SetStep(msg.Chat.Id, 0);
                return;
                }
            else if ( (((m == "...")) && (GetStep(msg.Chat.Id) == 2)) || (((m == "...")) && (GetStep(msg.Chat.Id) == 1)) )  
                {
                string s = Class_DB_Operations.ShowAccess();
                if (s != "") await bot.SendTextMessageAsync(pu_msg.Chat.Id, s);
                return;
                }
            else if (((m == "...")) && (GetStep(msg.Chat.Id) == 2))
                {
                var replyKeyboardMarkup = new ReplyKeyboardMarkup(
                                                    new KeyboardButton[][]
                                                    {
                                                    new KeyboardButton[] { "...", "...", "..." },
                                                    new KeyboardButton[] { "...", "..." },
                                                    new KeyboardButton[] { "...", "..." },
                                                    new KeyboardButton[] { "..." },
                                                    },
                                                    oneTimeKeyboard: true, resizeKeyboard: true
                                                );
                await bot.SendTextMessageAsync(chatId: msg.Chat.Id, text: "...", replyMarkup: replyKeyboardMarkup);
                SetStep(msg.Chat.Id, 7);
                return;
                }
            else if (((m == "По годам")) && (GetStep(msg.Chat.Id) == 2))
                {
                var replyKeyboardMarkup = new ReplyKeyboardMarkup(
                                                    new KeyboardButton[][]
                                                    {
                                                    new KeyboardButton[] { "..." },
                                                    new KeyboardButton[] { "...", "..." },
                                                    new KeyboardButton[] { "Отмена" },
                                                    },
                                                    oneTimeKeyboard: true, resizeKeyboard: true
                                                );

                await bot.SendTextMessageAsync(chatId: msg.Chat.Id, text: "...", replyMarkup: replyKeyboardMarkup);
                SetStep(msg.Chat.Id, 18);
                return;
                }
            else if ((StepOfChat.ContainsKey(msg.Chat.Id)) && (GetStep(msg.Chat.Id) == 7))  // инвертировние 
                {
                byte Resultat = 0;
                //выполнение
                Class_DB_Operations.EditAccess(m, ref Resultat);
                //обработка результата
                if (Resultat == 1)
                    await bot.SendTextMessageAsync(pu_msg.Chat.Id, "Выполнено!" + char.ConvertFromUtf32(0x1F64B));
                else if (Resultat == 0)
                    await bot.SendTextMessageAsync(pu_msg.Chat.Id, "Не удалось выполнить изменения!" + char.ConvertFromUtf32(0x1F602), replyMarkup: new ReplyKeyboardRemove());
                else
                    await bot.SendTextMessageAsync(pu_msg.Chat.Id, "Ошибка при обращении к процедуре! " + char.ConvertFromUtf32(0x1F602), replyMarkup: new ReplyKeyboardRemove());
                SetStep(msg.Chat.Id, 0);
                return;
                }
            else if ((StepOfChat.ContainsKey(msg.Chat.Id)) && (GetStep(msg.Chat.Id) == 18))  
                {
                byte Resultat = 0;
                //выполнение
                Class_DB_Operations.EditYearAccess(m, ref Resultat);
                //обработка результата
                if (Resultat == 1)
                    await bot.SendTextMessageAsync(pu_msg.Chat.Id, "Выполнено!" + char.ConvertFromUtf32(0x1F64B), replyMarkup: new ReplyKeyboardRemove());
                else if (Resultat == 0)
                    await bot.SendTextMessageAsync(pu_msg.Chat.Id, "Не удалось выполнить изменения!" + char.ConvertFromUtf32(0x1F602), replyMarkup: new ReplyKeyboardRemove());
                else
                    await bot.SendTextMessageAsync(pu_msg.Chat.Id, "Ошибка при обращении к процедуре! " + char.ConvertFromUtf32(0x1F602), replyMarkup: new ReplyKeyboardRemove());
                SetStep(msg.Chat.Id, 0);
                return;
                }
            else if (msg.Text == "Отмена")
                {
                //возврат к исходному состоянию и возвращение клавиатуры
                await bot.SendTextMessageAsync(pu_msg.Chat.Id, char.ConvertFromUtf32(0x1F609) + "  *Не беспокойтесь:* \r\n  действие отменено! ",
                    replyMarkup: new ReplyKeyboardRemove(),
                    parseMode: ParseMode.Markdown);
                SetStep(msg.Chat.Id, 0);
                return;
                }
            else if (msg.Text == "Закрыть")
                {
                //возврат к исходному состоянию и возвращение клавиатуры
                await bot.SendTextMessageAsync(pu_msg.Chat.Id, "Настройки закрыты! " + char.ConvertFromUtf32(0x1F645), replyMarkup: new ReplyKeyboardRemove());
                SetStep(msg.Chat.Id, 0);
                return;
                }
            //раздел "..."
            else if ((m.Length > 10) && (m.ToLower().Substring(0, 10) == "..."))
                {
                var replyKeyboardMarkup = new ReplyKeyboardMarkup(
                                    new KeyboardButton[][]
                                    {
                                        new KeyboardButton[] { "...", "..." },
                                        new KeyboardButton[] { "Отмена" },
                                    },
                                    oneTimeKeyboard: true, resizeKeyboard: true
                                );
                await bot.SendTextMessageAsync(chatId: msg.Chat.Id, text: "...", replyMarkup: replyKeyboardMarkup);
                //запомним номер заказа, чтобы потом знать с каким заказом дело имеем
                String[] words = m.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                UnlockingZakNumber = words[1];
                //шаг в многоступенчатом диалоге
                SetStep(msg.Chat.Id, 200);
                return;
                }
            else if ((StepOfChat.ContainsKey(msg.Chat.Id)) && (GetStep(msg.Chat.Id) == 200) && (m == "..."))  
                {
                //выполнение
                string Resultat = Class_DB_Operations.UnlockZak(UnlockingZakNumber, 0);
                //обработка результата
                if (Resultat != "")
                    await bot.SendTextMessageAsync(pu_msg.Chat.Id, Resultat, parseMode: ParseMode.Markdown);
                else
                    await bot.SendTextMessageAsync(pu_msg.Chat.Id, char.ConvertFromUtf32(0x2757) + " Нет подходящих заказов!", replyMarkup: new ReplyKeyboardRemove());
                return;
                }
            else if ((StepOfChat.ContainsKey(msg.Chat.Id)) && (GetStep(msg.Chat.Id) == 200) && (m == "..."))  
                {
                //выполнение
                string ResultatDel = Class_DB_Operations.UnlockZak(UnlockingZakNumber, 1);
                //обработка результата
                if (ResultatDel != "")
                    await bot.SendTextMessageAsync(pu_msg.Chat.Id, ResultatDel, replyMarkup: new ReplyKeyboardRemove());
                return;
                }
            else if (m.ToLower() == "...")  
                {
                await bot.SendTextMessageAsync(pu_msg.Chat.Id, Class_DB_Operations.ServerName);
                return;
                }
            else if (m.ToLower() == "...")  
                {
                await bot.SendTextMessageAsync(pu_msg.Chat.Id, Convert.ToString(pu_msg.Chat.Id));
                return;
                }
            else if (m.ToLower() == "/start")  // старт бота пользователем
                {
                await bot.SendTextMessageAsync(pu_msg.Chat.Id, "Здравствуйте, я FoxBot!");
                return;
                }
            // если сообщение текстовое и не распознанные команды
            if (msg.Type == MessageType.Text)
                {
                string s = DetHelloMsg(msg.Text);
                if (s == "") s = SetAnyMsg(); else s += msg.From.FirstName;
                await bot.SendTextMessageAsync(msg.Chat.Id, s);
                }
            // если сообщение - это фото
            else if (msg.Type == MessageType.Photo)
                {
                await bot.SendTextMessageAsync(msg.Chat.Id, "Классное фото, чем маешься?");
                }
            else
                {
                //MessageBox.Show("ответ");
                }

            }

        private static string DetHelloMsg(string s)
            {
            string r = "";
            //Random rnd = new Random();
            foreach (string tmp_str in m_hello)
                {
                if ((s.ToLower()).Contains(tmp_str.ToLower()))
                    {
                    int value = rnd.Next(0, m_hello.Length - 1);
                    r = m_hello[value];
                    break;
                    }
                }
            if (r != "") r += ", ";
            return r;
            }

        private static string SetAnyMsg()
            {
            string r;
            int value = rnd.Next(0, m_prosto.Length - 1);
            r = m_prosto[value];
            return r;
            }

        //ответ на нажатие контекстных кнопок
        private static async void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
            {
            string buttonText = callbackQueryEventArgs.CallbackQuery.Data;
            string name = $"{callbackQueryEventArgs.CallbackQuery.From.FirstName} {callbackQueryEventArgs.CallbackQuery.From.LastName}";
            string feedback;
            //реализация команды пользователя
            //MessageBox.Show( $"{name} pressed {buttonText}");
            switch (buttonText)
                {
                case "/hello":
                    feedback = "_Поздоровайтесь со мной_ \n Я умею отвечать на приветствие. Найти меня можно по полному имени @FoxTeleBot.";
                    break;
                case "/frases":
                    feedback = "_Скажите мне что-нибудь_ \n Я могу поддерживать разговор, но иногда повторяюсь. Не судите строго: я всего-лишь маленький бот.";
                    break;
                case "/common":
                    feedback = "...";
                    break;
                case "/keyboard":
                    feedback = "_Команды могут вызывать и менять клавиатуры_ \n Некоторые действия пошаговые. Если клавиатура скрылась, попробуйте открыть её соответствующей кнопкой показа.";
                    break;
                default:
                    feedback = "_Не опознанная команда:_ " + buttonText;
                    break;
                }

            Thread.Sleep(DelayAnswer);
            long current_chat_id;
            if (pu_msg != null) 
                current_chat_id = pu_msg.Chat.Id;
            else
                { 
                current_chat_id = CreatorIdChat;
                feedback = name + " попытался воспользоаться командой *...*, но не вышло!";
                }
                
            //шлём ответ
            await bot.SendTextMessageAsync(current_chat_id, feedback, parseMode: ParseMode.Markdown);
            //await bot.AnswerCallbackQueryAsync(
            //    callbackQueryEventArgs.CallbackQuery.Id,
            //    $"Received {callbackQueryEventArgs.CallbackQuery.Data}");
            }

        //отправка со стороны оператора
        private async void SendFromOperator()
            {
            //проверка на пустое сообщение
            if (TextBox_Send.Text == String.Empty)
                {
                MessageBoxResult result = MessageBox.Show("Посылаете пустое сообщение?", "ВНИМАНИЕ:", MessageBoxButton.YesNo);
                switch (result)
                    {
                    case MessageBoxResult.Yes:
                        MessageBox.Show("Отказ отправки пустого сообщения!", "ОТМЕНЕНО:");
                        break;
                    case MessageBoxResult.No:
                        break;
                    }
                return;
                };
            //проверка на выбранного пользователя
            if (usersList.SelectedItems.Count == 0)
                {
                MessageBox.Show("Не выбран получатель сообщения!", "ОТМЕНЕНО:");
                return;
                }

            var Recepient = Users[Users.IndexOf(usersList.SelectedItem as TelegramUser)];
            string Text_Content = TextBox_Send.Text;
            await bot.SendTextMessageAsync(Recepient.Id, Text_Content);
            //добавление сообщения от пользователя
            //Users[Users.IndexOf(usersList.SelectedItem as TelegramUser)].AddMessage(">"+TextBox_Send.Text);
            Users[Users.IndexOf(usersList.SelectedItem as TelegramUser)].AddUserMes(TextBox_Send.Text, 2);
            TextBox_Send.Clear();
            }

        private async void SetStep(long ChatId, byte Step)
            {
            try
                {
                StepOfChat[ChatId] = Step;
                }
            catch
                {
                StepOfChat.Clear();
                await bot.SendTextMessageAsync(pu_msg.Chat.Id, "Что-то я запутался, попробуйте выполнить последовательность действий заново!", replyMarkup: new ReplyKeyboardRemove());
                }
            }

        private byte GetStep(long ChatId)
            {
            try
                {
                if (StepOfChat.ContainsKey(ChatId)) return StepOfChat[ChatId]; else return 0;
                }
            catch
                {
                return 0;
                }

            }

        private async void TimerTick(object sender, EventArgs e)
            {
            if (System.IO.File.Exists(SignalFile))
                {
                System.IO.FileInfo file = new System.IO.FileInfo(SignalFile);
                DateTime mes_db_dile_time = file.LastWriteTime;
                if ((prev_db_file_time != mes_db_dile_time) && (prev_db_file_time.Year != 0))
                    {
                    //поскольку операция может быть длительной, меняем курсор мыши
                    Mouse.OverrideCursor = Cursors.Wait;
                    //составляем сообщение
                    string mes_caption = "<b>Сообщение от " + Class_DB_Operations.ServerName + ":</b>" + "\n";
                    string mes_text = Class_DB_Operations.GetMessages(Class_DB_Operations.LastMesIDFromDB);
                    string mes_caption_and_text = mes_caption + mes_text;
                    //отправим в группу, а если не получится Админу
                    if ((mes_text != null) && (mes_text != ""))
                        {
                        try
                            {
                            await bot.SendTextMessageAsync(FoxGroupIdChat, mes_caption_and_text, parseMode: ParseMode.Html);
                            }
                        catch
                            {
                            await bot.SendTextMessageAsync(CreatorIdChat, mes_caption_and_text, parseMode: ParseMode.Html);
                            }
                        //запоминаем, что мы отправили все сообщения до ID=LastMesIDFromDB
                        Settings.Default.LastMesIDFromDB = Class_DB_Operations.LastMesIDFromDB;
                        }
                    // курсор мышки возвращаем в исходное
                    Mouse.OverrideCursor = null;
                    }
                prev_db_file_time = mes_db_dile_time;
                }
            else
                {
                timer.Stop();
                }
            }

        #endregion BotMessageCreate

        private void Window_Initialized(object sender, EventArgs e)
            {
            //размер окна, только если они устанавливались для окна вручную
            try
                {
                Rect bounds = Settings.Default.WindowPosition;
                Top = bounds.Top;
                Left = bounds.Left;
                // восстановить размеры
                if (SizeToContent == SizeToContent.Manual)
                    {
                    Width = bounds.Width;
                    Height = bounds.Height;
                    }
                //id чатов               
                FoxGroupIdChat = Settings.Default.FoxGroupIdChat;
                CreatorIdChat = Settings.Default.CreatorIdChat;
                //с каким сервером работаем
                Class_DB_Operations.ServerName = Settings.Default.ServerName;
                Class_DB_Operations.DatabaseName = Settings.Default.DatabaseName;
                DelayAnswer = Settings.Default.DelayAnswer;
                //файл за датой которого следим и как часто
                SignalFile = Settings.Default.SignalFile;
                SignalFreq = Settings.Default.SignalFreq;
                }
            catch
                {
                }
            }
        private void Window_Closing(object sender, CancelEventArgs e)
            {
            //останавливаем приёмо-передачу
            bot.StopReceiving();

            //сохраняем размер окна в параметре
            Settings.Default.WindowPosition = RestoreBounds;
            //тоже с другими настройками
            Settings.Default.ServerName = Class_DB_Operations.ServerName;
            Settings.Default.DelayAnswer = DelayAnswer;
            Settings.Default.DatabaseName = Class_DB_Operations.DatabaseName;
            Settings.Default.SignalFile = SignalFile;
            Settings.Default.SignalFreq = SignalFreq;
            Settings.Default.Save();
            }

        private void Window_StateChanged(object sender, EventArgs e)
            {
            if (WindowState == WindowState.Minimized)
                {
                TrayIcon.Visibility = Visibility.Visible;
                Hide();
                }

            else
                { 
                PrevState = WindowState;
                TrayIcon.Visibility = Visibility.Hidden;
                }
                
            }

        private void TaskbarIcon_TrayLeftMouseDown(object sender, RoutedEventArgs e)
            {
            Show();
            WindowState = PrevState;
            }

        #region MenuOptions

        private void TextBox_ServerName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
            {
            Class_DB_Operations.ServerName = TextBox_ServerName.Text;
            }

        private void TextBox_DatabaseName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
            {
            Class_DB_Operations.DatabaseName = TextBox_DatabaseName.Text;
            }

        private void TextBox_DelayAnswer_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
            {
            DelayAnswer = Convert.ToInt32(TextBox_DelayAnswer.Text);
            }

        private void TextBox_SignalFile_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
            {
            timer.Stop();
            SignalFile = TextBox_SignalFile.Text;
            timer.Start();
            }

        private void TextBox_SignalFreq_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
            {
            timer.Stop();
            Int32.TryParse(TextBox_SignalFreq.Text, out int NewFreq);
            if ((NewFreq > 600) || (NewFreq < 1))
                {
                TextBox_SignalFreq.Text = "59";
                MessageBox.Show("Интервал должен быть указан в пределах (0-10) минут!", "ОШИБКА:", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
                }

            SignalFreq = Convert.ToInt32(TextBox_SignalFreq.Text);
            //задержка 
            Thread.Sleep(100);
            var ts = TimeSpan.FromSeconds(SignalFreq);
            timer.Interval = ts;
            timer.Start();
            }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
            {
            MenuIsOpen = !MenuIsOpen;
            if (MenuIsOpen)
                {
                TextBox_ServerName.Text = Class_DB_Operations.ServerName;
                TextBox_DatabaseName.Text = Class_DB_Operations.DatabaseName;
                TextBox_DelayAnswer.Text = Convert.ToString(DelayAnswer);
                if (TextBox_SignalFile.Text != SignalFile) TextBox_SignalFile.Text = SignalFile;
                if (TextBox_SignalFreq.Text != Convert.ToString(SignalFreq)) TextBox_SignalFreq.Text = Convert.ToString(SignalFreq);
                var obj = Assembly.GetExecutingAssembly().GetName().Version;
                Label_Ver.Content = "Версия приложения: " + string.Format("{0}.{1}", obj.Major, obj.Minor);
                MenuDockPanel.Visibility = Visibility.Visible;
                }
            }

        private void ToggleButton_MouseLeave(object sender, MouseEventArgs e)
            {
            if (!MenuIsOpen) MenuDockPanel.Visibility = Visibility.Collapsed;
            }

        #endregion MenuOptions


        }
    }
