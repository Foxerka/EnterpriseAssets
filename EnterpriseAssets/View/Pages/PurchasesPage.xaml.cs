using EnterpriseAssets.Model.DataBase;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace EnterpriseAssets.View.Pages
{
    /// <summary>
    /// Логика взаимодействия для PurchasesPage.xaml
    /// </summary>
    public partial class PurchasesPage : Page
    {
        // Контекст базы данных (Entity Framework)
        private DB_AssetManage _context;
        public int CurrentUserId { get; set; }

        // Все закупки (полный список до фильтрации)
        private List<PurchaseDisplay> _allPurchases;

        // Текущий выбранный поставщик для фильтра (ID или null для "Все")
        private int? _selectedSupplierId;

        // Текущий выбранный статус для фильтра (ID статуса или null для "Все")
        private int? _selectedStatusId;

        public PurchasesPage()
        {
            InitializeComponent();
            _context = new DB_AssetManage(); // Или new Entities(), в зависимости от реального имени контекста
        }

        // Загрузка страницы (синхронно)
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSuppliers();
            LoadPurchases();
        }

        // Загрузка списка поставщиков в комбобокс (синхронно)
        private void LoadSuppliers()
        {
            try
            {
                var suppliers = _context.SUPPLIERS
                    .Where(s => s.is_active == true || s.is_active == null)
                    .OrderBy(s => s.name)
                    .ToList(); // синхронно

                CmbSupplierFilter.Items.Clear();
                CmbSupplierFilter.Items.Add(new ComboBoxItem { Content = "Все поставщики", Tag = null, IsSelected = true });

                foreach (var s in suppliers)
                {
                    CmbSupplierFilter.Items.Add(new ComboBoxItem
                    {
                        Content = s.name,
                        Tag = s.id
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки поставщиков: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Загрузка данных о закупках (синхронно)
        private void LoadPurchases()
        {
            try
            {
                // Запрос с необходимыми связями
                var query = from p in _context.EQUIPMENT_PURCHASES
                            join e in _context.EQUIPMENT on p.asset_id equals e.ID into equipmentJoin
                            from e in equipmentJoin.DefaultIfEmpty()
                            join s in _context.SUPPLIERS on p.supplier_id equals s.id into supplierJoin
                            from s in supplierJoin.DefaultIfEmpty()
                            join m in _context.MASTERS on p.purchase_manager_id equals m.id into masterJoin
                            from m in masterJoin.DefaultIfEmpty()
                            join u in _context.USERS on m.user_id equals u.id into userJoin
                            from u in userJoin.DefaultIfEmpty()
                            join st in _context.STATUS_PURCHASE on p.status equals st.ID_status into statusJoin
                            from st in statusJoin.DefaultIfEmpty()
                            orderby p.order_date descending
                            select new
                            {
                                Purchase = p,
                                Equipment = e,
                                Supplier = s,
                                ManagerUser = u,
                                Status = st
                            };

                var data = query.ToList(); // синхронно

                // Преобразуем в список отображения
                _allPurchases = data.Select(x => new PurchaseDisplay
                {
                    Id = x.Purchase.id,
                    PurchaseNumber = x.Purchase.purchase_number ?? "Б/Н",
                    OrderDateDisplay = x.Purchase.order_date?.ToString("dd.MM.yyyy") ?? "—",
                    EquipmentName = x.Equipment != null
                        ? $"{x.Equipment.equipment_type} ({x.Equipment.manufacturer})"
                        : "Не указано",
                    SupplierName = x.Supplier?.name ?? "Не указан",
                    ManagerName = x.ManagerUser?.full_name ?? "Не назначен",
                    DeliveryDisplay = FormatDelivery(x.Purchase.expected_delivery, x.Purchase.actual_delivery),
                    DeliveryColor = GetDeliveryColor(x.Purchase.expected_delivery, x.Purchase.actual_delivery),
                    StatusName = x.Status?.Status ?? "Неизвестно",
                    StatusColor = GetStatusColor(x.Purchase.status),
                    QuantityDisplay = x.Purchase.quantity?.ToString() ?? "0",
                    TotalCostDisplay = x.Purchase.total_cost?.ToString("N2") + " ₽",
                    UnitPriceDisplay = (x.Purchase.unit_price ?? 0) > 0
                        ? (x.Purchase.unit_price?.ToString("N2") + " ₽/ед.")
                        : "",
                    StatusId = x.Purchase.status,
                    SupplierId = x.Purchase.supplier_id,
                    TotalCost = x.Purchase.total_cost ?? 0
                }).ToList();

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки закупок: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Форматирование строки доставки
        private string FormatDelivery(DateTime? expected, DateTime? actual)
        {
            if (actual.HasValue)
                return $"Доставлено: {actual.Value:dd.MM.yyyy}";
            if (expected.HasValue)
                return $"Ожидается: {expected.Value:dd.MM.yyyy}";
            return "Срок не указан";
        }

        // Цвет для срока доставки
        private Brush GetDeliveryColor(DateTime? expected, DateTime? actual)
        {
            if (actual.HasValue)
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")); // зелёный

            if (expected.HasValue)
            {
                if (expected.Value < DateTime.Today)
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C")); // просрочено - красный
                else
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F39C12")); // ожидается - оранжевый
            }

            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#95A5A6")); // серый
        }

        // Цвет статуса закупки (полоска слева)
        private Brush GetStatusColor(int? statusId)
        {
            return statusId switch
            {
                1 => new SolidColorBrush(Colors.Gray),       // Черновик
                2 => new SolidColorBrush(Colors.Orange),     // На согласовании
                3 => new SolidColorBrush(Colors.LightBlue),  // Согласовано
                4 => new SolidColorBrush(Colors.Purple),     // Заказано
                5 => new SolidColorBrush(Colors.Green),      // Доставлено
                6 => new SolidColorBrush(Colors.Red),        // Отменено
                _ => new SolidColorBrush(Colors.LightGray)
            };
        }

        // Применение фильтров (поиск, статус, поставщик)
        private void ApplyFilters()
        {
            if (_allPurchases == null) return;

            var filtered = _allPurchases.AsEnumerable();

            // Фильтр по поисковому тексту
            string searchText = TxtSearch.Text?.Trim().ToLower();
            if (!string.IsNullOrEmpty(searchText))
            {
                filtered = filtered.Where(p =>
                    (p.PurchaseNumber?.ToLower().Contains(searchText) ?? false) ||
                    (p.EquipmentName?.ToLower().Contains(searchText) ?? false) ||
                    (p.SupplierName?.ToLower().Contains(searchText) ?? false));
            }

            // Фильтр по статусу
            if (_selectedStatusId.HasValue)
            {
                filtered = filtered.Where(p => p.StatusId == _selectedStatusId.Value);
            }

            // Фильтр по поставщику
            if (_selectedSupplierId.HasValue)
            {
                filtered = filtered.Where(p => p.SupplierId == _selectedSupplierId.Value);
            }

            var resultList = filtered.ToList();
            PurchasesList.ItemsSource = resultList;

            // Обновление статистики внизу
            UpdateStatistics(resultList);
        }

        // Обновление нижней статистики
        private void UpdateStatistics(List<PurchaseDisplay> list)
        {
            TotalCount.Text = list.Count.ToString();
            decimal totalSum = list.Sum(p => p.TotalCost);
            TotalAmount.Text = totalSum.ToString("N2") + " ₽";

            // Ожидающие (статусы: Черновик, На согласовании, Согласовано, Заказано — исключая Доставлено и Отменено)
            int pending = list.Count(p => p.StatusId.HasValue &&
                                          p.StatusId.Value != 5 &&  // Доставлено
                                          p.StatusId.Value != 6);   // Отменено
            PendingCount.Text = pending.ToString();
        }

        // Обработчики фильтров
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void CmbStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbStatusFilter.SelectedItem is ComboBoxItem item)
            {
                string content = item.Content.ToString();
                _selectedStatusId = content switch
                {
                    "Черновик" => 1,
                    "На согласовании" => 2,
                    "Согласовано" => 3,
                    "Заказано" => 4,
                    "Доставлено" => 5,
                    "Отменено" => 6,
                    _ => (int?)null
                };
                ApplyFilters();
            }
        }

        private void CmbSupplierFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbSupplierFilter.SelectedItem is ComboBoxItem item)
            {
                _selectedSupplierId = item.Tag as int?; // Tag содержит ID поставщика или null
                ApplyFilters();
            }
        }

        // Кнопка "Новая закупка"
        private void BtnAddPurchase_Click(object sender, RoutedEventArgs e)
        {
            var win = new PurchaseManage(CurrentUserId); // передаём ID
            if (win.ShowDialog() == true)
            {
                LoadPurchases(); // обновить список после добавления
            }
        }

        // Кнопка "Обновить"
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadPurchases();
        }

        // Клик по карточке закупки 
        private void PurchaseCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is int purchaseId)
            {
                ViewPurchase(purchaseId);
            }
        }

        // Кнопка "Просмотр" в карточке
        private void ViewPurchase_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int purchaseId)
            {
                ViewPurchase(purchaseId);
            }
        }

        // Метод открытия просмотра
        private void ViewPurchase(int purchaseId)
        {
            MessageBox.Show($"Просмотр закупки ID: {purchaseId}", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Кнопка "Редактировать"
        private void EditPurchase_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int purchaseId)
            {
                var win = new PurchaseManage(purchaseId, CurrentUserId); // передаём ID и редактируемую закупку
                if (win.ShowDialog() == true)
                {
                    LoadPurchases();
                }
            }
        }

        // Кнопка "Удалить"
        private void DeletePurchase_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int purchaseId)
            {
                var result = MessageBox.Show("Вы действительно хотите удалить эту закупку?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var purchase = _context.EQUIPMENT_PURCHASES.Find(purchaseId); // синхронно
                        if (purchase != null)
                        {
                            _context.EQUIPMENT_PURCHASES.Remove(purchase);
                            _context.SaveChanges(); // синхронно
                            LoadPurchases(); // перезагружаем список
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        // Освобождение ресурсов контекста при выгрузке страницы
        //protected override void OnUnloaded(RoutedEventArgs e)
        //{
        //    _context?.Dispose();
        //    base.OnUnloaded(e);
        //}

        public class PurchaseDisplay
        {
            public int Id { get; set; }
            public string PurchaseNumber { get; set; }
            public string OrderDateDisplay { get; set; }
            public string EquipmentName { get; set; }
            public string SupplierName { get; set; }
            public string ManagerName { get; set; }
            public string DeliveryDisplay { get; set; }
            public Brush DeliveryColor { get; set; }
            public string StatusName { get; set; }
            public Brush StatusColor { get; set; }
            public string QuantityDisplay { get; set; }
            public string TotalCostDisplay { get; set; }
            public string UnitPriceDisplay { get; set; }

            // Служебные поля для фильтрации
            public int? StatusId { get; set; }
            public int? SupplierId { get; set; }
            public decimal TotalCost { get; set; }
        }
    }
}