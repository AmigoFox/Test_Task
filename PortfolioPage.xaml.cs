using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace Test_Task
{
    public partial class PortfolioPage : ContentPage
    {
        private readonly Client_REST_API _client = new Client_REST_API();

        public PortfolioPage()
        {
            InitializeComponent();
        }

        private async void OnCalculatePortfolioClicked(object sender, EventArgs e)
        {
            try
            {
                var originalBalances = new Dictionary<string, decimal>
                {
                    { "BTC", 1m },
                    { "XRP", 15000m },
                    { "XMR", 50m },
                    { "DASH", 30m }
                };
                var originalDisplay = originalBalances.Select(b => new OriginalBalance
                {
                    Currency = b.Key,
                    Amount = b.Value
                }).ToList();

                OriginalBalanceCollectionView.ItemsSource = originalDisplay;

                var portfolio = await _client.GetPortfolioInAllCurrencies();
                var displayList = portfolio.Select(p => new PortfolioDisplay
                {
                    Currency = p.Key,
                    Value = p.Value
                }).ToList();

                PortfolioCollectionView.ItemsSource = displayList;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "ОК");
            }
        }
        public class OriginalBalance
        {
            public string Currency { get; set; }
            public decimal Amount { get; set; }
        }

        public class PortfolioDisplay
        {
            public string Currency { get; set; }
            public decimal Value { get; set; }
            public string ConversionNote => $"(Всё переведено в {Currency})";
        }
    }
}
