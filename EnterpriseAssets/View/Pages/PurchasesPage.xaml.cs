using EnterpriseAssets.Model.DataBase;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using EnterpriseAssets.Model;

namespace EnterpriseAssets.View.Pages
{
    public partial class PurchasesPage : Page
    {
        private DB_AssetManage _context;
        public int CurrentUserId { get; set; }
        private List<PurchaseDisplay> _allPurchases;
        private int? _selectedSupplierId;
        private int? _selectedStatusId;

        public PurchasesPage()
        {
            InitializeComponent();
            _context = new DB_AssetManage();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSuppliers();
            LoadPurchases();
        }

        private void LoadSuppliers()
        {
            try
            {
                var suppliers = _context.SUPPLIERS
                    .Where(s => s.is_active == true || s.is_active == null)
                    .OrderBy(s => s.name)
                    .ToList();

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

        private void LoadPurchases()
        {
            try
            {
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

                var data = query.ToList();

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

        private string FormatDelivery(DateTime? expected, DateTime? actual)
        {
            if (actual.HasValue)
                return $"Доставлено: {actual.Value:dd.MM.yyyy}";
            if (expected.HasValue)
                return $"Ожидается: {expected.Value:dd.MM.yyyy}";
            return "Срок не указан";
        }

        private Brush GetDeliveryColor(DateTime? expected, DateTime? actual)
        {
            if (actual.HasValue)
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));

            if (expected.HasValue)
            {
                if (expected.Value < DateTime.Today)
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
                else
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F39C12"));
            }

            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#95A5A6"));
        }

        private Brush GetStatusColor(int? statusId)
        {
            return statusId switch
            {
                1 => new SolidColorBrush(Colors.Gray),
                2 => new SolidColorBrush(Colors.Orange),
                3 => new SolidColorBrush(Colors.LightBlue),
                4 => new SolidColorBrush(Colors.Purple),
                5 => new SolidColorBrush(Colors.Green),
                6 => new SolidColorBrush(Colors.Red),
                _ => new SolidColorBrush(Colors.LightGray)
            };
        }

        private void ApplyFilters()
        {
            if (_allPurchases == null) return;

            var filtered = _allPurchases.AsEnumerable();

            string searchText = TxtSearch.Text?.Trim().ToLower();
            if (!string.IsNullOrEmpty(searchText))
            {
                filtered = filtered.Where(p =>
                    (p.PurchaseNumber?.ToLower().Contains(searchText) ?? false) ||
                    (p.EquipmentName?.ToLower().Contains(searchText) ?? false) ||
                    (p.SupplierName?.ToLower().Contains(searchText) ?? false));
            }

            if (_selectedStatusId.HasValue)
            {
                filtered = filtered.Where(p => p.StatusId == _selectedStatusId.Value);
            }

            if (_selectedSupplierId.HasValue)
            {
                filtered = filtered.Where(p => p.SupplierId == _selectedSupplierId.Value);
            }

            var resultList = filtered.ToList();
            PurchasesList.ItemsSource = resultList;

            UpdateStatistics(resultList);
        }

        private void UpdateStatistics(List<PurchaseDisplay> list)
        {
            TotalCount.Text = list.Count.ToString();
            decimal totalSum = list.Sum(p => p.TotalCost);
            TotalAmount.Text = totalSum.ToString("N2") + " ₽";

            int pending = list.Count(p => p.StatusId.HasValue &&
                                          p.StatusId.Value != 5 &&
                                          p.StatusId.Value != 6);
            PendingCount.Text = pending.ToString();
        }

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
                _selectedSupplierId = item.Tag as int?;
                ApplyFilters();
            }
        }

        private void BtnAddPurchase_Click(object sender, RoutedEventArgs e)
        {
            var win = new PurchaseManage(CurrentUserId);
            if (win.ShowDialog() == true)
            {
                LoadPurchases();
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadPurchases();
        }

        private void PurchaseCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is int purchaseId)
            {
                ViewPurchase(purchaseId);
            }
        }

        private void ViewPurchase_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int purchaseId)
            {
                ViewPurchase(purchaseId);
            }
        }

        private void ViewPurchase(int purchaseId)
        {
            MessageBox.Show($"Просмотр закупки ID: {purchaseId}", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void EditPurchase_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int purchaseId)
            {
                var win = new PurchaseManage(purchaseId, CurrentUserId);
                if (win.ShowDialog() == true)
                {
                    LoadPurchases();
                }
            }
        }

        private void DeletePurchase_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int purchaseId)
            {
                var result = MessageBox.Show("Вы действительно хотите удалить эту закупку?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var purchase = _context.EQUIPMENT_PURCHASES.Find(purchaseId);
                        if (purchase != null)
                        {
                            _context.EQUIPMENT_PURCHASES.Remove(purchase);
                            _context.SaveChanges();
                            LoadPurchases();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        // 🔥 НОВЫЕ МЕТОДЫ ДЛЯ ДОКУМЕНТОВ И ОТЧЁТОВ

        private void Documents_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int purchaseId)
            {
                var dialog = new PurchaseDocumentsDialog(purchaseId, CurrentUserId, _context);
                dialog.Owner = Window.GetWindow(this);
                if (dialog.ShowDialog() == true)
                {
                    var purchase = _context.EQUIPMENT_PURCHASES.Find(purchaseId);
                    if (purchase != null)
                    {
                        AutoUpdateStatus(purchaseId);
                        _context.SaveChanges();
                    }
                    LoadPurchases();
                }
            }
        }

        private void PrintReport_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int purchaseId)
            {
                GeneratePurchaseReport(purchaseId);
            }
        }

        private void BtnExportReport_Click(object sender, RoutedEventArgs e)
        {
            GenerateSummaryReport();
        }

        private void AutoUpdateStatus(int purchaseId)
        {
            var purchase = _context.EQUIPMENT_PURCHASES.Find(purchaseId);
            if (purchase == null) return;

            var documents = _context.REPORTS
                .Where(r => r.report_data.Contains($"PURCHASE_ID:{purchaseId}") ||
                            (r.file_path != null && r.file_path.Contains($"Purchase_{purchaseId}")))
                .ToList();

            bool hasDeliveryNote = documents.Any(d => d.report_type == "DeliveryNote");
            bool hasAct = documents.Any(d => d.report_type == "Act");

            if (hasDeliveryNote || hasAct)
            {
                purchase.status = 5;
                purchase.actual_delivery = DateTime.Now;
            }
            else if (documents.Any() && purchase.status < 4)
            {
                purchase.status = 4;
            }
        }

        private void GeneratePurchaseReport(int purchaseId)
        {
            try
            {
                var purchase = _context.EQUIPMENT_PURCHASES
                    .Include("EQUIPMENT")
                    .Include("SUPPLIERS")
                    .Include("MASTERS")
                    .Include("MASTERS.USERS")
                    .FirstOrDefault(p => p.id == purchaseId);

                if (purchase == null) return;

                string reportText = $@"
ОТЧЁТ ПО ЗАКУПКЕ №{purchase.purchase_number}
==========================================

Дата заказа: {purchase.order_date:dd.MM.yyyy}
Ожидаемая доставка: {purchase.expected_delivery:dd.MM.yyyy}
Фактическая доставка: {(purchase.actual_delivery.HasValue ? purchase.actual_delivery.Value.ToString("dd.MM.yyyy") : "Не доставлено")}

ОБОРУДОВАНИЕ:
{(purchase.EQUIPMENT != null ? $"{purchase.EQUIPMENT.equipment_type} ({purchase.EQUIPMENT.manufacturer})" : "Не указано")}

ПОСТАВЩИК:
{(purchase.SUPPLIERS != null ? purchase.SUPPLIERS.name : "Не указан")}

КОЛИЧЕСТВО И СТОИМОСТЬ:
Количество: {purchase.quantity} ед.
Цена за единицу: {purchase.unit_price:N2} ₽
Общая стоимость: {purchase.total_cost:N2} ₽

МЕНЕДЖЕР:
{(purchase.MASTERS != null && purchase.MASTERS.USERS != null ? purchase.MASTERS.USERS.full_name : "Не назначен")}

СТАТУС:
{(purchase.STATUS_PURCHASE != null ? purchase.STATUS_PURCHASE.Status : "Неизвестно")}

ПРИМЕЧАНИЯ:
{(string.IsNullOrEmpty(purchase.notes) ? "—" : purchase.notes)}

==========================================
Сформирован: {DateTime.Now:dd.MM.yyyy HH:mm}
";

                string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                string fileName = $"Purchase_{purchaseId}_{purchase.purchase_number}_{DateTime.Now:yyyyMMdd}.txt";
                string filePath = Path.Combine(folderPath, fileName);
                File.WriteAllText(filePath, reportText);

                var report = new REPORTS
                {
                    report_type = "PurchaseReport",
                    period_start = purchase.order_date,
                    period_end = purchase.actual_delivery ?? purchase.expected_delivery,
                    generated_by = purchase.purchase_manager_id,
                    report_data = $"PURCHASE_ID:{purchaseId}|NUMBER:{purchase.purchase_number}",
                    file_path = filePath,
                    created_at = DateTime.Now
                };

                _context.REPORTS.Add(report);
                _context.SaveChanges();

                MessageBox.Show("Отчёт успешно сохранён!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                Process.Start(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка генерации отчёта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateSummaryReport()
        {
            try
            {
                var purchases = _context.EQUIPMENT_PURCHASES
                    .Include("EQUIPMENT")
                    .Include("SUPPLIERS")
                    .Include("STATUS_PURCHASE")
                    .ToList();

                string reportText = $@"
ОБЩИЙ ОТЧЁТ ПО ЗАКУПКАМ
==========================================
Период: все время
Всего закупок: {purchases.Count}

";

                decimal totalAmount = purchases.Sum(p => p.total_cost ?? 0);
                int delivered = purchases.Count(p => p.status == 5);
                int pending = purchases.Count(p => p.status != 5 && p.status != 6);

                reportText += $@"
СТАТИСТИКА:
Общая сумма: {totalAmount:N2} ₽
Доставлено: {delivered}
В ожидании: {pending}

";

                var bySupplier = purchases
                    .Where(p => p.SUPPLIERS != null)
                    .GroupBy(p => p.SUPPLIERS.name)
                    .Select(g => new
                    {
                        Supplier = g.Key,
                        Count = g.Count(),
                        Total = g.Sum(p => p.total_cost ?? 0)
                    })
                    .OrderByDescending(x => x.Total);

                reportText += "\nПО ПОСТАВЩИКАМ:\n";
                foreach (var s in bySupplier)
                {
                    reportText += $"{s.Supplier}: {s.Count} закупок, {s.Total:N2} ₽\n";
                }

                string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                string fileName = $"Purchases_Summary_{DateTime.Now:yyyyMMdd}.txt";
                string filePath = Path.Combine(folderPath, fileName);
                File.WriteAllText(filePath, reportText);

                var report = new REPORTS
                {
                    report_type = "PurchaseSummaryReport",
                    period_start = purchases.Min(p => p.order_date),
                    period_end = purchases.Max(p => p.actual_delivery ?? p.expected_delivery),
                    generated_by = Session.CurrentMasterId,
                    report_data = "PURCHASE_SUMMARY",
                    file_path = filePath,
                    created_at = DateTime.Now
                };

                _context.REPORTS.Add(report);
                _context.SaveChanges();

                MessageBox.Show("Общий отчёт успешно сохранён!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                Process.Start(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка генерации отчёта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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
            public int? StatusId { get; set; }
            public int? SupplierId { get; set; }
            public decimal TotalCost { get; set; }
        }
    }
}