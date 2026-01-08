using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using NivaTradeDocs.Data;
using NivaTradeDocs.Models;
using NivaTradeDocs.Repositories;
using NivaTradeDocs.Services.DTO;

namespace NivaTradeDocs
{
    public partial class OrderWindow : Window
    {
        private PosDbContext _db; // Тримаємо контекст відкритим поки вікно відкрите
        private Counterparty _selectedSupplier;

        // Колекція для прив'язки до таблиці замовлення (щоб оновлювалась сама)
        public ObservableCollection<OrderItemDisplay> OrderItems { get; set; } = new ObservableCollection<OrderItemDisplay>();

        public OrderWindow()
        {
            InitializeComponent();

            _db = new PosDbContext();
            _db.Database.EnsureCreated();

            gridOrder.ItemsSource = OrderItems;

            LoadSuppliers();
        }

        private async void LoadSuppliers()
        {
            var repo = new CounterpartyRepository(_db);
            var suppliers = await repo.GetAllAsync();
            listCounterparties.ItemsSource = suppliers;
        }

        // 1. Коли вибрали постачальника -> вантажимо специфікацію
        private async void ListCounterparties_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listCounterparties.SelectedItem is Counterparty supplier)
            {
                _selectedSupplier = supplier;
                OrderItems.Clear(); // Очищаємо старе замовлення при зміні постачальника
                UpdateTotal();

                var specRepo = new SpecificationRepository(_db);

                // ВАЖЛИВО: Той метод, що ми додали (шукає ОСТАННЮ специфікацію)
                var products = await specRepo.GetLatestItemsByCounterpartyAsync(supplier.Id);

                gridSpecItems.ItemsSource = products;

                if (products.Count == 0)
                    MessageBox.Show("Для цього постачальника немає активних специфікацій.");
            }
        }

        // 2. Подвійний клік по товару -> додаємо в замовлення
        private void GridSpecItems_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (gridSpecItems.SelectedItem is SpecItemDisplayModel specItem)
            {
                var existing = OrderItems.FirstOrDefault(x => x.ProductId == specItem.ProductId);
                if (existing != null)
                {
                    existing.Quantity++; // Якщо вже є - просто +1
                }
                else
                {
                    var newItem = new OrderItemDisplay
                    {
                        ProductId = specItem.ProductId,
                        ProductName = specItem.ProductName,
                        Price = specItem.Price,
                        Quantity = 1
                    };
                    // Підписуємось на зміни, щоб перераховувати "Всього"
                    newItem.PropertyChanged += (s, args) => UpdateTotal();
                    OrderItems.Add(newItem);
                }
                UpdateTotal();
            }
        }

        private void UpdateTotal()
        {
            decimal sum = OrderItems.Sum(x => x.Sum);
            txtTotal.Text = $"ВСЬОГО: {sum:N2}";
        }

        // 3. Збереження в базу
        private async void Button_Save_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedSupplier == null || OrderItems.Count == 0)
            {
                MessageBox.Show("Оберіть постачальника і додайте товари!");
                return;
            }

            // Створюємо справжнє Замовлення (Model)
            var newOrder = new Order
            {
                // Guid генерується автоматично в конструкторі Order
                Date = DateTime.Now,
                CounterpartyId = _selectedSupplier.Id,
                IsSent = false // Ще не відправлено в 1С
            };

            foreach (var item in OrderItems)
            {
                newOrder.Items.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Price
                    // Sum рахується автоматично
                });
            }

            _db.Orders.Add(newOrder);
            await _db.SaveChangesAsync();

            MessageBox.Show("Замовлення збережено! Воно відправиться при наступній синхронізації.");
            this.Close();
        }

        // Закриваємо з'єднання при закритті вікна
        protected override void OnClosed(EventArgs e)
        {
            _db.Dispose();
            base.OnClosed(e);
        }
    }

    // Допоміжний клас для таблиці замовлення (щоб працювала магія оновлення чисел)
    public class OrderItemDisplay : INotifyPropertyChanged
    {
        private decimal _quantity;

        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }

        public decimal Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Sum)); // Якщо змінилась к-сть, змінилась і сума
            }
        }

        public decimal Sum => Quantity * Price;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}