using EnterpriseAssets.Model.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace EnterpriseAssets.View.Pages
{
    public partial class SuppliersPage : Page
    {
        private DB_AssetManage db = new DB_AssetManage();
        private List<SupplierViewModel> _allSuppliers;

        public SuppliersPage()
        {
            InitializeComponent();

            // Подписываемся на событие загрузки страницы
            this.Loaded += SuppliersPage_Loaded;
        }

        private void SuppliersPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                var suppliers = db.SUPPLIERS.ToList();
                _allSuppliers = suppliers.Select(s => new SupplierViewModel(s)).ToList();
                ApplyFilterAndSearch();
                UpdateStats();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilterAndSearch()
        {
            if (_allSuppliers == null) return;

            var filtered = _allSuppliers.AsEnumerable();

            // Фильтр по статусу (проверяем что элементы не null)
            if (FilterActive != null && FilterActive.IsChecked == true)
                filtered = filtered.Where(s => s.IsActive);
            else if (FilterInactive != null && FilterInactive.IsChecked == true)
                filtered = filtered.Where(s => !s.IsActive);

            // Поиск по названию, контактному лицу, email
            if (TxtSearch != null && !string.IsNullOrWhiteSpace(TxtSearch.Text))
            {
                string search = TxtSearch.Text.ToLower();
                filtered = filtered.Where(s =>
                    (s.name != null && s.name.ToLower().Contains(search)) ||
                    (s.contact_person != null && s.contact_person.ToLower().Contains(search)) ||
                    (s.email != null && s.email.ToLower().Contains(search)));
            }

            SuppliersList.ItemsSource = filtered.OrderBy(s => s.name).ToList();
        }

        private void UpdateStats()
        {
            if (_allSuppliers == null) return;

            if (TotalCount != null)
                TotalCount.Text = _allSuppliers.Count.ToString();

            if (ActiveCount != null)
                ActiveCount.Text = _allSuppliers.Count(s => s.IsActive).ToString();
        }

        private void FilterSuppliers(object sender, RoutedEventArgs e)
        {
            ApplyFilterAndSearch();
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilterAndSearch();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void BtnAddSupplier_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SupplierManage();
            dialog.Owner = Window.GetWindow(this);

            if (dialog.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void SupplierCard_Click(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border?.Tag != null)
            {
                int supplierId = (int)border.Tag;
                var supplier = db.SUPPLIERS.FirstOrDefault(s => s.id == supplierId);
                if (supplier != null)
                {
                    var dialog = new SupplierManage(supplier);
                    dialog.Owner = Window.GetWindow(this);

                    if (dialog.ShowDialog() == true)
                    {
                        LoadData();
                    }
                }
            }
        }

        private void EditSupplier_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag != null)
            {
                int supplierId = (int)button.Tag;
                var supplier = db.SUPPLIERS.FirstOrDefault(s => s.id == supplierId);
                if (supplier != null)
                {
                    var dialog = new SupplierManage(supplier);
                    dialog.Owner = Window.GetWindow(this);

                    if (dialog.ShowDialog() == true)
                    {
                        LoadData();
                    }
                }
            }
        }

        private void ToggleStatus_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag != null)
            {
                int supplierId = (int)button.Tag;
                var supplier = db.SUPPLIERS.FirstOrDefault(s => s.id == supplierId);
                if (supplier != null)
                {
                    string action = supplier.is_active == true ? "деактивировать" : "активировать";
                    var result = MessageBox.Show(
                        $"Вы уверены, что хотите {action} поставщика '{supplier.name}'?",
                        "Подтверждение",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        supplier.is_active = supplier.is_active != true;
                        db.SaveChanges();
                        LoadData();
                    }
                }
            }
        }
    }

    // ViewModel для отображения поставщика
    public class SupplierViewModel
    {
        private SUPPLIERS _supplier;

        public SupplierViewModel(SUPPLIERS supplier)
        {
            _supplier = supplier ?? throw new ArgumentNullException(nameof(supplier));
        }

        public int id => _supplier.id;
        public string name => _supplier.name ?? "Без названия";
        public string contact_person => _supplier.contact_person ?? "Не указан";
        public string phone => _supplier.phone ?? "Не указан";
        public string email => _supplier.email ?? "Не указан";
        public string address => _supplier.address ?? "Не указан";
        public string tax_number => _supplier.tax_number ?? "Не указан";
        public bool IsActive => _supplier.is_active ?? true;

        public string StatusText => IsActive ? "Активен" : "Неактивен";
        public Brush StatusColor => IsActive ?
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")) :
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));

        public Brush StatusBorderColor => IsActive ?
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E1E5EB")) :
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFCDD2"));

        public Visibility StatusVisibility => IsActive ? Visibility.Collapsed : Visibility.Visible;

        public string ToggleIcon => IsActive ? "⏸️" : "▶️";
        public string ToggleTooltip => IsActive ? "Деактивировать" : "Активировать";
        public Brush ToggleColor => IsActive ?
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F39C12")) :
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));
    }
}