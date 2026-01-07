using Newtonsoft.Json;
using NivaTradeDocs.Data;
using NivaTradeDocs.Repositories;
using System.Net.Http;
using System.Text;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using NivaTradeDocs.Data.DTO;

namespace NivaTradeDocs.Services
{
    public class SyncService
    {
        private readonly string _apiUrl = "http://192.168.200.132:5000/api/sync";
        private readonly string _apiKey = "SecretKey123";
        private readonly string _shopId = "All"; // Або "Shop_01"

        // =========================================================
        // 1. ОТРИМАННЯ ДАНИХ З 1С 
        // =========================================================
        public async Task SyncDataAsync()
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-Api-Key", _apiKey);

            try
            {
                var response = await client.GetAsync($"{_apiUrl}/pull?target={_shopId}");
                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show($"Помилка сервера: {response.StatusCode}");
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();
                var packages = JsonConvert.DeserializeObject<List<SyncPackageDto>>(json);

                if (packages == null || packages.Count == 0)
                {
                    MessageBox.Show("Дані актуальні (нових пакетів немає).");
                    // Навіть якщо немає вхідних, пробуємо відправити вихідні (Замовлення)
                    await PushOrdersAsync();
                    return;
                }

                using var db = new PosDbContext();
                db.Database.EnsureCreated();

                // Підключаємо репозиторії
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
                        var msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                        MessageBox.Show($"Помилка пакету {package.Id}: {msg}");
                    }

                    if (success)
                    {
                        await client.DeleteAsync($"{_apiUrl}/ack?id={package.Id}");
                        successCount++;
                    }
                }

                if (successCount > 0)
                    MessageBox.Show($"Оновлено пакетів: {successCount}");

                // Після успішного отримання - відправляємо свої замовлення
                await PushOrdersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Критична помилка: {ex.Message}");
            }
        }

        // =========================================================
        // 2. ВІДПРАВКА ЗАМОВЛЕНЬ В 1С
        // =========================================================
        public async Task PushOrdersAsync()
        {
            using var db = new PosDbContext();
            db.Database.EnsureCreated();

            // Беремо всі замовлення, які ще НЕ відправлені
            var newOrders = await db.Orders
                                    .Include(o => o.Items)
                                    .Where(o => !o.IsSent)
                                    .ToListAsync();

            if (newOrders.Count == 0) return; // Нічого відправляти

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-Api-Key", _apiKey);

            int sentCount = 0;

            foreach (var order in newOrders)
            {
                try
                {
                    // Конвертуємо в DTO
                    var orderDto = new OrderDto
                    {
                        Uid = order.Id, // ВАЖЛИВО: Ми передаємо СВІЙ GUID
                        Date = order.Date,
                        CounterpartyUid = order.CounterpartyId,
                        Items = order.Items.Select(i => new OrderItemDto
                        {
                            ProductUid = i.ProductId,
                            Quantity = i.Quantity,
                            Price = i.Price
                        }).ToList()
                    };

                    // Пакуємо
                    var payloadJson = JsonConvert.SerializeObject(new List<OrderDto> { orderDto });
                    var package = new
                    {
                        source = _shopId,
                        target = "1C_Main",
                        dataType = "Order",
                        payload = payloadJson
                    };

                    var content = new StringContent(JsonConvert.SerializeObject(package), Encoding.UTF8, "application/json");

                    // Шлемо на сервер
                    var res = await client.PostAsync($"{_apiUrl}/push", content);

                    if (res.IsSuccessStatusCode)
                    {
                        order.IsSent = true; // Помічаємо як відправлене
                        sentCount++;
                    }
                }
                catch
                {
                    // Якщо одне замовлення не пішло - не страшно, спробуємо наступного разу
                }
            }

            if (sentCount > 0)
            {
                await db.SaveChangesAsync();
                MessageBox.Show($"Успішно відправлено замовлень: {sentCount}");
            }
        }
    } 
}