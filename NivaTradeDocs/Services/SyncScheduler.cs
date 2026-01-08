using System.Windows.Threading;

namespace NivaTradeDocs.Services
{
    public class SyncScheduler
    {
        private readonly SyncService _syncService;
        private readonly DispatcherTimer _timer;
        private bool _isSyncing = false;

        // Інтервал перевірки (наприклад, кожні 60 секунд)
        private readonly TimeSpan _interval = TimeSpan.FromSeconds(60);

        public SyncScheduler(SyncService syncService)
        {
            _syncService = syncService;

            // Використовуємо DispatcherTimer, щоб події оновлювали UI без проблем з потоками
            _timer = new DispatcherTimer();
            _timer.Interval = _interval;
            _timer.Tick += Timer_Tick;
        }

        public void Start()
        {
            _timer.Start();
            // Запускаємо першу синхронізацію одразу (асинхронно)
            Task.Run(PerformSync);
        }

        public void Stop() => _timer.Stop();

        private async void Timer_Tick(object? sender, EventArgs e)
        {
            await PerformSync();
        }

        private async Task PerformSync()
        {
            if (_isSyncing) return; // Якщо попередній раз ще не закінчився

            _isSyncing = true;
            try
            {
                await _syncService.SyncDataAsync();
            }
            finally
            {
                _isSyncing = false;
            }
        }
    }
}