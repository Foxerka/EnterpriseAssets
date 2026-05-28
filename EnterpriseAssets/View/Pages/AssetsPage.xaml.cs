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
    public partial class AssetsPage : Page
    {
        private DB_AssetManage db = new DB_AssetManage();
        private List<AssetViewModel> _allAssets;
        private bool _isAdmin;
        private int _currentUserId;

        // Конструктор по умолчанию
        public AssetsPage()
        {
            InitializeComponent();
            Loaded += AssetsPage_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Показываем/скрываем админ-вкладку
            if (TabAdmin != null)
            {
                TabAdmin.Visibility = _isAdmin ? Visibility.Visible : Visibility.Collapsed;
            }

            LoadReferenceData();
            LoadAssets();
            LoadSuppliersForAdmin();
        }

        private void CmbTypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadAssets();
        }


        public AssetsPage(bool isAdmin) : this()
        {
            _isAdmin = isAdmin;
        }

        public AssetsPage(bool isAdmin, int currentUserId) : this(isAdmin)
        {
            _currentUserId = currentUserId;
        }

        private void AssetsPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Показываем/скрываем админ-вкладку
            if (TabAdmin != null)
            {
                TabAdmin.Visibility = _isAdmin ? Visibility.Visible : Visibility.Collapsed;
            }

            LoadReferenceData();
            LoadAssets();
            LoadSuppliersForAdmin();
        }

        private void LoadReferenceData()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 Загрузка справочников...");

                // Типы активов
                var assetTypes = db.ASSETTYPE.OrderBy(t => t.AssetType1).ToList();
                System.Diagnostics.Debug.WriteLine($"📦 Типы: {assetTypes.Count} записей");

                // Категории
                var categories = db.CATEGORY.OrderBy(c => c.Category1).ToList();
                System.Diagnostics.Debug.WriteLine($"📁 Категории: {categories.Count} записей");

                // Статусы
                var statuses = db.STATUSASSETS.OrderBy(s => s.Status).ToList();
                System.Diagnostics.Debug.WriteLine($"📊 Статусы: {statuses.Count} записей");

                // Заполняем ComboBox для админки
                ListAssetTypes.ItemsSource = assetTypes;
                ListCategories.ItemsSource = categories;
                ListStatuses.ItemsSource = statuses;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка загрузки справочников: {ex.Message}");
            }
        }

        private void LoadAssets()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 Загрузка активов...");

                // Загружаем активы без Include для избежания ошибок
                var assets = db.PRODUCTION_ASSETS.ToList();

                // Загружаем связанные данные отдельно
                var workshops = db.WORKSHOPS.ToDictionary(w => w.id, w => w.name);
                var categories = db.CATEGORY.ToDictionary(c => c.ID_category, c => c.Category1);
                var assetTypes = db.ASSETTYPE.ToDictionary(t => t.ID_ASSETTYPE, t => t.AssetType1);
                var units = db.Unit.ToDictionary(u => u.ID, u => u.unit1);
                var statuses = db.STATUSASSETS.ToDictionary(s => s.ID_status, s => s.Status);
                var suppliers = db.SUPPLIERS.ToDictionary(s => s.id, s => s.name);

                _allAssets = assets.Select(a => new AssetViewModel
                {
                    Id = a.id,
                    Name = a.name,
                    AssetType = assetTypes.GetValueOrDefault(a.asset_type ?? 0, "Не указан"),
                    CategoryName = categories.GetValueOrDefault(a.id_category ?? 0, "Без категории"),
                    WorkshopName = workshops.GetValueOrDefault(a.workshop_id ?? 0, "Без цеха"),
                    UnitName = units.GetValueOrDefault(a.unit ?? 0, "шт"),
                    Quantity = a.quantity ?? 0,
                    MinQuantity = a.min_quantity ?? 0,
                    CurrentValue = a.current_value ?? 0,
                    PurchaseCost = a.purchase_cost ?? 0,
                    StatusName = statuses.GetValueOrDefault(a.status ?? 0, ""),
                    TypeIcon = GetTypeIcon(assetTypes.GetValueOrDefault(a.asset_type ?? 0, "")),
                    TypeColor = GetTypeColor(assetTypes.GetValueOrDefault(a.asset_type ?? 0, "")),
                    SupplierName = suppliers.GetValueOrDefault(a.supplier_id ?? 0, ""),
                    ShowStatus = assetTypes.GetValueOrDefault(a.asset_type ?? 0, "") == "Оборудование",
                    StatusColor = GetStatusColor(statuses.GetValueOrDefault(a.status ?? 0, ""))
                }).OrderBy(a => a.Name).ToList();

                System.Diagnostics.Debug.WriteLine($"✅ Загружено активов: {_allAssets.Count}");

                ApplyFilters();
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка загрузки активов: {ex.Message}");
                MessageBox.Show($"Ошибка загрузки активов: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSuppliersForAdmin()
        {
            try
            {
                if (!_isAdmin) return;

                var suppliers = db.SUPPLIERS.OrderBy(s => s.name).ToList();
                ListSuppliers.ItemsSource = suppliers;
                System.Diagnostics.Debug.WriteLine($"✅ Поставщики: {suppliers.Count} записей");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка загрузки поставщиков: {ex.Message}");
            }
        }

        private void ApplyFilters()
        {
            if (_allAssets == null)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ AssetsList is null!");
                return;
            }

            var filtered = _allAssets.AsEnumerable();

            // Поиск
            string searchText = TxtSearch.Text?.Trim().ToLower() ?? "";
            if (!string.IsNullOrEmpty(searchText))
            {
                filtered = filtered.Where(a =>
                    a.Name.ToLower().Contains(searchText) ||
                    a.CategoryName.ToLower().Contains(searchText) ||
                    a.WorkshopName.ToLower().Contains(searchText)
                );
            }

            // Фильтр по типу
            if (CmbTypeFilter.SelectedItem is ComboBoxItem typeItem && typeItem.Content.ToString() != "Все типы")
            {
                filtered = filtered.Where(a => a.AssetType == typeItem.Content.ToString());
            }

            // Фильтр по цеху
            if (CmbWorkshopFilter.SelectedItem is ComboBoxItem workshopItem && workshopItem.Content.ToString() != "Все цеха")
            {
                filtered = filtered.Where(a => a.WorkshopName == workshopItem.Content.ToString());
            }

            AssetsList.ItemsSource = filtered.OrderBy(a => a.Name).ToList();
        }

        private void UpdateStatistics()
        {
            if (_allAssets == null) return;

            TotalAssetsCount.Text = _allAssets.Count.ToString();
            EquipmentCount.Text = _allAssets.Count(a => a.AssetType == "Оборудование").ToString();
            LowStockCount.Text = _allAssets.Count(a => a.Quantity > 0 && a.Quantity <= a.MinQuantity).ToString();
        }

        private string GetTypeIcon(string assetType)
        {
            return assetType switch
            {
                "Оборудование" => "⚙️",
                "Материалы" => "📦",
                "Инструмент" => "🔧",
                "Запчасти" => "🔩",
                _ => "📄"
            };
        }

        private string GetTypeColor(string assetType)
        {
            return assetType switch
            {
                "Оборудование" => "#3498DB",
                "Материалы" => "#27AE60",
                "Инструмент" => "#F39C12",
                "Запчасти" => "#9B59B6",
                _ => "#95A5A6"
            };
        }

        private string GetStatusColor(string status)
        {
            return status switch
            {
                "Исправен" => "#27AE60",
                "В работе" => "#3498DB",
                "В ремонте" => "#F39C12",
                "На обслуживании" => "#9B59B6",
                "Неисправен" => "#E74C3C",
                "Списан" => "#95A5A6",
                _ => "#95A5A6"
            };
        }

        // ========== ОБРАБОТЧИКИ ==========
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }


        private void CmbWorkshopFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void BtnAddAsset_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AssetManage();
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true)
            {
                LoadAssets();
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadAssets();
            LoadSuppliersForAdmin();
        }

        private void AssetCard_Click(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border?.DataContext is AssetViewModel asset)
            {
                var fullAsset = db.PRODUCTION_ASSETS.FirstOrDefault(a => a.id == asset.Id);
                if (fullAsset != null)
                {
                    var dialog = new AssetManage(fullAsset);
                    dialog.Owner = Window.GetWindow(this);
                    if (dialog.ShowDialog() == true)
                    {
                        LoadAssets();
                    }
                }
            }
        }

        private void EditAsset_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag != null)
            {
                int id = (int)button.Tag;
                var asset = db.PRODUCTION_ASSETS.FirstOrDefault(a => a.id == id);
                if (asset != null)
                {
                    var dialog = new AssetManage(asset);
                    dialog.Owner = Window.GetWindow(this);
                    if (dialog.ShowDialog() == true)
                    {
                        LoadAssets();
                    }
                }
            }
        }

        private void DeleteAsset_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag != null)
            {
                int id = (int)button.Tag;
                var asset = db.PRODUCTION_ASSETS.FirstOrDefault(a => a.id == id);
                if (asset != null)
                {
                    var result = MessageBox.Show(
                        $"Вы уверены, что хотите удалить актив «{asset.name}»?\n\nЭто действие нельзя отменить.",
                        "Подтверждение удаления",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            // Проверяем связанные записи
                            bool hasWorkActs = db.WORK_ACTS.Any(wa => wa.asset_id == id);
                            bool hasMaterials = db.WORK_ACTS_MATERIALS.Any(wam => wam.asset_id == id);

                            if (hasWorkActs || hasMaterials)
                            {
                                MessageBox.Show(
                                    "Невозможно удалить актив, так как он используется в рабочих актах.\n\n" +
                                    "Сначала удалите или измените связанные записи.",
                                    "Ошибка удаления",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                                return;
                            }

                            db.PRODUCTION_ASSETS.Remove(asset);
                            db.SaveChanges();
                            LoadAssets();
                            LoadSuppliersForAdmin();

                            MessageBox.Show("Актив успешно удален", "Успех",
                                          MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                                          MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
        }

        // ========== АДМИНКА ==========
        private void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainTabs.SelectedItem == TabAdmin && _isAdmin)
            {
                LoadReferenceData();
                LoadSuppliersForAdmin();
            }
        }

        private void AddAssetType_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtNewAssetType.Text))
            {
                MessageBox.Show("Введите название типа актива", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var newType = new ASSETTYPE { AssetType1 = TxtNewAssetType.Text.Trim() };
                db.ASSETTYPE.Add(newType);
                db.SaveChanges();

                LoadReferenceData();
                TxtNewAssetType.Text = "";

                MessageBox.Show("Тип актива добавлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteAssetType_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag != null)
            {
                int id = (int)button.Tag;
                var type = db.ASSETTYPE.Find(id);
                if (type != null)
                {
                    // Проверяем, используется ли тип
                    bool inUse = db.PRODUCTION_ASSETS.Any(a => a.asset_type == id);
                    if (inUse)
                    {
                        MessageBox.Show("Нельзя удалить тип, который используется в активах", "Ошибка",
                                      MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    db.ASSETTYPE.Remove(type);
                    db.SaveChanges();
                    LoadReferenceData();
                }
            }
        }

        private void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtNewCategory.Text))
            {
                MessageBox.Show("Введите название категории", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var newCategory = new CATEGORY { Category1 = TxtNewCategory.Text.Trim() };
                db.CATEGORY.Add(newCategory);
                db.SaveChanges();

                LoadReferenceData();
                TxtNewCategory.Text = "";

                MessageBox.Show("Категория добавлена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag != null)
            {
                int id = (int)button.Tag;
                var category = db.CATEGORY.Find(id);
                if (category != null)
                {
                    bool inUse = db.PRODUCTION_ASSETS.Any(a => a.id_category == id);
                    if (inUse)
                    {
                        MessageBox.Show("Нельзя удалить категорию, которая используется в активах", "Ошибка",
                                      MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    db.CATEGORY.Remove(category);
                    db.SaveChanges();
                    LoadReferenceData();
                }
            }
        }

        private void AddStatus_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtNewStatus.Text))
            {
                MessageBox.Show("Введите название статуса", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var newStatus = new STATUSASSETS { Status = TxtNewStatus.Text.Trim() };
                db.STATUSASSETS.Add(newStatus);
                db.SaveChanges();

                LoadReferenceData();
                TxtNewStatus.Text = "";

                MessageBox.Show("Статус добавлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteStatus_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag != null)
            {
                int id = (int)button.Tag;
                var status = db.STATUSASSETS.Find(id);
                if (status != null)
                {
                    bool inUse = db.PRODUCTION_ASSETS.Any(a => a.status == id);
                    if (inUse)
                    {
                        MessageBox.Show("Нельзя удалить статус, который используется в активах", "Ошибка",
                                      MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    db.STATUSASSETS.Remove(status);
                    db.SaveChanges();
                    LoadReferenceData();
                }
            }
        }

        // ========== ПОСТАВЩИКИ ==========
        private void TxtSearchSupplier_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Проверяем, что элемент существует
            if (ListSuppliers?.ItemsSource == null) return;

            // Получаем TextBox через sender
            var searchBox = sender as TextBox;
            if (searchBox == null) return;

            string search = searchBox.Text?.Trim().ToLower() ?? "";

            try
            {
                var filtered = db.SUPPLIERS
                    .Where(s => s.name.ToLower().Contains(search) ||
                               (s.contact_person ?? "").ToLower().Contains(search))
                    .ToList();
                ListSuppliers.ItemsSource = filtered;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка поиска: {ex.Message}");
            }
        }

        private void AddSupplier_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SupplierManage();
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true)
            {
                LoadSuppliersForAdmin();
            }
        }

        private void ListSuppliers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListSuppliers.SelectedItem is SUPPLIERS selected)
            {
                try
                {
                    var assets = db.PRODUCTION_ASSETS
                        .Where(a => a.supplier_id == selected.id)
                        .ToList();
                    SupplierAssetsList.ItemsSource = assets;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка загрузки активов поставщика: {ex.Message}");
                }
            }
        }

        private void UnlinkAsset_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag != null)
            {
                int assetId = (int)button.Tag;
                try
                {
                    var asset = db.PRODUCTION_ASSETS.Find(assetId);
                    if (asset != null)
                    {
                        asset.supplier_id = null;
                        db.SaveChanges();

                        if (ListSuppliers.SelectedItem is SUPPLIERS selected)
                        {
                            var assets = db.PRODUCTION_ASSETS
                                .Where(a => a.supplier_id == selected.id)
                                .ToList();
                            SupplierAssetsList.ItemsSource = assets;
                        }

                        MessageBox.Show("Связь с поставщиком удалена", "Успех",
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    // ViewModel для актива
    public class AssetViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string AssetType { get; set; }
        public string CategoryName { get; set; }
        public string WorkshopName { get; set; }
        public string UnitName { get; set; }
        public decimal Quantity { get; set; }
        public decimal MinQuantity { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal PurchaseCost { get; set; }
        public string StatusName { get; set; }
        public string TypeIcon { get; set; }
        public string TypeColor { get; set; }
        public string SupplierName { get; set; }
        public bool ShowStatus { get; set; }
        public string StatusColor { get; set; }

        public string QuantityDisplay => $"{Quantity:N0} {UnitName}";
        public string ValueDisplay => CurrentValue > 0 ? $"{CurrentValue:N2} ₸" : "";
        public bool IsLowStock => Quantity > 0 && Quantity <= MinQuantity;
    }

    // Extension method для Dictionary
    public static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue defaultValue)
        {
            return dict.TryGetValue(key, out TValue value) ? value : defaultValue;
        }
    }
}