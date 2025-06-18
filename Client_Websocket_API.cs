using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Test_Task
{
    public class Client_Websocket_API : ITestConnector
    {
        private ClientWebSocket _ws;
        private Uri _uri = new Uri("wss://stream.binance.com:9443/ws");

        public async Task ConnectAndSubscribeTradesAsync(string symbol = "btcusdt")
        {
            _ws = new ClientWebSocket();
            await _ws.ConnectAsync(_uri, CancellationToken.None);
            string streamName = $"{symbol}@trade";

            var subscribeMsg = new
            {
                method = "SUBSCRIBE",
                @params = new[] { streamName },
                id = 1
            };
            string message = JsonSerializer.Serialize(subscribeMsg);
            await _ws.SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, true, CancellationToken.None);

            var buffer = new byte[8192];
            while (_ws.State == WebSocketState.Open)
            {
                var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                var responseString = Encoding.UTF8.GetString(buffer, 0, result.Count);

                Console.WriteLine($"[WS] {responseString}");
            }
        }

        public async Task ConnectAndSubscribeCandlesAsync(string symbol = "btcusdt", string interval = "1m")
        {
            _ws = new ClientWebSocket();
            await _ws.ConnectAsync(_uri, CancellationToken.None);

            string streamName = $"{symbol}@kline_{interval}";

            var subscribeMsg = new
            {
                method = "SUBSCRIBE",
                @params = new[] { streamName },
                id = 1
            };
            string message = JsonSerializer.Serialize(subscribeMsg);
            await _ws.SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, true, CancellationToken.None);

            var buffer = new byte[8192];
            while (_ws.State == WebSocketState.Open)
            {
                var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                var responseString = Encoding.UTF8.GetString(buffer, 0, result.Count);

                Console.WriteLine($"[WS] {responseString}");
            }
        }
        public async Task DisconnectAsync()
        {
            if (_ws != null)
            {
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnect", CancellationToken.None);
                _ws.Dispose();
            }
        }

        public event Action<Trade> NewBuyTrade;
        public event Action<Trade> NewSellTrade;
        public event Action<Candle> CandleSeriesProcessing;
        private ClientWebSocket _wsTrade;
        private ClientWebSocket _wsCandle;

        public void SubscribeTrades(string pair, int maxCount = 100)
        {
            Task.Run(async () =>
            {
                _wsTrade = new ClientWebSocket();
                await _wsTrade.ConnectAsync(new Uri("wss://stream.binance.com:9443/ws"), CancellationToken.None);

                var msg = new
                {
                    method = "SUBSCRIBE",
                    @params = new[] { $"{pair.ToLower()}@trade" },
                    id = 1
                };

                var json = JsonSerializer.Serialize(msg);
                await _wsTrade.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, CancellationToken.None);

                var buffer = new byte[8192];
                while (_wsTrade.State == WebSocketState.Open)
                {
                    var result = await _wsTrade.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    using var doc = JsonDocument.Parse(message);
                    if (doc.RootElement.TryGetProperty("e", out var e) && e.GetString() == "trade")
                    {
                        var price = decimal.Parse(doc.RootElement.GetProperty("p").GetString());
                        var amount = decimal.Parse(doc.RootElement.GetProperty("q").GetString());
                        var time = DateTimeOffset.FromUnixTimeMilliseconds(doc.RootElement.GetProperty("T").GetInt64());
                        var isBuyerMarketMaker = doc.RootElement.GetProperty("m").GetBoolean();

                        var trade = new Trade
                        {
                            Pair = pair,
                            Price = price,
                            Amount = amount,
                            Time = time,
                            Side = isBuyerMarketMaker ? "sell" : "buy"
                        };

                        if (trade.Side == "buy") NewBuyTrade?.Invoke(trade);
                        else NewSellTrade?.Invoke(trade);
                    }
                }
            });
        }




        public void SubscribeCandles(string pair, int periodInSec, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = 0)
        {
            Task.Run(async () =>
            {
                _wsCandle = new ClientWebSocket();
                await _wsCandle.ConnectAsync(new Uri("wss://stream.binance.com:9443/ws"), CancellationToken.None);

                string interval = periodInSec switch
                {
                    <= 60 => "1m",
                    <= 180 => "3m",
                    <= 300 => "5m",
                    <= 900 => "15m",
                    <= 1800 => "30m",
                    <= 3600 => "1h",
                    <= 14400 => "4h",
                    <= 86400 => "1d",
                    _ => "1m"
                };

                var msg = new
                {
                    method = "SUBSCRIBE",
                    @params = new[] { $"{pair.ToLower()}@kline_{interval}" },
                    id = 1
                };

                var json = JsonSerializer.Serialize(msg);
                await _wsCandle.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, CancellationToken.None);

                var buffer = new byte[8192];
                while (_wsCandle.State == WebSocketState.Open)
                {
                    var result = await _wsCandle.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    using var doc = JsonDocument.Parse(message);
                    if (doc.RootElement.TryGetProperty("e", out var e) && e.GetString() == "kline")
                    {
                        var k = doc.RootElement.GetProperty("k");
                        var candle = new Candle
                        {
                            Pair = pair,
                            OpenPrice = decimal.Parse(k.GetProperty("o").GetString()),
                            HighPrice = decimal.Parse(k.GetProperty("h").GetString()),
                            LowPrice = decimal.Parse(k.GetProperty("l").GetString()),
                            ClosePrice = decimal.Parse(k.GetProperty("c").GetString()),
                            TotalVolume = decimal.Parse(k.GetProperty("v").GetString()),
                            TotalPrice = decimal.Parse(k.GetProperty("q").GetString()),
                            OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(k.GetProperty("t").GetInt64())
                        };

                        CandleSeriesProcessing?.Invoke(candle);
                    }
                }
            });
        }


        public Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount)
        {
            // REST-клиент — перенаправь туда
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to = null, long? count = 0)
        {
            // Аналогично, можно подключить REST API или оставить заглушку
            throw new NotImplementedException();
        }

        public void UnsubscribeTrades(string pair)
        {
            var unsubscribeMsg = new
            {
                method = "UNSUBSCRIBE",
                @params = new[] { $"{pair.ToLower()}@trade" },
                id = 3
            };

            var json = JsonSerializer.Serialize(unsubscribeMsg);
            if (_wsTrade?.State == WebSocketState.Open)
            {
                _wsTrade.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        public void UnsubscribeCandles(string pair)
        {
            var unsubscribeMsg = new
            {
                method = "UNSUBSCRIBE",
                @params = new[] { $"{pair.ToLower()}@kline_1m" }, // Здесь нужно хранить interval, если был другой
                id = 4
            };

            var json = JsonSerializer.Serialize(unsubscribeMsg);
            if (_wsCandle?.State == WebSocketState.Open)
            {
                _wsCandle.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        public void SubscribeAllCandles()
        {
            string[] pairs = new[] { "btcusdt", "xrpusdt", "xmrusdt", "dashusdt" };
            foreach (var pair in pairs)
            {
                SubscribeCandles(pair, 60);
            }
        }
    }
}
