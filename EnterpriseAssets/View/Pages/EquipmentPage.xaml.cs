using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using EnterpriseAssets.Model.DataBase;
using System.ComponentModel;

namespace EnterpriseAssets.View.Pages
{
    public partial class EquipmentPage : Page, INotifyPropertyChanged
    {
        private DB_AssetManage db = new DB_AssetManage();

        private List<EquipmentViewModel> _allEquipment;
        private List<EquipmentViewModel> _filteredEquipment;
        private List<string> _workshops;
        private List<STATUSASSETS> _statuses;

        public event PropertyChangedEventHandler PropertyChanged;

        // Статистика (привязана к нижней панели)
        private int _totalCount;
        public int TotalCount
        {
            get => _totalCount;
            set { _totalCount = value; OnPropertyChanged(nameof(TotalCount)); }
        }

        private int _activeCount;
        public int ActiveCount
        {
            get => _activeCount;
            set { _activeCount = value; OnPropertyChanged(nameof(ActiveCount)); }
        }

        private int _maintenanceCount;
        public int MaintenanceCount
        {
            get => _maintenanceCount;
            set { _maintenanceCount = value; OnPropertyChanged(nameof(MaintenanceCount)); }
        }

        public EquipmentPage()
        {
            InitializeComponent();
            DataContext = this; // для привязки статистики
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
            FillFilters();
            ApplyFilters();
        }

        /// <summary>
        /// Загрузка данных из БД (синхронно)
        /// </summary>
        private void LoadData()
        {
            // Загружаем оборудование со связанными сущностями
            var equipmentQuery = db.EQUIPMENT
                .Include("WORKSHOPS")
                .Include("MASTERS.USERS")
                .Include("STATUSASSETS")
                .Include("MAINTENANCE");

            var equipmentList = equipmentQuery.ToList();
            _allEquipment = equipmentList.Select(eq => new EquipmentViewModel(eq)).ToList();

            _statuses = db.STATUSASSETS.ToList();
        }

        /// <summary>
        /// Заполнение фильтров (цеха и статусы)
        /// </summary>
        private void FillFilters()
        {
            // Цеха
            _workshops = _allEquipment
                .Where(e => e.WorkshopName != null)
                .Select(e => e.WorkshopName)
                .Distinct()
                .OrderBy(w => w)
                .ToList();

            CmbWorkshopFilter.Items.Clear();
            CmbWorkshopFilter.Items.Add(new ComboBoxItem { Content = "Все цеха", IsSelected = true });
            foreach (var ws in _workshops)
            {
                CmbWorkshopFilter.Items.Add(new ComboBoxItem { Content = ws });
            }

            // Статусы (из справочника STATUSASSETS)
            CmbStatusFilter.Items.Clear();
            CmbStatusFilter.Items.Add(new ComboBoxItem { Content = "Все статусы", IsSelected = true });
            foreach (var status in _statuses.OrderBy(s => s.Status))
            {
                CmbStatusFilter.Items.Add(new ComboBoxItem { Content = status.Status });
            }
        }

        /// <summary>
        /// Применение фильтров
        /// </summary>
        private void ApplyFilters()
        {
            if (_allEquipment == null) return;

            var query = _allEquipment.AsEnumerable();

            // Поиск по тексту
            string searchText = TxtSearch.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(searchText))
            {
                query = query.Where(e =>
                    (e.AssetName?.ToLower().Contains(searchText) ?? false) ||
                    (e.AssetId?.ToLower().Contains(searchText) ?? false) ||
                    (e.Manufacturer?.ToLower().Contains(searchText) ?? false) ||
                    (e.EquipmentType?.ToLower().Contains(searchText) ?? false));
            }

            // Фильтр по цеху
            if (CmbWorkshopFilter.SelectedItem is ComboBoxItem workshopItem && workshopItem.Content.ToString() != "Все цеха")
            {
                string selectedWorkshop = workshopItem.Content.ToString();
                query = query.Where(e => e.WorkshopName == selectedWorkshop);
            }

            // Фильтр по статусу
            if (CmbStatusFilter.SelectedItem is ComboBoxItem statusItem && statusItem.Content.ToString() != "Все статусы")
            {
                string selectedStatus = statusItem.Content.ToString();
                query = query.Where(e => e.StatusName == selectedStatus);
            }

            _filteredEquipment = query.ToList();
            EquipmentList.ItemsSource = _filteredEquipment;

            // Обновление статистики
            UpdateStatistics();
        }

        /// <summary>
        /// Обновление нижней статистики
        /// </summary>
        private void UpdateStatistics()
        {
            TotalCount = _filteredEquipment?.Count ?? 0;
            ActiveCount = _filteredEquipment?.Count(e => e.StatusName.Contains("эксплуатац") || e.OperationalStatus?.Contains("эксплуатац") == true) ?? 0;
            MaintenanceCount = _filteredEquipment?.Count(e => e.IsMaintenanceDue) ?? 0;
        }

        // Обработчики фильтров
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();
        private void CmbWorkshopFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();
        private void CmbStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();

        // Обновление (кнопка Refresh)
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
            FillFilters();
            ApplyFilters();
        }

        // Добавление оборудования
        private void BtnAddEquipment_Click(object sender, RoutedEventArgs e)
        {
            var window = new EquipmentManage(); // без параметра – добавление
            window.Owner = Window.GetWindow(this); // привязываем к родительскому окну
            if (window.ShowDialog() == true)
            {
                // Если данные изменились – обновляем список
                LoadData();
                FillFilters();
                ApplyFilters();
            }
        }

        // Клик по карточке (просмотр деталей)
        private void EquipmentCard_Click(object sender, MouseButtonEventArgs e)
        {
            // Если кликнули по кнопке (или её потомку) – игнорируем, чтобы не открывать карточку повторно
            var originalSource = e.OriginalSource as DependencyObject;
            if (originalSource != null && FindParent<Button>(originalSource) != null)
                return;

            if (sender is Border border && border.Tag is int id)
            {
                var window = new EquipmentManage(id);
                window.Owner = Window.GetWindow(this);
                if (window.ShowDialog() == true)
                {
                    LoadData();
                    FillFilters();
                    ApplyFilters();
                }
            }
        }

        // Вспомогательный метод для поиска родительского элемента определённого типа
        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            while (parent != null && !(parent is T))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as T;
        }

        // Редактирование
        private void EditEquipment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var window = new EquipmentManage(id);
                window.Owner = Window.GetWindow(this);
                if (window.ShowDialog() == true)
                {
                    LoadData();
                    FillFilters();
                    ApplyFilters();
                }
            }
        }

        // Удаление
        private void DeleteEquipment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var result = MessageBox.Show("Вы уверены, что хотите удалить оборудование?", "Подтверждение", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    var equipment = db.EQUIPMENT.Find(id); // синхронный Find
                    if (equipment != null)
                    {
                        db.EQUIPMENT.Remove(equipment);
                        db.SaveChanges(); // синхронный SaveChanges
                        LoadData();
                        FillFilters();
                        ApplyFilters();
                    }
                }
            }
        }

        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// ViewModel для отображения оборудования в списке
    /// </summary>
    public class EquipmentViewModel : INotifyPropertyChanged
    {
        private readonly EQUIPMENT _entity;

        public EquipmentViewModel(EQUIPMENT entity)
        {
            _entity = entity;
        }

        public int Id => _entity.ID;
        public string AssetId => _entity.asset_id;

        // Название актива (собираем из типа и производителя)
        public string AssetName => $"{_entity.equipment_type} {_entity.manufacturer}".Trim();

        public string EquipmentType => _entity.equipment_type;
        public string Manufacturer => _entity.manufacturer;

        // Цех
        public string WorkshopName => _entity.WORKSHOPS?.name;

        // Мастер (используем full_name из USERS)
        public string MasterName
        {
            get
            {
                if (_entity.MASTERS?.USERS != null && !string.IsNullOrWhiteSpace(_entity.MASTERS.USERS.full_name))
                    return _entity.MASTERS.USERS.full_name;
                return _entity.assigned_to?.ToString() ?? "Не назначен";
            }
        }

        // Статус (из справочника STATUSASSETS)
        public string StatusName => _entity.STATUSASSETS?.Status ?? _entity.operational_status ?? "Неизвестно";

        // Для цветовой индикации статуса
        public Brush StatusColor
        {
            get
            {
                return (StatusName ?? "").ToLower() switch
                {
                    string s when s.Contains("эксплуатац") => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")), // зелёный
                    string s when s.Contains("обслуживан") => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F39C12")), // оранжевый
                    string s when s.Contains("неисправ") => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C")),   // красный
                    string s when s.Contains("списан") => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7F8C8D")),     // серый
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
        }

        // Операционный статус (для фильтрации, если нужно)
        public string OperationalStatus => _entity.operational_status;

        // Наработка
        public double WorkHours => _entity.current_work_hours ?? 0;
        public double MaxHours => _entity.max_work_hours_before_maintenance ?? 0;
        public string WorkHoursDisplay => $"{WorkHours:0} ч";
        public string MaxHoursDisplay => $"{MaxHours:0} ч";
        public double WorkHoursPercent => MaxHours > 0 ? (WorkHours / MaxHours) * 100 : 0;

        // Следующее обслуживание
        public DateTime? NextMaintenance => _entity.next_maintenance_date;
        public string NextMaintenanceDisplay
        {
            get
            {
                if (!NextMaintenance.HasValue) return "Не назначено";
                if (NextMaintenance.Value < DateTime.Now) return "Просрочено!";
                var days = (NextMaintenance.Value - DateTime.Now).Days;
                return days == 0 ? "Сегодня" : $"Через {days} дн.";
            }
        }

        public Brush MaintenanceColor
        {
            get
            {
                if (!NextMaintenance.HasValue) return new SolidColorBrush(Colors.Gray);
                if (NextMaintenance.Value < DateTime.Now) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C")); // красный
                if ((NextMaintenance.Value - DateTime.Now).Days <= 7) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F39C12")); // оранжевый
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")); // зелёный
            }
        }

        // Гарантия
        public DateTime? WarrantyUntil
        {
            get
            {
                if (_entity.installation_date.HasValue && _entity.warranty_period_months.HasValue)
                    return _entity.installation_date.Value.AddMonths(_entity.warranty_period_months.Value);
                return null;
            }
        }

        public string WarrantyDisplay
        {
            get
            {
                if (!WarrantyUntil.HasValue) return "Нет данных";
                if (WarrantyUntil.Value < DateTime.Now) return "Гарантия истекла";
                var days = (WarrantyUntil.Value - DateTime.Now).Days;
                return $"Гарантия: {days} дн.";
            }
        }

        public Brush WarrantyColor
        {
            get
            {
                if (!WarrantyUntil.HasValue) return new SolidColorBrush(Colors.Gray);
                if (WarrantyUntil.Value < DateTime.Now) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C")); // красный
                if ((WarrantyUntil.Value - DateTime.Now).Days <= 30) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F39C12")); // оранжевый
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")); // зелёный
            }
        }

        // Флаг для статистики (требуется ТО в ближайшие 7 дней или просрочено)
        public bool IsMaintenanceDue
        {
            get
            {
                if (!NextMaintenance.HasValue) return false;
                return NextMaintenance.Value <= DateTime.Now.AddDays(7);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}