
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml;
using static Test_Task.Client_REST_API;
namespace Test_Task
{
    public partial class MainPage : ContentPage
    {
        Client_REST_API client = new Client_REST_API();

        public MainPage()
        {
            InitializeComponent();
            _client = new Client_Websocket_API();
            _client.CandleSeriesProcessing += OnNewCandle;

            CandlesCollectionView.ItemsSource = _candles;
        }



        private void OnNewCandle(Candle candle)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var existing = _candles.FirstOrDefault(c => c.OpenTime == candle.OpenTime && c.Pair == candle.Pair);
                if (existing != null)
                {
                    existing.OpenPrice = candle.OpenPrice;
                    existing.HighPrice = candle.HighPrice;
                    existing.LowPrice = candle.LowPrice;
                    existing.ClosePrice = candle.ClosePrice;
                    existing.TotalVolume = candle.TotalVolume;
                }
                else
                {
                    _candles.Add(candle);
                }
            });
        }


        private ObservableCollection<Candle> _candles = new ObservableCollection<Candle>();
        private Client_Websocket_API _client;

        private async void GetInfo(object sender, EventArgs e)
        {
            try
            {
                _candles.Clear();
                string[] pairs = new[] { "btcusdt", "xrpusdt", "xmrusdt", "dashusdt" };
                int seconds = _selectedInterval switch
                {
                    "1m" => 60,
                    "5m" => 300,
                    "15m" => 900,
                    "1h" => 3600,
                    "4h" => 14400,
                    "1d" => 86400,
                    _ => 60
                };

                foreach (var pair in pairs)
                    _client.SubscribeCandles(pair, seconds);

                await DisplayAlert("ОК", $"Получение информации {_selectedInterval}", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }




        private async void OnOpenPortfolioClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new PortfolioPage());
        }


        private string _selectedInterval = "1m";

        private void OnIntervalChanged(object sender, EventArgs e)
        {
            var picker = (Picker)sender;
            if (picker.SelectedItem is string selected)
            {
                _selectedInterval = selected;
            }
        }


    }
}


