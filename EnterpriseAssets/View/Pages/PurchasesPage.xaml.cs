using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Data.Entity;
using EnterpriseAssets.Model.DataBase;

namespace EnterpriseAssets.View.Pages
{
    public partial class PurchasesPage : Page
    {
        private DB_AssetManage db = new DB_AssetManage();

        public PurchasesPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadFilters();
            LoadPurchases();
        }

        // 🔹 Загрузка фильтров
        private void LoadFilters()
        {
            try
            {
                // Поставщики
                CmbSupplierFilter.Items.Clear();
                CmbSupplierFilter.Items.Add(new ComboBoxItem { Content = "Все поставщики", Tag = (int?)null });

                var suppliers = db.SUPPLIERS.OrderBy(s => s.name).ToList();
                foreach (var s in suppliers)
                {
                    CmbSupplierFilter.Items.Add(new ComboBoxItem { Content = s.name, Tag = s.id });
                }
                CmbSupplierFilter.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ LoadFilters: {ex.Message}");
            }
        }

        // 🔹 Загрузка закупок
        private void LoadPurchases()
        {
            try
            {
                var query = db.EQUIPMENT_PURCHASES
                    .Include(p => p.EQUIPMENT)
                    .Include(p => p.SUPPLIERS)
                    .Include(p => p.MASTERS)
                    .Include(p => p.MASTERS.USERS)  // ✅ Цепочка навигации
                    .Include(p => p.STATUS_PURCHASE)
                    .AsQueryable();

                // 🔍 Поиск
                if (!string.IsNullOrWhiteSpace(TxtSearch?.Text))
                {
                    var search = TxtSearch.Text.ToLower();
                    query = query.Where(p =>
                        (p.purchase_number != null && p.purchase_number.ToLower().Contains(search)) ||
                        (p.EQUIPMENT != null && p.EQUIPMENT.asset_id != null && p.EQUIPMENT.asset_id.ToLower().Contains(search)) ||
                        (p.SUPPLIERS != null && p.SUPPLIERS.name != null && p.SUPPLIERS.name.ToLower().Contains(search)));
                }

                // 🔍 Фильтр по статусу
                if (CmbStatusFilter != null && CmbStatusFilter.SelectedIndex > 0)
                {
                    if (CmbStatusFilter.SelectedItem is ComboBoxItem statusItem)
                    {
                        var statusName = statusItem.Content?.ToString();
                        if (!string.IsNullOrEmpty(statusName))
                        {
                            query = query.Where(p => p.STATUS_PURCHASE != null && p.STATUS_PURCHASE.Status == statusName);
                        }
                    }
                }

                // 🔍 Фильтр по поставщику
                if (CmbSupplierFilter != null && CmbSupplierFilter.SelectedIndex > 0)
                {
                    if (CmbSupplierFilter.SelectedItem is ComboBoxItem supplierItem)
                    {
                        if (supplierItem.Tag is int supplierId)
                        {
                            query = query.Where(p => p.supplier_id == supplierId);
                        }
                    }
                }

                // ✅ Сначала загружаем данные из БД
                var purchases = query.OrderByDescending(p => p.created_at).ToList();

                // ✅ Потом создаём ViewModel (уже в памяти, можно использовать string.Format)
                var viewModelList = purchases.Select(p => new PurchaseViewModel
                {
                    Id = p.id,
                    PurchaseNumber = !string.IsNullOrEmpty(p.purchase_number) ? p.purchase_number : $"#{p.id}",
                    EquipmentName = GetAssetNameWithType(p.asset_id),
                    SupplierName = p.SUPPLIERS != null ? p.SUPPLIERS.name : "—",
                    ManagerName = (p.MASTERS != null && p.MASTERS.USERS != null)
                        ? (p.MASTERS.USERS.full_name ?? p.MASTERS.USERS.username ?? "—")
                        : "—",

                    Quantity = p.quantity ?? 0,
                    UnitPrice = p.unit_price ?? 0,
                    TotalCost = p.total_cost ?? 0,

                    OrderDate = p.order_date,
                    ExpectedDelivery = p.expected_delivery,
                    ActualDelivery = p.actual_delivery,

                    StatusName = p.STATUS_PURCHASE != null ? p.STATUS_PURCHASE.Status : "—",
                    StatusColor = GetStatusColor(p.STATUS_PURCHASE != null ? p.STATUS_PURCHASE.Status : null),

                    QuantityDisplay = $"{p.quantity ?? 0} шт.",
                    UnitPriceDisplay = p.unit_price.HasValue ? $"{p.unit_price:C}/шт." : "—",
                    TotalCostDisplay = p.total_cost.HasValue ? $"{p.total_cost:C}" : "—",

                    OrderDateDisplay = p.order_date?.ToString("dd.MM.yyyy") ?? "—",
                    DeliveryDisplay = GetDeliveryDisplay(p.expected_delivery, p.actual_delivery),
                    DeliveryColor = GetDeliveryColor(p.expected_delivery, p.actual_delivery)
                }).ToList();

                PurchasesList.ItemsSource = viewModelList;
                UpdateStats(purchases);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ LoadPurchases: {ex.Message}");
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetAssetNameWithType(int? assetId)
        {
            if (!assetId.HasValue) return "—";

            try
            {
                var asset = db.PRODUCTION_ASSETS
                    .Include("ASSETTYPE")
                    .FirstOrDefault(a => a.id == assetId.Value);

                if (asset == null) return "—";

                var typeName = asset.ASSETTYPE?.AssetType1 ?? "Актив";
                return $"{asset.name} ({typeName})";
            }
            catch
            {
                return "—";
            }
        }

        // 🔹 ViewModel
        public class PurchaseViewModel
        {
            public int Id { get; set; }
            public string PurchaseNumber { get; set; }
            public string EquipmentName { get; set; }
            public string SupplierName { get; set; }
            public string ManagerName { get; set; }

            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal TotalCost { get; set; }

            public DateTime? OrderDate { get; set; }
            public DateTime? ExpectedDelivery { get; set; }
            public DateTime? ActualDelivery { get; set; }

            public string StatusName { get; set; }
            public string StatusColor { get; set; }

            public string QuantityDisplay { get; set; }
            public string UnitPriceDisplay { get; set; }
            public string TotalCostDisplay { get; set; }
            public string OrderDateDisplay { get; set; }
            public string DeliveryDisplay { get; set; }
            public string DeliveryColor { get; set; }
        }

        // 🔹 Вспомогательные методы
        private string GetStatusColor(string statusName)
        {
            return statusName?.Trim().ToLower() switch
            {
                "черновик" => "#95A5A6",
                "на согласовании" => "#F39C12",
                "согласовано" => "#3498DB",
                "заказано" => "#9B59B6",
                "доставлено" => "#27AE60",
                "отменено" => "#E74C3C",
                _ => "#7F8C8D"
            };
        }

        private string GetDeliveryDisplay(DateTime? expected, DateTime? actual)
        {
            if (actual.HasValue) return $"✅ Доставлено {actual.Value:dd.MM.yyyy}";
            if (!expected.HasValue) return "📅 Срок не указан";

            var daysLeft = (expected.Value - DateTime.Now).Days;
            if (daysLeft < 0) return $"❌ Просрочено на {Math.Abs(daysLeft)} дн.";
            if (daysLeft <= 3) return $"⚠️ Через {daysLeft} дн.";
            return $"📅 {expected.Value:dd.MM.yyyy}";
        }

        private string GetDeliveryColor(DateTime? expected, DateTime? actual)
        {
            if (actual.HasValue) return "#27AE60";
            if (!expected.HasValue) return "#7F8C8D";

            var daysLeft = (expected.Value - DateTime.Now).Days;
            if (daysLeft < 0) return "#E74C3C";
            if (daysLeft <= 3) return "#E67E22";
            return "#27AE60";
        }

        private void UpdateStats(List<EQUIPMENT_PURCHASES> purchases)
        {
            TotalCount.Text = purchases.Count.ToString();
            TotalAmount.Text = $"{purchases.Sum(p => p.total_cost ?? 0):C}";
            PendingCount.Text = purchases.Count(p =>
                p.STATUS_PURCHASE?.Status == "На согласовании" ||
                p.STATUS_PURCHASE?.Status == "Черновик").ToString();
        }

        // 🔹 Обработчики
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => LoadPurchases();
        private void CmbStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadPurchases();
        private void CmbSupplierFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadPurchases();
        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => LoadPurchases();

        private void BtnAddPurchase_Click(object sender, RoutedEventArgs e)
        {
            var window = new PurchaseManage();
            if (window.ShowDialog() == true) LoadPurchases();
        }

        private void PurchaseCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is int id) OpenViewWindow(id);
        }

        private void ViewPurchase_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is int id) OpenViewWindow(id);
        }

        private void EditPurchase_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is int id) OpenEditWindow(id, false);
        }

        private void OpenViewWindow(int id) => OpenEditWindow(id, true);

        private void OpenEditWindow(int id, bool isViewOnly)
        {
            var purchase = db.EQUIPMENT_PURCHASES
                .Include(p => p.EQUIPMENT)
                .Include(p => p.SUPPLIERS)
                .Include(p => p.MASTERS)
                .Include(p => p.STATUS_PURCHASE)
                .FirstOrDefault(p => p.id == id);

            if (purchase != null)
            {
                var window = new PurchaseManage(purchase, isViewOnly);
                if (window.ShowDialog() == true) LoadPurchases();
            }
        }

        private void DeletePurchase_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not int id) return;

            var result = MessageBox.Show("Удалить эту закупку?", "Подтверждение",
                                       MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                var item = db.EQUIPMENT_PURCHASES.Find(id);
                if (item != null)
                {
                    db.EQUIPMENT_PURCHASES.Remove(item);
                    db.SaveChanges();
                    LoadPurchases();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
            }
        }

        public void Dispose() => db?.Dispose();
    }
}