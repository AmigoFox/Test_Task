
using System;
using System.Collections.Generic;
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
        }

        private async void GetInfo(object sender, EventArgs e)
        {
            try
            {
                var candles = await client.GetCandlesAsync();
                CandlesCollectionView.ItemsSource = candles;
                await DisplayAlert("ок", "Получение информации", "ок");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        private void ShowBalanceClicked(object sender, EventArgs e)
        {
            var balances = new Dictionary<string, decimal>
            {
                { "BTC", 1m },
                { "XRP", 15000m },
                { "XMR", 50m },
                { "DASH", 30m }
            };

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("📌 Изначальный баланс портфеля:");
            foreach (var kvp in balances)
            {
                sb.AppendLine($"{kvp.Key}: {kvp.Value}");
            }

            DisplayAlert("Изначальный баланс", sb.ToString(), "Ок");
        }


        private async void OnOpenPortfolioClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new PortfolioPage());
        }


    }
}


