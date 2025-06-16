using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Test_Task
{
    public class Client_Websocket_API
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
    }
}
