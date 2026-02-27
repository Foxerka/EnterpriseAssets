using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Data.Entity;
using EnterpriseAssets.Model.DataBase;

namespace EnterpriseAssets.View.Pages
{
    public partial class EquipmentPage : Page
    {
        private DB_AssetManage db = new DB_AssetManage();

        public EquipmentPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadFilters();
            LoadEquipment();
        }
        private void CmbStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadEquipment();
        }
        // 🔹 Загрузка фильтров (цеха)
        private void LoadFilters()
        {
            try
            {
                CmbWorkshopFilter.Items.Clear();
                CmbWorkshopFilter.Items.Add(new ComboBoxItem { Content = "Все цеха", Tag = (int?)null });

                var workshops = db.WORKSHOPS.OrderBy(w => w.name).ToList();
                foreach (var w in workshops)
                {
                    CmbWorkshopFilter.Items.Add(new ComboBoxItem
                    {
                        Content = w.name,
                        Tag = w.id
                    });
                }
                CmbWorkshopFilter.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ LoadFilters: {ex.Message}");
            }
        }

        // 🔹 Загрузка оборудования
        private void LoadEquipment()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔹 LoadEquipment started");

                var query = db.EQUIPMENT
                    .Include(e => e.WORKSHOPS)
                    .Include(e => e.MASTERS)
                    .Include(e => e.MASTERS.USERS)
                    .Include(e => e.STATUSASSETS)
                    .AsQueryable();

                // Поиск
                if (!string.IsNullOrWhiteSpace(TxtSearch?.Text))
                {
                    var search = TxtSearch.Text.ToLower();
                    query = query.Where(e =>
                        (!string.IsNullOrEmpty(e.asset_id) && e.asset_id.ToLower().Contains(search)) ||
                        (!string.IsNullOrEmpty(e.equipment_type) && e.equipment_type.ToLower().Contains(search)) ||
                        (!string.IsNullOrEmpty(e.manufacturer) && e.manufacturer.ToLower().Contains(search)) ||
                        (!string.IsNullOrEmpty(e.notes) && e.notes.ToLower().Contains(search)));
                }

                // Фильтр по цеху
                if (CmbWorkshopFilter != null && CmbWorkshopFilter.SelectedIndex > 0)
                {
                    if (CmbWorkshopFilter.SelectedItem is ComboBoxItem wsItem)
                    {
                        if (wsItem.Tag is int workshopId)
                        {
                            query = query.Where(e => e.Workshop_id == workshopId);
                        }
                    }
                }

                var equipment = query.OrderByDescending(e => e.installation_date).ToList();
                System.Diagnostics.Debug.WriteLine($"🔹 Equipment loaded: {equipment.Count} items");

                // 🔹 Формируем ViewModel ПОЭЛЕМЕНТНО (безопасно)
                var viewModelList = new List<EquipmentViewModel>();

                foreach (var e in equipment)
                {
                    try
                    {
                        var vm = new EquipmentViewModel
                        {
                            Id = e.ID,
                            AssetId = !string.IsNullOrEmpty(e.asset_id) ? e.asset_id : "—",
                            AssetName = GetAssetName(e.asset_id),
                            EquipmentType = !string.IsNullOrEmpty(e.equipment_type) ? e.equipment_type : "—",
                            Manufacturer = !string.IsNullOrEmpty(e.manufacturer) ? e.manufacturer : "—",
                        };

                        // Цех
                        if (e.WORKSHOPS != null && !string.IsNullOrEmpty(e.WORKSHOPS.name))
                        {
                            vm.WorkshopName = e.WORKSHOPS.name;
                        }
                        else
                        {
                            vm.WorkshopName = "Не назначен";
                        }

                        // Мастер
                        if (e.MASTERS != null && e.MASTERS.USERS != null)
                        {
                            var user = e.MASTERS.USERS;
                            vm.MasterName = !string.IsNullOrEmpty(user.full_name)
                                ? user.full_name
                                : (!string.IsNullOrEmpty(user.username) ? user.username : "Не назначен");
                        }
                        else
                        {
                            vm.MasterName = "Не назначен";
                        }

                        // Статус
                        if (e.STATUSASSETS != null && !string.IsNullOrEmpty(e.STATUSASSETS.Status))
                        {
                            vm.StatusName = e.STATUSASSETS.Status;
                            vm.StatusColor = GetStatusColor(e.STATUSASSETS.Status);
                        }
                        else
                        {
                            vm.StatusName = "—";
                            vm.StatusColor = "#7F8C8D";
                        }

                        // Даты и наработка
                        vm.InstallationDate = e.installation_date;
                        vm.WarrantyMonths = e.warranty_period_months;
                        vm.WarrantyDisplay = GetWarrantyDisplay(e.installation_date, e.warranty_period_months);
                        vm.WarrantyColor = GetWarrantyColor(e.installation_date, e.warranty_period_months);

                        vm.LastMaintenance = e.last_maintenance_date;
                        vm.NextMaintenance = e.next_maintenance_date;
                        vm.NextMaintenanceDisplay = GetNextMaintenanceDisplay(e.next_maintenance_date);
                        vm.MaintenanceColor = GetMaintenanceColor(e.next_maintenance_date);

                        vm.CurrentHours = e.current_work_hours ?? 0;
                        vm.MaxHours = e.max_work_hours_before_maintenance;
                        vm.WorkHoursDisplay = $"{vm.CurrentHours} ч.";
                        vm.MaxHoursDisplay = e.max_work_hours_before_maintenance.HasValue ? $"{e.max_work_hours_before_maintenance} ч." : "∞";
                        vm.WorkHoursPercent = CalculateWorkHoursPercent(e.current_work_hours, e.max_work_hours_before_maintenance);

                        vm.Notes = e.notes;

                        viewModelList.Add(vm);
                    }
                    catch (Exception itemEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Error processing Equipment ID={e.ID}: {itemEx.Message}");
                        // Пропускаем проблемную запись
                    }
                }

                if (EquipmentList == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ EquipmentList is NULL! Проверьте XAML");
                    MessageBox.Show("EquipmentList не найден! Проверьте XAML файл.", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                EquipmentList.ItemsSource = viewModelList;
                System.Diagnostics.Debug.WriteLine($"✅ ViewModel created: {viewModelList.Count} items");

                UpdateStats(equipment);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ LoadEquipment ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                MessageBox.Show($"Ошибка загрузки: {ex.Message}\n\n{ex.StackTrace}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 🔹 ViewModel для карточки оборудования
        public class EquipmentViewModel
        {
            public int Id { get; set; }
            public string AssetId { get; set; }
            public string AssetName { get; set; }
            public string EquipmentType { get; set; }
            public string Manufacturer { get; set; }
            public string WorkshopName { get; set; }
            public string MasterName { get; set; }
            public string StatusName { get; set; }
            public string StatusColor { get; set; }

            public DateTime? InstallationDate { get; set; }
            public int? WarrantyMonths { get; set; }
            public string WarrantyDisplay { get; set; }
            public string WarrantyColor { get; set; }

            public DateTime? LastMaintenance { get; set; }
            public DateTime? NextMaintenance { get; set; }
            public string NextMaintenanceDisplay { get; set; }
            public string MaintenanceColor { get; set; }

            public int CurrentHours { get; set; }
            public int? MaxHours { get; set; }
            public string WorkHoursDisplay { get; set; }
            public string MaxHoursDisplay { get; set; }
            public double WorkHoursPercent { get; set; }

            public string Notes { get; set; }
        }

        // 🔹 Вспомогательные методы
        private string GetAssetName(string assetId)
        {
            if (string.IsNullOrEmpty(assetId)) return "—";
            var asset = db.PRODUCTION_ASSETS.FirstOrDefault(a => a.name == assetId || a.serial_number == assetId);
            return asset?.name ?? assetId;
        }

        private string GetStatusColor(string statusName)
        {
            if (string.IsNullOrEmpty(statusName))
                return "#7F8C8D";  // Серый для NULL

            return statusName?.Trim().ToLower() switch
            {
                "в эксплуатации" => "#27AE60",
                "на обслуживании" => "#F39C12",
                "неисправен" => "#E74C3C",
                "списан" => "#95A5A6",
                _ => "#7F8C8D"
            };
        }

        private string GetWarrantyDisplay(DateTime? installDate, int? warrantyMonths)
        {
            if (!installDate.HasValue || !warrantyMonths.HasValue) return "—";
            var warrantyEnd = installDate.Value.AddMonths(warrantyMonths.Value);
            var daysLeft = (warrantyEnd - DateTime.Now).Days;

            if (daysLeft < 0) return $"❌ Истекла ({Math.Abs(daysLeft)} дн. назад)";
            if (daysLeft <= 30) return $"⚠️ {daysLeft} дн. до конца";
            return $"✅ {daysLeft} дн. осталось";
        }

        private string GetWarrantyColor(DateTime? installDate, int? warrantyMonths)
        {
            if (!installDate.HasValue || !warrantyMonths.HasValue) return "#7F8C8D";
            var warrantyEnd = installDate.Value.AddMonths(warrantyMonths.Value);
            var daysLeft = (warrantyEnd - DateTime.Now).Days;

            if (daysLeft < 0) return "#E74C3C";
            if (daysLeft <= 30) return "#F39C12";
            return "#27AE60";
        }

        private string GetNextMaintenanceDisplay(DateTime? nextMaintenance)
        {
            if (!nextMaintenance.HasValue) return "Не запланировано";
            var daysLeft = (nextMaintenance.Value - DateTime.Now).Days;

            if (daysLeft < 0) return $"❌ Просрочено на {Math.Abs(daysLeft)} дн.";
            if (daysLeft <= 7) return $"⚠️ Через {daysLeft} дн.";
            return $"📅 {nextMaintenance.Value:dd.MM.yyyy}";
        }

        private string GetMaintenanceColor(DateTime? nextMaintenance)
        {
            if (!nextMaintenance.HasValue) return "#7F8C8D";
            var daysLeft = (nextMaintenance.Value - DateTime.Now).Days;

            if (daysLeft < 0) return "#E74C3C";
            if (daysLeft <= 7) return "#E67E22";
            return "#27AE60";
        }

        private double CalculateWorkHoursPercent(int? current, int? max)
        {
            if (!current.HasValue || !max.HasValue || max.Value == 0) return 0;
            return Math.Min(100, (double)current.Value / max.Value * 100);
        }

        private void UpdateStats(List<EQUIPMENT> equipment)
        {
            TotalCount.Text = equipment.Count.ToString();
            ActiveCount.Text = equipment.Count(e => e.STATUSASSETS?.Status == "В эксплуатации").ToString();
            MaintenanceCount.Text = equipment.Count(e =>
            {
                if (!e.next_maintenance_date.HasValue) return false;
                var daysLeft = (e.next_maintenance_date.Value - DateTime.Now).Days;
                return daysLeft <= 14 && daysLeft >= 0;
            }).ToString();
        }

        // 🔹 Обработчики событий
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => LoadEquipment();
        private void CmbWorkshopFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadEquipment();
        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => LoadEquipment();

        private void BtnAddEquipment_Click(object sender, RoutedEventArgs e)
        {
            var window = new EquipmentManage();
            if (window.ShowDialog() == true)
            {
                LoadEquipment();
            }
        }

        private void EquipmentCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is int id)
            {
                OpenEditWindow(id);
            }
        }

        private void EditEquipment_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is int id)
            {
                OpenEditWindow(id);
            }
        }

        private void OpenEditWindow(int id)
        {
            var equipment = db.EQUIPMENT
                .Include(e => e.WORKSHOPS)
                .Include(e => e.MASTERS)
                .Include(e => e.STATUSASSETS)
                .FirstOrDefault(e => e.ID == id);

            if (equipment != null)
            {
                var window = new EquipmentManage(equipment);
                if (window.ShowDialog() == true)
                {
                    LoadEquipment();
                }
            }
        }

        private void DeleteEquipment_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not int id) return;

            var result = MessageBox.Show("Удалить это оборудование?", "Подтверждение",
                                       MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                var item = db.EQUIPMENT.Find(id);
                if (item != null)
                {
                    db.EQUIPMENT.Remove(item);
                    db.SaveChanges();
                    LoadEquipment();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 🔹 Освобождение ресурсов
        public void Dispose() => db?.Dispose();
    }
}