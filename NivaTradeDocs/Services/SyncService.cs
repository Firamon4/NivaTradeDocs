using Newtonsoft.Json;
using NivaTradeDocs.Data;
using System.Net.Http;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace NivaTradeDocs.Services
{
    public class SyncService
    {
        // Якщо жорстко задав в API 0.0.0.0:5000, то тут лишаємо localhost
        private readonly string _apiUrl = "http://localhost:5000/api/sync";
        private readonly string _apiKey = "SecretKey123";
        private readonly string _shopId = "All";

        // Головний метод синхронізації (Товари + Контрагенти)
        public async Task SyncDataAsync()
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-Api-Key", _apiKey);

            try
            {
                // 1. Отримуємо пакети
                var response = await client.GetAsync($"{_apiUrl}/pull?target={_shopId}");
                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show($"Сервер відповів помилкою: {response.StatusCode}");
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();
                var packages = JsonConvert.DeserializeObject<List<SyncPackageDto>>(json);

                if (packages == null || packages.Count == 0)
                {
                    MessageBox.Show("Нових даних немає.");
                    return;
                }

                using var db = new PosDbContext();
                db.Database.EnsureCreated();

                int successCount = 0;

                // 2. Обробляємо пакети ПО ОДНОМУ
                foreach (var package in packages)
                {
                    bool packageProcessedSuccessfully = false;

                    try
                    {
                        // === РОЗПОДІЛЬНИЙ ЦЕНТР (SWITCH) ===
                        switch (package.DataType)
                        {
                            case "Product":
                                var productsData = JsonConvert.DeserializeObject<List<ProductDto>>(package.Payload);
                                if (productsData != null)
                                {
                                    foreach (var pDto in productsData) await UpsertProduct(db, pDto);
                                }
                                break;

                            case "Counterparty":
                                var clientsData = JsonConvert.DeserializeObject<List<CounterpartyDto>>(package.Payload);
                                if (clientsData != null)
                                {
                                    foreach (var cDto in clientsData) await UpsertCounterparty(db, cDto);
                                }
                                break;

                            default:
                                // Якщо прийшов невідомий тип - просто ігноруємо, але вважаємо обробленим,
                                // щоб видалити з черги і не застрягати.
                                break;
                        }

                        // ВАЖЛИВО: Зберігаємо зміни в локальну базу ПЕРЕД тим, як підтвердити серверу
                        await db.SaveChangesAsync();

                        // Якщо ми дійшли сюди - значить SQLite прийняла дані без помилок
                        packageProcessedSuccessfully = true;
                    }
                    catch (Exception ex)
                    {
                        var err = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                        MessageBox.Show($"Помилка при збереженні пакету {package.Id} ({package.DataType}): {err}\nСинхронізацію зупинено.");
                        return;
                    }

                    // 3. Тільки якщо все успішно - кажемо серверу видалити пакет
                    if (packageProcessedSuccessfully)
                    {
                        var ackResponse = await client.DeleteAsync($"{_apiUrl}/ack?id={package.Id}");
                        if (ackResponse.IsSuccessStatusCode)
                        {
                            successCount++;
                        }
                    }
                }

                if (successCount > 0)
                    MessageBox.Show($"Успішно завантажено пакетів: {successCount}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Загальна помилка: {ex.Message}");
            }
        }

        private async Task UpsertCounterparty(PosDbContext db, CounterpartyDto dto)
        {
            var existing = await db.Counterparties.FindAsync(dto.Uid);

            if (existing == null)
            {
                db.Counterparties.Add(new Counterparty
                {
                    Id = dto.Uid,
                    Name = dto.Name,
                    Code = dto.Code,
                    TaxId = dto.TaxId,
                    IsDeleted = dto.IsDeleted
                });
            }
            else
            {
                existing.Name = dto.Name;
                existing.Code = dto.Code;
                existing.TaxId = dto.TaxId;
                existing.IsDeleted = dto.IsDeleted;
            }
        }

        private async Task UpsertProduct(PosDbContext db, ProductDto dto)
        {
            // Використовуємо FindAsync, щоб уникнути дублів в межах одного пакету
            var existing = await db.Products.FindAsync(dto.Uid);

            if (existing == null)
            {
                db.Products.Add(new Product
                {
                    Id = dto.Uid,
                    Name = dto.Name,
                    Code = dto.Code,
                    Barcode = dto.Barcode,
                    Articul = dto.Articul,
                    IsFolder = dto.IsFolder,
                    ParentId = string.IsNullOrEmpty(dto.ParentUid) ? null : dto.ParentUid
                });
            }
            else
            {
                existing.Name = dto.Name;
                existing.Code = dto.Code;
                existing.Barcode = dto.Barcode;
                existing.Articul = dto.Articul;
                existing.IsFolder = dto.IsFolder;
                existing.ParentId = string.IsNullOrEmpty(dto.ParentUid) ? null : dto.ParentUid;
            }
        }
    }

    // DTO класи
    public class SyncPackageDto
    {
        public int Id { get; set; }
        public string DataType { get; set; }
        public string Payload { get; set; }
    }

    public class ProductDto
    {
        public string Uid { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Barcode { get; set; }
        public string Articul { get; set; }
        public bool   IsFolder { get; set; }
        public bool   IsDeleted { get; set; }
        public string ParentUid { get; set; }
    }

    public class CounterpartyDto
    {
        public string Uid { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string TaxId { get; set; }
        public bool   IsDeleted { get; set; }
    }
}