using System.Windows;
using NivaTradeDocs.Services; // Не забудь додати цей using

namespace NivaTradeDocs
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Button_Sync_Click(object sender, RoutedEventArgs e)
        {
            // Блокуємо кнопку, щоб не натискали двічі
            var btn = sender as System.Windows.Controls.Button;
            btn.IsEnabled = false;

            var service = new SyncService();
            await service.SyncDataAsync();

            btn.IsEnabled = true;
        }
    }
}