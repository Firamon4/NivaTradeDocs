using Newtonsoft.Json;
using NivaTradeDocs.Data;
using NivaTradeDocs.Repositories;
using NivaTradeDocs.Services.DTO;
using System.Net.Http;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace NivaTradeDocs.Services
{
    public class SyncService
    {
        // Події для сповіщення зовнішнього світу (UI)
        public event Action<string> OnStatusChanged;     // "Завантажую...", "Готово"
        public event Action<string> OnError;             // Помилки
        public event Action<int> OnNewPackagesReceived;  // "Отримано 5 пакетів"

        private readonly string _apiUrl = "http://192.168.200.132:5000/api/sync"; // Заміни на свій IP якщо треба
        private readonly string _apiKey = "SecretKey123";
        private readonly string _shopId = "All";

        public async Task SyncDataAsync()
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-Api-Key", _apiKey);

            try
            {
                Notify("Перевірка нових даних...");

                // 1. PULL
                var response = await client.GetAsync($"{_apiUrl}/pull?target={_shopId}");
                if (!response.IsSuccessStatusCode)
                {
                    NotifyError($"Сервер: {response.StatusCode}");
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();
                var packages = JsonConvert.DeserializeObject<List<SyncPackageDto>>(json);

                if (packages == null || packages.Count == 0)
                {
                    // Немає вхідних, пробуємо відправити вихідні
                    await PushOrdersAsync(client);
                    Notify($"Синхронізовано. ({DateTime.Now:HH:mm})");
                    return;
                }

                Notify($"Отримано пакетів: {packages.Count}. Обробка...");

                using var db = new PosDbContext();
                db.Database.EnsureCreated();

                var prodRepo = new ProductRepository(db);
                var clientRepo = new CounterpartyRepository(db);
                var specRepo = new SpecificationRepository(db);

                int successCount = 0;

                foreach (var package in packages)
                {
                    bool success = false;
                    try
                    {
                        switch (package.DataType)
                        {
                            case "Product":
                                var products = JsonConvert.DeserializeObject<List<ProductDto>>(package.Payload);
                                if (products != null) foreach (var p in products) await prodRepo.UpsertAsync(p);
                                break;
                            case "Counterparty":
                                var clients = JsonConvert.DeserializeObject<List<CounterpartyDto>>(package.Payload);
                                if (clients != null) foreach (var c in clients) await clientRepo.UpsertAsync(c);
                                break;
                            case "Specification":
                                var specs = JsonConvert.DeserializeObject<List<SpecificationDto>>(package.Payload);
                                if (specs != null) foreach (var s in specs) await specRepo.UpsertAsync(s);
                                break;
                        }

                        await db.SaveChangesAsync();
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        NotifyError($"Помилка пакету {package.Id}: {ex.Message}");
                    }

                    if (success)
                    {
                        await client.DeleteAsync($"{_apiUrl}/ack?id={package.Id}");
                        successCount++;
                    }
                }

                // Після прийому - відправляємо замовлення
                await PushOrdersAsync(client);

                Notify($"Успішно оновлено пакетів: {successCount}");
                OnNewPackagesReceived?.Invoke(successCount);
            }
            catch (Exception ex)
            {
                NotifyError($"Помилка з'єднання: {ex.Message}");
            }
        }

        private async Task PushOrdersAsync(HttpClient client)
        {
            using var db = new PosDbContext();
            db.Database.EnsureCreated();

            var newOrders = await db.Orders
                                    .Include(o => o.Items)
                                    .Where(o => !o.IsSent)
                                    .ToListAsync();

            if (newOrders.Count == 0) return;

            Notify($"Відправка замовлень ({newOrders.Count})...");
            int sentCount = 0;

            foreach (var order in newOrders)
            {
                try
                {
                    var orderDto = new OrderDto
                    {
                        Uid = order.Id,
                        Date = order.Date,
                        CounterpartyUid = order.CounterpartyId,
                        Items = order.Items.Select(i => new OrderItemDto
                        {
                            ProductUid = i.ProductId,
                            Quantity = i.Quantity,
                            Price = i.Price
                        }).ToList()
                    };

                    var package = new
                    {
                        source = _shopId,
                        target = "1C_Main",
                        dataType = "Order",
                        payload = JsonConvert.SerializeObject(new List<OrderDto> { orderDto })
                    };

                    var content = new StringContent(JsonConvert.SerializeObject(package), Encoding.UTF8, "application/json");
                    var res = await client.PostAsync($"{_apiUrl}/push", content);

                    if (res.IsSuccessStatusCode)
                    {
                        order.IsSent = true;
                        sentCount++;
                    }
                }
                catch { /* ігноруємо, спробуємо потім */ }
            }

            if (sentCount > 0)
            {
                await db.SaveChangesAsync();
                Notify($"Відправлено замовлень: {sentCount}");
            }
        }

        private void Notify(string msg) => OnStatusChanged?.Invoke(msg);
        private void NotifyError(string msg) => OnError?.Invoke(msg);
    }
}