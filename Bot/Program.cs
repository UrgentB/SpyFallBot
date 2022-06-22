using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;

namespace TelegramBotExperiments
{
    class Player
    {
        public long PlayerID;
        public string PlayerName;
        public string RoomPassword;

    }
    class Program
    {
        //Коллода с карточками
        public static string[] CardDeck = new string[] {"Шпион",
                                                        "Полицейский участок",
                                                        "Кондитерская фабрика",
                                                        "Выставка кошек",
                                                        "Ферма осетра",
                                                        "Комик кон",
                                                        "Хогвардс",
                                                        "Съёмки кинофильма",
                                                        "Казино",
                                                        "Страусиная ферма",
                                                        "Аквапарк",
                                                        "Музей восковых фигур"};
        //Коллекция игроков
        public static List<Player> players = new List<Player>();
        static ITelegramBotClient bot = new TelegramBotClient("5573406426:AAFgXBCUfrm1BbMfAkNhTNwhaN2uUsFXdrc");
        public static Random rnd = new Random();

        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            
            // Некоторые действия
            if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
            {
                Message message = update.Message;
                Console.WriteLine(message.From.Id + " | "
                                  + message.From.FirstName + " " + message.From.LastName + " | "
                                  + message.Text);
                //Проверка есть ли в сообщении текст
                if (message.Text == null)
                {
                    var wariningaudio = System.IO.File.OpenRead("audio_2022-06-21_15-14-46.ogg");
                    await botClient.SendVoiceAsync(message.Chat, wariningaudio);
                    return;
                }
                //Приветсвие
                if (message.Text.ToLower() == "/start")
                {
                    await botClient.SendTextMessageAsync(message.Chat, "Добро пожаловать. Чтобы зарегестрироваться введите /reg");
                    return;
                }
                //Запуск игры: проверка иницирующего, составление группы, тасовка карточек.
                if (message.Text.ToLower() == "/game")
                {
                    
                    if (players.Find(x => x.PlayerID == message.From.Id) == null || players.Find(x => x.PlayerID == message.From.Id && x.PlayerName == null)!=null) 
                    {
                        await botClient.SendTextMessageAsync(message.Chat, "Чтобы начать игру необходимо зарегистрировваться. Для регистрации нажмите /reg");
                        return;
                    }

                    //Формирование списка игроков раунда
                    string passwordOfround = players[players.FindIndex(x => x.PlayerID == message.From.Id)].RoomPassword;
                    int n = 0;
                    Dictionary<int, long> playersOfround = new Dictionary<int, long>();

                    Console.WriteLine(message.From.Id + " | "
                                      + message.From.FirstName + " " + message.From.LastName 
                                      + " иницировал игру" + "\nИгроки:");
                    foreach (var x in players)
                    {
                        if(x.RoomPassword == passwordOfround)
                        {
                            playersOfround.Add(n, x.PlayerID);
                            Console.WriteLine("\t"+x.PlayerID + " | " + x.PlayerName + " | " + x.RoomPassword);
                        }
                        n++;
                    }

                    //Определение локации и шпиона
                    int location_id = rnd.Next(1, CardDeck.Length-1);
                    int Spy_id = rnd.Next(playersOfround.Count-1);
                    Console.WriteLine("\n\tШпион: " + playersOfround[Spy_id] + " | " 
                                      + players[players.FindIndex(x=>x.PlayerID == playersOfround[Spy_id])].PlayerName
                                      + "\n\tЛокация: " + CardDeck[location_id] + "\n");

                    //Рассылка игрокам их ролей
                    foreach (var x in playersOfround)
                    {
                        if (x.Key != Spy_id)
                        {
                            await botClient.SendTextMessageAsync(x.Value, "Ваше местоположение:\n"+CardDeck[location_id]);
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(x.Value, "Вы " + CardDeck[0]);
                        }
                    }
                    
                    return;
                }
                    
                //Регистрация игрока
                //Определение id и его наличие в списке
                if (message.Text.ToLower() == "/reg")
                {
                    await botClient.SendTextMessageAsync(message.Chat, "Введите имя");
                    if (players.Find(x => x.PlayerID == message.From.Id) == null)
                         { players.Add(new Player() { PlayerID = message.From.Id });}
                    else 
                         {players[players.FindIndex(x => x.PlayerID == message.From.Id)].PlayerName = null;
                          players[players.FindIndex(x => x.PlayerID == message.From.Id)].RoomPassword = null;}
                    return;
                }
                //Определение имени игрока
                if(players.Find(x => x.PlayerID == message.From.Id && x.PlayerName==null && x.RoomPassword==null) != null)
                {
                    players[players.FindIndex(x => x.PlayerID == message.From.Id)].PlayerName = message.Text;
                    await botClient.SendTextMessageAsync(message.Chat, 
                        "Введите код-пароль (Он необходим для определения игроков, с которыми вы будете играть)");
                    return;
                }
                //Определение его код-пароля
                if (players.Find(x => x.PlayerID == message.From.Id && x.PlayerName != null && x.RoomPassword == null) != null)
                {
                    players[players.FindIndex(x => x.PlayerID == message.From.Id)].RoomPassword = message.Text;
                    await botClient.SendTextMessageAsync(message.Chat, "Спасибо за регистриацию, чтобы начать игру нажмите /game");
                    Console.WriteLine($"\nNew player:\n\t{players.FindIndex(x => x.PlayerID == message.From.Id)}" +
                                     $"\n\tID: {players[players.FindIndex(x => x.PlayerID == message.From.Id)].PlayerID}" +
                                     $"\n\tName: {players[players.FindIndex(x => x.PlayerID == message.From.Id)].PlayerName}" +
                                     $"\n\tPassword: { players[players.FindIndex(x => x.PlayerID == message.From.Id)].RoomPassword}");
                    Console.WriteLine("All players:");
                    foreach (Player x in players)
                    {
                        Console.WriteLine($"\t{x.PlayerID}");
                        Console.WriteLine($"\t{x.PlayerName}");
                        Console.WriteLine($"\t{x.RoomPassword}\n");
                    }
                    return;
                   
                }

                    return;
            }
        }


        public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            // Некоторые действия
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
        }


        static void Main(string[] args)
        {
            
            Console.WriteLine("Запущен бот " + bot.GetMeAsync().Result.FirstName+"\n");

            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }, // receive all update types
            };
            bot.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken
            );
            Console.ReadLine();
        }
    }
}