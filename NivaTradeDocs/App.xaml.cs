using System.Windows;
using NivaTradeDocs.Services;

namespace NivaTradeDocs
{
    public partial class App : Application
    {
        public static SyncService GlobalSyncService { get; private set; }
        private SyncScheduler _scheduler;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 1. Створюємо сервіс (один на всю програму)
            GlobalSyncService = new SyncService();

            // 2. Створюємо планувальник
            _scheduler = new SyncScheduler(GlobalSyncService);

            // 3. Запускаємо "серцебиття"
            _scheduler.Start();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _scheduler.Stop();
            base.OnExit(e);
        }
    }
}