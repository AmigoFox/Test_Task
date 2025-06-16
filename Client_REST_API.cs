using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Test_Task
{
    public class Client_REST_API
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.binance.com/api/v3/")
        };

        public async Task<decimal> GetLastPriceAsync(string symbol)
        {
            var url = $"ticker/price?symbol={symbol}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            string priceStr = doc.RootElement.GetProperty("price").GetString();

            if (decimal.TryParse(priceStr, out decimal price))
                return price;
            else
                throw new Exception($"Не удалось преобразовать цену для пары {symbol}");
        }

        public async Task<Dictionary<string, decimal>> GetPortfolioInAllCurrencies()
        {
            var balances = new Dictionary<string, decimal>
            {
                { "BTC", 1m },
                { "XRP", 15000m },
                { "XMR", 50m },
                { "DASH", 30m }
            };

            var symbols = new[] { "BTC", "XRP", "XMR", "DASH", "USDT" };
            var prices = new Dictionary<string, decimal>();

            foreach (var symbol in symbols)
            {
                if (symbol == "USDT")
                {
                    prices[symbol] = 1m;
                    continue;
                }

                string pair = $"{symbol}USDT";
                try
                {
                    prices[symbol] = await GetLastPriceAsync(pair);
                }
                catch
                {
                    prices[symbol] = 0m;
                }
            }

            var result = new Dictionary<string, decimal>();

            foreach (var target in symbols)
            {
                decimal sum = 0;
                foreach (var (coin, amount) in balances)
                {
                    decimal inUSDT = prices[coin] * amount;
                    decimal converted = target == "USDT" ? inUSDT : inUSDT / prices[target];
                    sum += converted;
                }
                result[target] = Math.Round(sum, 2);
            }

            return result;
        }

        public async Task<List<Candle>> GetCandlesAsync(string symbol = "BTCUSDT", string interval = "1h", int limit = 10)
        {
            var url = $"klines?symbol={symbol}&interval={interval}&limit={limit}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var elements = System.Text.Json.JsonSerializer.Deserialize<List<List<JsonElement>>>(json);

            var candles = new List<Candle>();

            foreach (var item in elements)
            {
                candles.Add(new Candle
                {
                    OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(item[0].GetInt64()),
                    OpenPrice = decimal.Parse(item[1].GetString()),
                    HighPrice = decimal.Parse(item[2].GetString()),
                    LowPrice = decimal.Parse(item[3].GetString()),
                    ClosePrice = decimal.Parse(item[4].GetString()),
                    TotalVolume = decimal.Parse(item[5].GetString()),
                    Pair = symbol
                });
            }
            return candles;
        }
        public class Candle
        {
            public string Pair { get; set; }
            public DateTimeOffset OpenTime { get; set; }
            public decimal OpenPrice { get; set; }
            public decimal HighPrice { get; set; }
            public decimal LowPrice { get; set; }
            public decimal ClosePrice { get; set; }
            public decimal TotalVolume { get; set; }
        }
    }
}
