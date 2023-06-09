using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBotWeather.Models
{
    public class Statistic
    {
        public List<CityMain> CityMains { get; set; }
    }
    public class CityMain
    {
        public string CityName { get; set; }
        public double Temp { get; set; }
        public DateTime DateTime { get; set; }
        public int Visibility { get; set; }
        public double SpeedWind { get; set; }
        public double GustWind { get; set; }
        public double Pressure { get; set; }
        public double Humidity { get; set; }
        public string Country { get; set; }
        public CityMain (string cityName, double temp, DateTime  dateTime,Int32 visibility, double speedWind, double gustWind, double pressure, double humidity, string country)
        {
            CityName = cityName;
            Temp = temp;
            DateTime = dateTime;
            Visibility = visibility;
            SpeedWind = speedWind;
            GustWind = gustWind;
            Pressure = pressure;
            Humidity = humidity;
            Country = country;
        }

    }
}
