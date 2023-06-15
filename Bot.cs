using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Bot_Telegram
{
    internal class Bot : BackgroundService
    {
        private ITelegramBotClient _telegramClient;
        private bool _isCalculatingSum;

        public Bot(ITelegramBotClient telegramClient)
        {
            _telegramClient = telegramClient;
            _isCalculatingSum = false;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _telegramClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                new ReceiverOptions() { AllowedUpdates = { } }, // Здесь выбираем, какие обновления хотим получать
                cancellationToken: stoppingToken);

            Console.WriteLine("Бот запущен");
        }

        async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            // Обрабатываем входящие сообщения из Telegram Bot API
            if (update.Type == UpdateType.Message)
            {
                var messageText = update.Message.Text;
                var chatId = update.Message.Chat.Id;

                if (messageText == "/start")
                {
                    // Отправляем главное меню с кнопками
                    var replyMarkup = new ReplyKeyboardMarkup(new[]
                    {
                        new KeyboardButton("Подсчет символов"),
                        new KeyboardButton("Вычисление суммы чисел")
                    });
                    await _telegramClient.SendTextMessageAsync(chatId, "Выберите действие:", replyMarkup: replyMarkup, cancellationToken: cancellationToken);
                }
                else if (messageText == "Подсчет символов")
                {
                    // Отправляем запрос на ввод текста
                    await _telegramClient.SendTextMessageAsync(chatId, "Введите текст:", cancellationToken: cancellationToken);
                }
                else if (messageText == "Вычисление суммы чисел")
                {
                    // Отправляем запрос на ввод чисел
                    await _telegramClient.SendTextMessageAsync(chatId, "Введите числа через пробел:", cancellationToken: cancellationToken);
                    _isCalculatingSum = true;
                }
                else if (_isCalculatingSum)
                {
                    // Вычисление суммы чисел
                    var numbers = messageText.Split(' ');
                    int sum = 0;
                    foreach (var number in numbers)
                    {
                        if (int.TryParse(number, out int num))
                        {
                            sum += num;
                        }
                    }
                    await _telegramClient.SendTextMessageAsync(chatId, $"Сумма чисел: {sum}", cancellationToken: cancellationToken);
                    _isCalculatingSum = false;
                }
                else
                {
                    // Подсчет количества символов
                    var length = messageText.Length;
                    await _telegramClient.SendTextMessageAsync(chatId, $"В вашем сообщении {length} символов", cancellationToken: cancellationToken);
                }
            }
        }

        Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            // Задаем сообщение об ошибке в зависимости от того, какая именно ошибка произошла
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            // Выводим в консоль информацию об ошибке
            Console.WriteLine(errorMessage);

            // Задержка перед повторным подключением
            Console.WriteLine("Ожидаем 10 секунд перед повторным подключением.");
            Thread.Sleep(10000);

            return Task.CompletedTask;
        }
    }
}