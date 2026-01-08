using System.Windows;
using System.Windows.Media; // Потрібно для кольорів (Brushes)

namespace NivaTradeDocs
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Підписуємось на події глобального сервісу синхронізації
            // Перевіряємо на null, щоб не було помилок у конструкторі вікна (Designer)
            if (App.GlobalSyncService != null)
            {
                App.GlobalSyncService.OnStatusChanged += UpdateStatusUI;
                App.GlobalSyncService.OnError += UpdateErrorUI;
            }
        }

        // Метод оновлення статусу (викликається з фонового потоку, тому треба Dispatcher)
        private void UpdateStatusUI(string msg)
        {
            Dispatcher.Invoke(() =>
            {
                // Оновлюємо нижній статус-бар
                if (txtFooterStatus != null)
                {
                    txtFooterStatus.Text = msg;
                    txtFooterStatus.Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80)); // Темний колір #2C3E50
                }

                // Оновлюємо великий напис на дашборді
                if (txtSyncStatusBig != null)
                {
                    txtSyncStatusBig.Text = msg;
                    txtSyncStatusBig.Foreground = Brushes.Green; // Зелений колір
                }
            });
        }

        // Метод обробки помилок
        private void UpdateErrorUI(string err)
        {
            Dispatcher.Invoke(() =>
            {
                if (txtFooterStatus != null)
                {
                    txtFooterStatus.Text = "ПОМИЛКА: " + err;
                    txtFooterStatus.Foreground = Brushes.Red;
                }
            });
        }

        // Обробник кнопки "Створити Замовлення" з меню
        private void BtnNewOrder_Click(object sender, RoutedEventArgs e)
        {
            // Створюємо та показуємо вікно замовлення
            var orderWin = new OrderWindow();
            orderWin.ShowDialog(); // ShowDialog блокує головне вікно, поки замовлення не закриють
        }
    }
}