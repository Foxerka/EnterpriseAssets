using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Data.Entity;
using EnterpriseAssets.Model.DataBase;

namespace EnterpriseAssets.View.Pages
{
    public partial class MaintenancePage : Page
    {
        private DB_AssetManage db = new DB_AssetManage();
        private int? _selectedEquipmentId;

        public MaintenancePage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadEquipment();
        }

        // 🔹 Загрузка оборудования с данными ТО
        private void LoadEquipment()
        {
            try
            {
                // Загружаем оборудование с навигационными свойствами
                var equipment = db.EQUIPMENT
                    .Include(e => e.WORKSHOPS)
                    .Include(e => e.MASTERS)
                    .Include(e => e.MASTERS.USERS)
                    .Include(e => e.STATUSASSETS)
                    .Include(e => e.WORK_ACTS)
                    .ToList();

                // Фильтр по статусу
                if (CmbStatusFilter.SelectedIndex > 0 && CmbStatusFilter.SelectedItem is ComboBoxItem item)
                {
                    var filter = item.Content?.ToString();
                    equipment = filter switch
                    {
                        "✅ В работе" => equipment.Where(e => e.STATUSASSETS?.Status == "В работе").ToList(),
                        "⚠️ Требует ТО" => equipment.Where(e => IsMaintenanceDue(e)).ToList(),
                        "🔧 На обслуживании" => equipment.Where(e => e.STATUSASSETS?.Status == "На обслуживании").ToList(),
                        "❌ Неисправен" => equipment.Where(e => e.STATUSASSETS?.Status == "Неисправен").ToList(),
                        _ => equipment
                    };
                }

                // Преобразуем в ViewModel
                EquipmentList.ItemsSource = equipment.Select(e => {
                    // Отладка – посмотрим, что пришло
                    System.Diagnostics.Debug.WriteLine($"Processing equipment ID: {e.ID}");

                    // Безопасно получаем имя мастера
                    string masterName = "";
                    try
                    {
                        if (e.MASTERS != null)
                        {
                            if (e.MASTERS.USERS != null)
                            {
                                masterName = e.MASTERS.USERS.full_name ?? e.MASTERS.USERS.username ?? "";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error getting master name: {ex.Message}");
                    }

                    // Безопасно получаем статус
                    string status = e.STATUSASSETS?.Status ?? "";

                    return new EquipmentViewModel
                    {
                        Id = e.ID,
                        AssetName = e.asset_id ?? "",
                        EquipmentType = e.equipment_type ?? "",
                        Workshop = e.WORKSHOPS?.name ?? "",
                        Master = masterName,
                        StatusName = status,
                        StatusColor = GetStatusColor(status),
                        StatusBorderColor = GetStatusBorderColor(status),
                        NextMaintenance = e.next_maintenance_date,
                        NextMaintenanceDisplay = GetNextMaintenanceDisplay(e.next_maintenance_date),
                        MaintenanceColor = GetMaintenanceColor(e.next_maintenance_date),
                        DaysToMaintenance = GetDaysToMaintenance(e.next_maintenance_date),
                        UrgencyIcon = GetUrgencyIcon(e.next_maintenance_date),
                        UrgencyColor = GetUrgencyColor(e.next_maintenance_date)
                    };
                })
 .OrderBy(e => GetMaintenancePriority(e.NextMaintenance, e.StatusName))
 .ToList();

                UpdateStats(equipment);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
            }
        }

        // 🔹 ViewModel для карточки оборудования
        public class EquipmentViewModel
        {
            public int Id { get; set; }
            public string AssetName { get; set; }
            public string EquipmentType { get; set; }
            public string Workshop { get; set; }
            public string Master { get; set; }
            public string StatusName { get; set; }
            public string StatusColor { get; set; }
            public string StatusBorderColor { get; set; }
            public DateTime? NextMaintenance { get; set; }
            public string NextMaintenanceDisplay { get; set; }
            public string MaintenanceColor { get; set; }
            public string DaysToMaintenance { get; set; }
            public string UrgencyIcon { get; set; }
            public string UrgencyColor { get; set; }
        }

        // 🔹 ViewModel для записи ТО
        public class MaintenanceViewModel
        {
            public DateTime? Date { get; set; }
            public string DateDisplay { get; set; }
            public string Type { get; set; }
            public string Description { get; set; }
            public string Parts { get; set; }
            public string CostDisplay { get; set; }
            public string DowntimeDisplay { get; set; }
            public string NextMaintenance { get; set; }
            public string NextMaintenanceColor { get; set; }
        }

        // 🔹 Вспомогательные методы
        private string GetStatusColor(string status)
        {
            return status?.Trim().ToLower() switch
            {
                "в эксплуатации" => "#27AE60",
                "на обслуживании" => "#F39C12",
                "неисправен" => "#E74C3C",
                "списан" => "#95A5A6",
                _ => "#7F8C8D"
            };
        }

        private string GetStatusBorderColor(string status)
        {
            return status?.Trim().ToLower() switch
            {
                "в эксплуатации" => "#27AE60",
                "на обслуживании" => "#F39C12",
                "неисправен" => "#E74C3C",
                _ => "Transparent"
            };
        }

        private string GetNextMaintenanceDisplay(DateTime? date)
        {
            if (!date.HasValue) return "Не запланировано";
            return date.Value.ToString("dd.MM.yyyy");
        }

        private string GetMaintenanceColor(DateTime? date)
        {
            if (!date.HasValue) return "#7F8C8D";
            var days = (date.Value - DateTime.Now).Days;
            return days < 0 ? "#E74C3C" : days <= 7 ? "#E67E22" : "#27AE60";
        }

        private string GetDaysToMaintenance(DateTime? date)
        {
            if (!date.HasValue) return "";
            var days = (date.Value - DateTime.Now).Days;
            return days < 0 ? $"Просрочено: {Math.Abs(days)} дн." : days <= 7 ? $"⚠️ {days} дн." : $"{days} дн.";
        }

        private string GetUrgencyIcon(DateTime? date)
        {
            if (!date.HasValue) return "○";
            var days = (date.Value - DateTime.Now).Days;
            return days < 0 ? "🔴" : days <= 7 ? "🟡" : "🟢";
        }

        private string GetUrgencyColor(DateTime? date)
        {
            if (!date.HasValue) return "#BDC3C7";
            var days = (date.Value - DateTime.Now).Days;
            return days < 0 ? "#E74C3C" : days <= 7 ? "#F39C12" : "#27AE60";
        }

        private bool IsMaintenanceDue(EQUIPMENT e)
        {
            if (!e.next_maintenance_date.HasValue) return false;
            var days = (e.next_maintenance_date.Value - DateTime.Now).Days;
            return days <= 14 && days >= 0;
        }

        private int GetMaintenancePriority(DateTime? nextMaint, string status)
        {
            // Приоритет сортировки: неисправные → просроченные → скоро ТО → остальные
            if (status == "Неисправен") return 0;
            if (nextMaint.HasValue && (nextMaint.Value - DateTime.Now).Days < 0) return 1;
            if (nextMaint.HasValue && (nextMaint.Value - DateTime.Now).Days <= 7) return 2;
            return 3;
        }

        private void UpdateStats(List<EQUIPMENT> equipment)
        {
            TotalCount.Text = equipment.Count.ToString();
            ActiveCount.Text = equipment.Count(e => e.STATUSASSETS?.Status == "В эксплуатации").ToString();
            DueCount.Text = equipment.Count(e => IsMaintenanceDue(e)).ToString();
            BrokenCount.Text = equipment.Count(e => e.STATUSASSETS?.Status == "Неисправен").ToString();
        }

        // 🔹 Загрузка истории ТО для выбранного оборудования
        private void LoadMaintenanceHistory(int equipmentId)
        {
            try
            {
                var history = db.MAINTENANCE
                    .Include(m => m.STATUSASSETS)
                    .Include(m => m.MASTERS)
                    .Include(m => m.MASTERS.USERS)
                    .Where(m => m.equipment_id == equipmentId)
                    .OrderByDescending(m => m.maintenance_date)
                    .ToList();

                MaintenanceHistoryList.ItemsSource = history.Select(m => new MaintenanceViewModel
                {
                    Date = m.maintenance_date,
                    DateDisplay = m.maintenance_date?.ToString("dd.MM.yyyy") ?? "—",
                    Type = m.maintenance_type ?? "—",
                    Description = m.description ?? "—",
                    Parts = string.IsNullOrEmpty(m.parts_replaced) ? "—" : $"Заменено: {m.parts_replaced}",
                    CostDisplay = m.cost.HasValue ? $"{m.cost:C}" : "—",
                    DowntimeDisplay = m.downtime_hours.HasValue ? $"{m.downtime_hours} ч." : "—",
                    NextMaintenance = m.next_maintenance_date.HasValue
                        ? $"След. ТО: {m.next_maintenance_date:dd.MM.yyyy}"
                        : "Не запланировано",
                    NextMaintenanceColor = GetMaintenanceColor(m.next_maintenance_date)
                }).ToList();

                TxtNoHistory.Visibility = history.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки истории: {ex.Message}", "Ошибка");
            }
        }

        // 🔹 Обработчики событий
        private void CmbStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => LoadEquipment();

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
            => LoadEquipment();

        private void EquipmentCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is int id)
            {
                _selectedEquipmentId = id;

                // Загружаем данные оборудования
                var eq = db.EQUIPMENT
                    .Include(e => e.STATUSASSETS)
                    .Include(e => e.WORKSHOPS)
                    .FirstOrDefault(e => e.ID == id);

                if (eq != null)
                {
                    TxtSelectedAsset.Text = eq.asset_id ?? "—";
                    TxtSelectedInfo.Text = $"{eq.equipment_type} • {eq.WORKSHOPS?.name ?? "—"}";
                    TxtCurrentStatus.Text = eq.STATUSASSETS?.Status ?? "—";
                    StatusBadge.Background = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(
                            GetStatusColor(eq.STATUSASSETS?.Status)));
                }

                // Загружаем историю
                LoadMaintenanceHistory(id);
            }
        }

        // 🔹 Освобождение ресурсов
        public void Dispose() => db?.Dispose();
    }
}