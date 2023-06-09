using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using System.Diagnostics.Metrics;
using TelegramBotWeather.Models;
using System.Threading;
using System.Collections.Generic;
using Telegram.Bot.Types;
using System.Xml.Linq;


namespace TelegramBotWeather
{
    public class Constants
    {
        
        public static string address = "https://localhost:7290";
        public static string Connect = "Host=localhost;Username=postgres;Password=123123;Database=postgres";

    }
    public class WeatherClient
    {
        private HttpClient _httpClient;
        private static string _address;

        public WeatherClient()
        {
            _address = Constants.address;
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_address);

        }
        public async Task<CityWeather> GetCityWeatherAsync(string query)
        {

            var response = await _httpClient.GetAsync($"/CityWeather?query={query}"); 
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<CityWeather>(content);


            return result;
        }

        public class DataBase
        {
            NpgsqlConnection connection = new NpgsqlConnection(Constants.Connect);
            public async Task InsertCityWeatherAsync(string query, CityWeather cityWeather)
            {
                var sql = "INSERT INTO public.\"CityWeather\"(\"CityName\", \"Temp\", \"Time\", \"Visibility\", \"SpeedWind\", \"GustWind\", \"Pressure\", \"Humidity\", \"Country\")"
                    + "VALUES (@CityName, @Temp, @Time, @Visibility, @SpeedWind, @GustWind, @Pressure, @Humidity, @Country)";

                NpgsqlCommand command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("CityName", query);
                command.Parameters.AddWithValue("Temp", cityWeather.Main.Temp);
                command.Parameters.AddWithValue("Time", DateTime.Now);
                command.Parameters.AddWithValue("Visibility", cityWeather.Visibility);
                command.Parameters.AddWithValue("SpeedWind", cityWeather.Wind.Speed);
                command.Parameters.AddWithValue("GustWind", cityWeather.Wind.Gust);
                command.Parameters.AddWithValue("Pressure", cityWeather.Main.Pressure);
                command.Parameters.AddWithValue("Humidity", cityWeather.Main.Humidity);
                command.Parameters.AddWithValue("Country", cityWeather.Sys.Country);

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
                await connection.CloseAsync();
            }
            public async Task DeleteWeather(string quary)
            {
                NpgsqlConnection connection = new NpgsqlConnection(Constants.Connect);
                await connection.OpenAsync();

                var sql = "delete from public.\"CityWeather\" where \"CityName\" = @CityName";
                NpgsqlCommand command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("CityName", quary);
                await command.ExecuteNonQueryAsync();
                await connection.CloseAsync();

            }


            
        }
    }
}
