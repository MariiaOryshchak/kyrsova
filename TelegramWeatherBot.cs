
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Polling;
using Npgsql;
using TelegramBotWeather.Models;
using static TelegramBotWeather.WeatherClient;
using static TelegramBotWeather.WeatherClient.DataBase;
using System.Net.Http;
using System.Net.Http.Json;
using System.Xml.Linq;
using System;
using System.Diagnostics.Metrics;
using static System.Net.Mime.MediaTypeNames;
using System.Threading;
using System.Collections.Generic;
using static TelegramBotWeather.Models.CityWeather;
using static TelegramBotWeather.Models.CityMain;
using System.Collections;
using System.Data;

namespace TelegramBotWeather
{
    public class TelegramWeatherBot
    {

        static TelegramBotClient botClient = new TelegramBotClient("5957782232:AAGGAg0s1RejPgJZrtZU4LzRHi8oyW-pL9o");

        CancellationToken cancellationToken = new CancellationToken();
        ReceiverOptions receiverOptions = new ReceiverOptions { AllowedUpdates = { } };

        public async Task Start()
        {
            botClient.StartReceiving(HandlerUpdateAsync, HandlerError, receiverOptions, cancellationToken);
            var botMe = await botClient.GetMeAsync();
            Console.WriteLine($"Бот {botMe.Username} почав працювати");

        }

        public Task HandlerError(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"В API телеграм-бота сталася помилка:\n {apiRequestException.ErrorCode}" +
                $"\n{apiRequestException.Message}",
                _ => exception.ToString()
            };
            return Task.CompletedTask;
        }

        public async Task HandlerUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update?.Message?.Text != null)
            {
                await HandlerMessageAsync(botClient, update);
            }

        }


        private Dictionary<long, string> currentStage = new Dictionary<long, string>();

        public async Task HandlerMessageAsync(ITelegramBotClient botClient, Update update)
        {
            var message = update.Message;
            if (!currentStage.ContainsKey(message.Chat.Id))
            {
                currentStage.Add(message.Chat.Id, "home");
            }

            switch (currentStage[message.Chat.Id]) //чекає відповідь юзера
            {
                case "/addCity":
                    await GetWaetherByAddCity(message.Text);
                    break;
                case "/deleteCity":
                    await DeleteWeatherByUser(message.Text);
                    break;
                default:
                    break;
            }

            switch (message.Text) //змінює вміст змінної (для всіх)
            {
                case "/addCity":
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Введіть назву міста\n ");
                    currentStage[message.Chat.Id] = "/addCity";
                    break;
                case "/myCities":
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Міста в моєму списку\n ");
                    currentStage[message.Chat.Id] = "/myCities";
                    break;
                case "/deleteCity":
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Введіть назву міста, яке ви хочете видалити зі списку\n ");
                    currentStage[message.Chat.Id] = "/deleteCity";
                    break;
                case "/start":
                    currentStage[message.Chat.Id] = "/start";
                    break;
                case "/keyboard":
                    currentStage[message.Chat.Id] = "/keyboard";
                    break;
            }

            switch (message.Text) //не чекає відповіді
            {
                case "/start":
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Ласкаво просимо до бота погоди. Виберіть " +
                        "команду, щоб продовжити /keyboard");
                    break;
                case "/myCities":
                    List<CityMain> cityMains = new List<CityMain>();
                    await GetAllWeather(message.Chat.Id);
                    break;
                case "/keyboard":
                    ReplyKeyboardMarkup replyKeyboardMarkup = new ReplyKeyboardMarkup(
                        new[]
                        {
                    new KeyboardButton[] { "Дізнатися погоду у місті" },
                    new KeyboardButton[] { "Міста в моєму списку", "Видалити місто зі списку" },
                        }
                    )
                    {
                        ResizeKeyboard = true
                    };
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Виберіть пункт меню:", replyMarkup: replyKeyboardMarkup);
                    break;
                case "Дізнатися погоду у місті":
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Для того щоб дізнатися погоду у місті виберіть цю команду /addCity");
                    break;
                case "Міста в моєму списку":
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Виберіть цю команду /myCities");
                    break;
                case "Видалити місто зі списку":
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Виберіть цю команду /deleteCity");
                    break;
            }


            async Task GetWaetherByAddCity(string query)
            {
                try
                {


                    WeatherClient weatherClient = new WeatherClient();

                    CityWeather cityWeather = await weatherClient.GetCityWeatherAsync(query);
                    await botClient.SendTextMessageAsync(message.Chat.Id, $"Погода у місті: {query}\n" +
                        $"Час запиту: {DateTime.Now}\n" +
                        $"Температура: {cityWeather.Main.Temp}°C \n" +
                        $"Видимість: {cityWeather.Visibility}м \n" +
                        $"Вологість: {cityWeather.Main.Humidity}% \n" +
                        $"Швидкість вітру: {cityWeather.Wind.Speed}км/год  \n" +
                        $"Пориви вітру: {cityWeather.Wind.Gust}км/год  \n" +
                        $"Тиск: {cityWeather.Main.Pressure}гПа  \n" +
                        $"Країна: {cityWeather.Sys.Country}");

                    DataBase dataBase = new DataBase();
                }
                catch (Exception ex)
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Виникла помилка при отриманні погодових даних. Будь ласка, перевірте правильність введених даних.");
                }

                currentStage[message.Chat.Id] = "";

            }
            async Task<List<CityMain>> SelectStatistWeather(long id)
            {
                NpgsqlConnection connection = new NpgsqlConnection(Constants.Connect);
                List<CityMain> review = new List<CityMain>();
                await connection.OpenAsync();

                var sql = $"select \"CityName\", \"Temp\", \"Time\",\"Visibility\", \"SpeedWind\", \"GustWind\", \"Pressure\", \"Humidity\", \"Country\" from public.\"CityWeather\"";

                NpgsqlCommand command = new NpgsqlCommand(sql, connection);

                try
                {
                    using (NpgsqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            review.Add(new CityMain(
                                cityName: reader.GetString(0),
                                temp: reader.GetDouble(1),
                                dateTime: reader.GetDateTime(2),
                                visibility: reader.GetInt32(3),
                                speedWind: reader.GetDouble(4),
                                gustWind: reader.GetDouble(5),
                                pressure: reader.GetDouble(6),
                                humidity: reader.GetDouble(7),
                                country: reader.GetString(8)
                            ));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Помилка під час виконання запиту: {ex.Message}");
                }

                await connection.CloseAsync();
                currentStage[message.Chat.Id] = "";
                return review;
            }

            async Task DeleteWeatherByUser(string quary)
            {
                DataBase database = new DataBase();
                await database.DeleteWeather(quary);
                await botClient.SendTextMessageAsync(message.Chat.Id, "Місто видалено");
                currentStage[message.Chat.Id] = "";

            }

            async Task GetAllWeather(long id)
            {
                Statistic statistic = new Statistic();
                var result = SelectStatistWeather(id).Result;

                foreach (var user in result)
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, $"Назва міста: {user.CityName}\n" +
                        $"Температура: {user.Temp}°C\n" +
                        $"Швидкість вітру: {user.SpeedWind}км/год\n" +
                        $"Пориви вітру: {user.GustWind}км/год \n" +
                        $"Тиск: {user.Pressure}гПа \n" +
                        $"Вологість: {user.Humidity}% \n" +
                        $"Країна: {user.Country} \n");
                    currentStage[message.Chat.Id] = "";
                }
            }

            return;
        }


    }
}


