
using System;
using System.Collections.Generic;
using System.Xml;
using static Test_Task.Client_REST_API;
namespace Test_Task
{
    public partial class MainPage : ContentPage
    {
        BinanceClient client = new BinanceClient();

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
    }
}


