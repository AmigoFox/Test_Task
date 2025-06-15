using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;

namespace Test_Task
{
    internal class Client_REST_API
    {

        public class Trade
        {
            public string Pair { get; set; }
            public decimal Price { get; set; }
            public decimal Amount { get; set; }
            public string Side { get; set; }
            public string Id { get; set; }
            public DateTimeOffset Time { get; set; }
        }

        public class Candle
        {
            public string Pair { get; set; }
            public decimal OpenPrice { get; set; }
            public decimal HighPrice { get; set; }
            public decimal LowPrice { get; set; }
            public decimal ClosePrice { get; set; }
            public decimal TotalPrice { get; set; }
            public decimal TotalVolume { get; set; }
            public DateTimeOffset OpenTime { get; set; }
        }


        public class BinanceClient
        {
            private static readonly HttpClient _httpClient = new HttpClient();

            public async Task<List<Candle>> GetCandlesAsync(string symbol = "BTCUSDT", string interval = "1h", int limit = 10)
            {
                var url = $"https://api.binance.com/api/v3/klines?symbol={symbol}&interval={interval}&limit={limit}";
                var response = await _httpClient.GetStringAsync(url);

                var candles = new List<Candle>();

                using var jsonDoc = JsonDocument.Parse(response);

                foreach (var item in jsonDoc.RootElement.EnumerateArray())
                {
                    candles.Add(new Candle
                    {
                        OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(item[0].GetInt64()),

                        OpenPrice = Convert.ToDecimal(item[1].GetString()),
                        HighPrice = Convert.ToDecimal(item[2].GetString()),
                        LowPrice = Convert.ToDecimal(item[3].GetString()),
                        ClosePrice = Convert.ToDecimal(item[4].GetString()),
                        TotalVolume = Convert.ToDecimal(item[5].GetString())
                    });
                }

                return candles;
            }
        }

    }
}

