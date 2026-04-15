using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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

        /// <summary>
        /// Загрузка оборудования с данными ТО
        /// </summary>
        private void LoadEquipment()
        {
            try
            {
                // Проверяем, что EquipmentList существует
                if (EquipmentList == null)
                {
                    System.Diagnostics.Debug.WriteLine("EquipmentList is null!");
                    return;
                }

                // Загружаем оборудование с навигационными свойствами
                var equipment = db.EQUIPMENT
                    .Include("WORKSHOPS")
                    .Include("MASTERS")
                    .Include("MASTERS.USERS")
                    .Include("STATUSASSETS")
                    .ToList();

                if (equipment == null || !equipment.Any())
                {
                    EquipmentList.ItemsSource = null;
                    UpdateStats(new List<EQUIPMENT>());
                    return;
                }

                // Фильтр по статусу
                if (CmbStatusFilter.SelectedIndex > 0 && CmbStatusFilter.SelectedItem is ComboBoxItem item)
                {
                    var filter = item.Content?.ToString();
                    equipment = filter switch
                    {
                        "✅ В эксплуатации" => equipment.Where(e => e.STATUSASSETS?.Status == "В эксплуатации").ToList(),
                        "⚠️ Требует ТО" => equipment.Where(e => IsMaintenanceDue(e)).ToList(),
                        "🔧 На обслуживании" => equipment.Where(e => e.STATUSASSETS?.Status == "На обслуживании").ToList(),
                        "❌ Неисправен" => equipment.Where(e => e.STATUSASSETS?.Status == "Неисправен").ToList(),
                        _ => equipment
                    };
                }

                // Преобразуем в ViewModel с безопасной обработкой null
                var viewModels = equipment.Select(e => new EquipmentViewModel1
                {
                    Id = e.ID,
                    AssetName = e.asset_id ?? "—",
                    EquipmentType = e.equipment_type ?? "—",
                    Workshop = e.WORKSHOPS?.name ?? "—",
                    Master = GetMasterName(e.MASTERS),
                    StatusName = e.STATUSASSETS?.Status ?? e.operational_status ?? "Неизвестно",
                    StatusColor = GetStatusColor(e.STATUSASSETS?.Status ?? e.operational_status),
                    StatusBorderColor = GetStatusBorderColor(e.STATUSASSETS?.Status ?? e.operational_status),
                    NextMaintenance = e.next_maintenance_date,
                    NextMaintenanceDisplay = GetNextMaintenanceDisplay(e.next_maintenance_date),
                    MaintenanceColor = GetMaintenanceColor(e.next_maintenance_date),
                    DaysToMaintenance = GetDaysToMaintenance(e.next_maintenance_date),
                    UrgencyIcon = GetUrgencyIcon(e.next_maintenance_date),
                    UrgencyColor = GetUrgencyColor(e.next_maintenance_date),
                    WorkHours = e.current_work_hours ?? 0,
                    MaxWorkHours = e.max_work_hours_before_maintenance ?? 0
                }).ToList();

                // Сортировка по приоритету
                viewModels = viewModels.OrderBy(vm => GetMaintenancePriority(vm.NextMaintenance, vm.StatusName)).ToList();

                EquipmentList.ItemsSource = viewModels;
                UpdateStats(equipment);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}\n\n{ex.StackTrace}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Безопасное получение имени мастера
        /// </summary>
        private string GetMasterName(MASTERS master)
        {
            try
            {
                if (master == null) return "Не назначен";
                if (master.USERS == null) return $"Мастер ID: {master.id}";

                var userName = master.USERS.full_name;
                if (string.IsNullOrWhiteSpace(userName))
                    userName = master.USERS.username;

                return string.IsNullOrWhiteSpace(userName) ? $"Мастер ID: {master.id}" : userName;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting master name: {ex.Message}");
                return "Ошибка загрузки";
            }
        }

        /// <summary>
        /// Проверка, требуется ли ТО
        /// </summary>
        private bool IsMaintenanceDue(EQUIPMENT e)
        {
            if (e == null || !e.next_maintenance_date.HasValue) return false;
            var days = (e.next_maintenance_date.Value - DateTime.Now).Days;
            return days <= 14 && days >= 0;
        }

        /// <summary>
        /// Получение приоритета для сортировки
        /// </summary>
        private int GetMaintenancePriority(DateTime? nextMaint, string status)
        {
            if (status == "Неисправен") return 0;
            if (status == "Неизвестно") return 4;
            if (nextMaint.HasValue && (nextMaint.Value - DateTime.Now).Days < 0) return 1;
            if (nextMaint.HasValue && (nextMaint.Value - DateTime.Now).Days <= 7) return 2;
            return 3;
        }

        /// <summary>
        /// Обновление статистики
        /// </summary>
        private void UpdateStats(List<EQUIPMENT> equipment)
        {
            try
            {
                if (TotalCount != null) TotalCount.Text = equipment?.Count.ToString() ?? "0";
                if (ActiveCount != null) ActiveCount.Text = equipment?.Count(e => e.STATUSASSETS?.Status == "В эксплуатации" || e.operational_status == "В эксплуатации").ToString() ?? "0";
                if (DueCount != null) DueCount.Text = equipment?.Count(e => IsMaintenanceDue(e)).ToString() ?? "0";
                if (BrokenCount != null) BrokenCount.Text = equipment?.Count(e => e.STATUSASSETS?.Status == "Неисправен" || e.operational_status == "Неисправен").ToString() ?? "0";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating stats: {ex.Message}");
            }
        }

        // 🔹 Вспомогательные методы для отображения
        private string GetStatusColor(string status)
        {
            if (string.IsNullOrEmpty(status)) return "#7F8C8D";
            return status.Trim().ToLower() switch
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
            if (string.IsNullOrEmpty(status)) return "Transparent";
            return status.Trim().ToLower() switch
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

        /// <summary>
        /// Загрузка истории ТО для выбранного оборудования
        /// </summary>
        private void LoadMaintenanceHistory(int equipmentId)
        {
            try
            {
                if (MaintenanceHistoryList == null) return;

                var history = db.MAINTENANCE
                    .Where(m => m.equipment_id == equipmentId)
                    .OrderByDescending(m => m.maintenance_date)
                    .ToList();

                var historyViewModels = history.Select(m => new MaintenanceViewModel
                {
                    Id = m.id,
                    Date = m.maintenance_date,
                    DateDisplay = m.maintenance_date?.ToString("dd.MM.yyyy") ?? "—",
                    Type = m.maintenance_type ?? "—",
                    Description = m.description ?? "—",
                    Parts = string.IsNullOrEmpty(m.parts_replaced) ? "—" : $"Заменено: {m.parts_replaced}",
                    CostDisplay = m.cost.HasValue ? $"{m.cost:N2} руб." : "—",
                    DowntimeDisplay = m.downtime_hours.HasValue ? $"{m.downtime_hours} ч." : "—",
                    NextMaintenance = m.next_maintenance_date.HasValue
                        ? $"След. ТО: {m.next_maintenance_date:dd.MM.yyyy}"
                        : "Не запланировано",
                    NextMaintenanceColor = GetMaintenanceColor(m.next_maintenance_date)
                }).ToList();

                MaintenanceHistoryList.ItemsSource = historyViewModels;

                if (TxtNoHistory != null)
                    TxtNoHistory.Visibility = history.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки истории: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Проведение ТО (открытие диалогового окна)
        /// </summary>
        private void PerformMaintenance_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is int equipmentId)
            {
                var equipment = db.EQUIPMENT.FirstOrDefault(eq => eq.ID == equipmentId);
                if (equipment != null)
                {
                    var dialog = new MaintenanceDialog(equipmentId);
                    dialog.Owner = Window.GetWindow(this);
                    if (dialog.ShowDialog() == true)
                    {
                        LoadEquipment();
                        if (_selectedEquipmentId == equipmentId)
                        {
                            LoadMaintenanceHistory(equipmentId);
                        }
                    }
                }
            }
        }

        // 🔹 Обработчики событий
        private void CmbStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => LoadEquipment();

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
            => LoadEquipment();

        private void EquipmentCard_Click(object sender, MouseButtonEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is int id)
            {
                _selectedEquipmentId = id;

                try
                {
                    var eq = db.EQUIPMENT
                        .Include("STATUSASSETS")
                        .Include("WORKSHOPS")
                        .FirstOrDefault(e => e.ID == id);

                    if (eq != null)
                    {
                        if (TxtSelectedAsset != null) TxtSelectedAsset.Text = eq.asset_id ?? "—";
                        if (TxtSelectedInfo != null) TxtSelectedInfo.Text = $"{eq.equipment_type ?? "—"} • {eq.WORKSHOPS?.name ?? "—"}";
                        if (TxtCurrentStatus != null) TxtCurrentStatus.Text = eq.STATUSASSETS?.Status ?? eq.operational_status ?? "—";
                        if (StatusBadge != null)
                        {
                            StatusBadge.Background = new SolidColorBrush(
                                (Color)ColorConverter.ConvertFromString(GetStatusColor(eq.STATUSASSETS?.Status ?? eq.operational_status)));
                        }
                    }

                    LoadMaintenanceHistory(id);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading equipment details: {ex.Message}");
                }
            }
        }

        public void Dispose() => db?.Dispose();
    }

    /// <summary>
    /// ViewModel для оборудования
    /// </summary>
    public class EquipmentViewModel1
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
        public double WorkHours { get; set; }
        public double MaxWorkHours { get; set; }
        public double WorkHoursPercent => MaxWorkHours > 0 ? (WorkHours / MaxWorkHours) * 100 : 0;
    }

    /// <summary>
    /// ViewModel для истории ТО
    /// </summary>
    public class MaintenanceViewModel
    {
        public int Id { get; set; }
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
}