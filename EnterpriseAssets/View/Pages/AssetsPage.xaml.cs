using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using EnterpriseAssets.Model.DataBase;
using System.Data.Entity;
using EnterpriseAssets.ViewModel;

namespace EnterpriseAssets.View.Pages
{
    public partial class AssetsPage : Page
    {
        private DB_AssetManage db = new DB_AssetManage();
        private int? _currentUserId;
        private bool _isAdmin;

        public AssetsPage(bool isAdmin = false)
        {
            LoadEquipmentTypeId();
            InitializeComponent();
            _isAdmin = isAdmin;  // ✅ Прямая установка
            InitializePage();
        }

        private void InitializePage()
        {
            // Скрываем админ-панель если не админ
            if (!_isAdmin && TabAdmin != null)
            {
                TabAdmin.Visibility = Visibility.Collapsed;
                AdminPanel.Visibility = Visibility.Collapsed;
            }

            // Загружаем данные
            LoadFilters();
            LoadAssets();
            LoadAdminData();  // ✅ Внутри есть проверка if (!_isAdmin) return;
            LoadSuppliers();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("📦 AssetsPage loaded");

                // ✅ Проверяем что db инициализирован
                if (db == null)
                {
                    db = new DB_AssetManage();
                    System.Diagnostics.Debug.WriteLine("✅ DB_AssetManage создана");
                }

                LoadFilters();
                LoadAssets();
                LoadAdminData();
                LoadSuppliers();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Page_Loaded error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        // 🔹 Загрузка фильтров (цеха)
        private void LoadFilters()
        {
            try
            {
                // ✅ Проверяем что ComboBox существует
                if (CmbWorkshopFilter == null)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ CmbWorkshopFilter is null!");
                    return;
                }

                CmbWorkshopFilter.Items.Clear();
                CmbWorkshopFilter.Items.Add(new ComboBoxItem { Content = "Все цеха", Tag = null });

                var workshops = db.WORKSHOPS?.OrderBy(w => w.name).ToList();
                if (workshops != null)
                {
                    foreach (var w in workshops)
                    {
                        CmbWorkshopFilter.Items.Add(new ComboBoxItem { Content = w.name, Tag = w.id });
                    }
                }
                CmbWorkshopFilter.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ошибка: {ex.Message}");
            }
        }

        // 🔹 Загрузка активов с привязанными данными
        private void LoadAssets()
        {
            try
            {
                // ✅ Проверяем что AssetsList существует
                if (AssetsList == null)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ AssetsList is null!");
                    return;
                }

                var query = db.PRODUCTION_ASSETS
                    .Include("ASSETTYPE")
                    .Include("CATEGORY")
                    .Include("WORKSHOPS")
                    .Include("STATUSASSETS")
                    .Include(a => a.Unit1)
                    .AsQueryable();

                // Поиск
                if (!string.IsNullOrWhiteSpace(TxtSearch?.Text))
                {
                    var search = TxtSearch.Text.ToLower();
                    query = query.Where(a => a.name.ToLower().Contains(search)
                                          || (a.description != null && a.description.ToLower().Contains(search))
                                          || (a.serial_number != null && a.serial_number.ToLower().Contains(search)));
                }

                // Фильтр по цеху - ✅ БЕЗОПАСНАЯ ПРОВЕРКА
                if (CmbWorkshopFilter != null && CmbWorkshopFilter.SelectedIndex > 0)
                {
                    if (CmbWorkshopFilter.SelectedItem is ComboBoxItem selectedWorkshop)
                    {
                        if (selectedWorkshop.Tag is int workshopId)
                        {
                            query = query.Where(a => a.workshop_id == workshopId);
                        }
                    }
                }

                var assets = query.OrderByDescending(a => a.created_at).ToList();

                // Преобразуем в ViewModel для отображения
                AssetsList.ItemsSource = assets.Select(a => new AssetViewModel
                {
                    Id = a.id,
                    Name = a.name ?? "Без названия",
                    CategoryName = a.CATEGORY?.Category1 ?? "—",
                    WorkshopName = a.WORKSHOPS?.name ?? "Не назначен",
                    Quantity = a.quantity,

                    // ✅ Безопасное получение единицы измерения
                    UnitName = GetUnitName(a.unit, a.Unit1),

                    Value = a.current_value,
                    StatusName = a.STATUSASSETS?.Status,
                    StatusColor = GetStatusColor(a.STATUSASSETS?.Status),  // ← название статуса

                    // Получаем название типа из навигационного свойства или fallback
                    TypeName = a.ASSETTYPE?.AssetType1,
                    TypeIcon = GetTypeIcon(a.ASSETTYPE?.AssetType1),       // ← название типа
                    TypeColor = GetTypeColor(a.ASSETTYPE?.AssetType1),     // ← название типа

                    // Показ статуса только для "Оборудование"
                    ShowStatus = string.Equals(a.ASSETTYPE?.AssetType1?.Trim(), "Оборудование",
                               StringComparison.OrdinalIgnoreCase)
                 ? Visibility.Visible
                 : Visibility.Collapsed,
                    ShowStatusIcon = string.Equals(a.ASSETTYPE?.AssetType1?.Trim(), "Оборудование",
                                   StringComparison.OrdinalIgnoreCase)
                     ? Visibility.Visible
                     : Visibility.Collapsed,

                    // ✅ Используем GetUnitName и для отображения
                    QuantityDisplay = $"{a.quantity} {GetUnitName(a.unit, a.Unit1)}",
                    ValueDisplay = a.current_value.HasValue ? $"{a.current_value:C}" : "—"
                }).ToList();

                UpdateStats(assets);
            }
            catch (Exception ex)
            {

                System.Diagnostics.Debug.WriteLine($"❌ LoadAssets ошибка: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                if (IsLoaded)
                {
                    try
                    {
                        MessageBox.Show($"Ошибка загрузки активов: {ex.Message}", "Ошибка",
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    catch { /* Игнорируем ошибки при показе MessageBox */ }
                }
            }
        }


        private string GetUnitName(int? unitId, Unit unitEntity)
        {
            if (unitEntity?.unit1 != null)
                return unitEntity.unit1;
            if (unitId.HasValue)
            {
                var unit = db.Unit.Find(unitId.Value);
                if (unit?.unit1 != null)
                    return unit.unit1;
            }
            return "шт.";
        }
        private void LoadSuppliers()
        {
            try
            {
                // ✅ Проверяем что ListSuppliers существует
                if (ListSuppliers == null)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ ListSuppliers is null!");
                    return;
                }

                if (db?.SUPPLIERS == null)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ db.SUPPLIERS is null!");
                    return;
                }

                ListSuppliers.ItemsSource = db.SUPPLIERS.OrderBy(s => s.name).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ LoadSuppliers error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        // 🔹 ViewModel для карточки актива
        public class AssetViewModel
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string TypeName { get; set; }
            public string CategoryName { get; set; }
            public string WorkshopName { get; set; }
            public decimal? Quantity { get; set; }
            public string UnitName { get; set; }
            public decimal? Value { get; set; }
            public string StatusName { get; set; }
            public string StatusColor { get; set; }
            public string TypeIcon { get; set; }
            public string TypeColor { get; set; }
            public Visibility ShowStatus { get; set; }
            public Visibility ShowStatusIcon { get; set; }
            public string QuantityDisplay { get; set; }
            public string ValueDisplay { get; set; }
        }

        // 🔹 Вспомогательные методы
        private string GetStatusColor(string statusName)
        {
            return statusName?.Trim().ToLower() switch
            {
                "Исправен" => "#27AE60",
                "В работе" => "#27AE60",    // 🟢 Зелёный
                "на обслуживании" => "#F39C12",   // 🟡 Жёлтый
                "неисправен" => "#E74C3C",        // 🔴 Красный
                "списан" => "#95A5A6",            // ⚪ Серый
                "В ремонте" => "#E67E22",        // 🟠 Оранжевый
                "В резерве" => "#3498DB",            // 🔵 Синий
                _ => "#7F8C8D"                    // ⚫ Дефолтный серый
            };
        }

        // ⚙️ Иконка типа актива по названию
        private string GetTypeIcon(string assetTypeName)
        {
            return assetTypeName?.Trim().ToLower() switch
            {
                "оборудование" => "⚙️",
                "материал" => "📦",
                "Комплектующие" => "🔧",
                "инструмент" => "🪛",
                "сырье" => "🧱",
                "готовая продукция" => "📦",
                _ => "📋"  // Дефолтная иконка
            };
        }

        // 🎨 Цвет типа актива по названию
        private string GetTypeColor(string assetTypeName)
        {
            return assetTypeName?.Trim().ToLower() switch
            {
                "оборудование" => "#4A6FA5",   // 🔵 Синий
                "материал" => "#27AE60",      // 🟢 Зелёный
                "Комплектующие" => "#8E44AD",       // 🟣 Фиолетовый
                "инструмент" => "#D35400",     // 🟠 Оранжевый
                "сырье" => "#16A085",          // 🟢 Бирюзовый
                "готовая продукция" => "#2C3E50", // ⚫ Тёмно-серый
                _ => "#95A5A6"                 // ⚪ Дефолтный серый
            };
        }


        private int _equipmentTypeId;

        private void LoadEquipmentTypeId()
        {
            _equipmentTypeId = db.ASSETTYPE
                .Where(t => t.AssetType1 == "Оборудование")
                .Select(t => t.ID_ASSETTYPE)
                .FirstOrDefault();
        }

        private void UpdateStats(List<PRODUCTION_ASSETS> assets)
        {
            TotalAssetsCount.Text = assets.Count.ToString();
            EquipmentCount.Text = assets.Count(a => a.asset_type == _equipmentTypeId).ToString();
            LowStockCount.Text = assets.Count(a => a.quantity < a.min_quantity).ToString();
        }

        // 🔹 Обработчики событий фильтров
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
            => LoadAssets();

        private void CmbTypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => LoadAssets();

        private void CmbWorkshopFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => LoadAssets();

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
            => LoadAssets();

        private void BtnAddAsset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = new AssetManage();
                if (window.ShowDialog() == true)
                {
                    LoadAssets();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AssetCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is int assetId)
            {
                try
                {
                    var asset = db.PRODUCTION_ASSETS.Find(assetId);
                    if (asset != null)
                    {
                        var window = new AssetManage(asset);
                        if (window.ShowDialog() == true)
                        {
                            LoadAssets();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void EditAsset_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is int assetId)
            {
                try
                {
                    var asset = db.PRODUCTION_ASSETS.Find(assetId);
                    if (asset != null)
                    {
                        var window = new AssetManage(asset);
                        if (window.ShowDialog() == true)
                        {
                            LoadAssets();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteAsset_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not int assetId) return;

            var result = MessageBox.Show("Удалить этот актив?", "Подтверждение",
                                       MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var asset = db.PRODUCTION_ASSETS.Find(assetId);
                    if (asset == null) return;

                    // Проверка зависимостей
                    bool hasWorkActs = db.WORK_ACTS.Any(wa => wa.asset_id == assetId);
                    bool hasWorkActsMaterials = db.WORK_ACTS_MATERIALS.Any(wam => wam.asset_id == assetId);

                    if (hasWorkActs || hasWorkActsMaterials)
                    {
                        MessageBox.Show(
                            "Невозможно удалить актив, так как он используется в актах выполненных работ.",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    db.PRODUCTION_ASSETS.Remove(asset);
                    db.SaveChanges();
                    LoadAssets();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // 🔹 Админ-панель: загрузка справочников
        private void LoadAdminData()
        {
            if (!_isAdmin) return;

            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 Загрузка справочников...");

                var types = db.ASSETTYPE.OrderBy(t => t.AssetType1).ToList();
                System.Diagnostics.Debug.WriteLine($"📦 Типы: {types.Count} записей");
                ListAssetTypes.ItemsSource = types;

                var categories = db.CATEGORY.OrderBy(c => c.Category1).ToList();
                System.Diagnostics.Debug.WriteLine($"📁 Категории: {categories.Count} записей");
                ListCategories.ItemsSource = categories;

                var statuses = db.STATUSASSETS.OrderBy(s => s.Status).ToList();
                System.Diagnostics.Debug.WriteLine($"📊 Статусы: {statuses.Count} записей");
                ListStatuses.ItemsSource = statuses;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка: {ex.Message}");
                MessageBox.Show($"Ошибка загрузки справочников: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        // 🔹 Типы активов
        private void AddAssetType_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtNewAssetType.Text))
            {
                MessageBox.Show("Введите название типа", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                db.ASSETTYPE.Add(new ASSETTYPE { AssetType1 = TxtNewAssetType.Text.Trim() });
                db.SaveChanges();
                TxtNewAssetType.Clear();
                LoadAdminData();
                MessageBox.Show("Тип добавлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteAssetType_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not int id) return;

            try
            {
                // Проверка зависимостей
                bool hasAssets = db.PRODUCTION_ASSETS.Any(a => a.asset_type == id);
                if (hasAssets)
                {
                    MessageBox.Show("Невозможно удалить тип, так как есть активы с этим типом",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var item = db.ASSETTYPE.Find(id); 
                if (item != null)
                {
                    db.ASSETTYPE.Remove(item);
                    db.SaveChanges();
                    LoadAdminData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 🔹 Категории
        private void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtNewCategory.Text))
            {
                MessageBox.Show("Введите название категории", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                db.CATEGORY.Add(new CATEGORY { Category1 = TxtNewCategory.Text.Trim() });
                db.SaveChanges();
                TxtNewCategory.Clear();
                LoadAdminData();
                MessageBox.Show("Категория добавлена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not int id) return;

            try
            {
                bool hasAssets = db.PRODUCTION_ASSETS.Any(a => a.id_category == id);
                if (hasAssets)
                {
                    MessageBox.Show("Невозможно удалить категорию, так как есть активы с этой категорией",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var item = db.CATEGORY.Find(id);
                if (item != null)
                {
                    db.CATEGORY.Remove(item);
                    db.SaveChanges();
                    LoadAdminData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 🔹 Статусы
        private void AddStatus_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtNewStatus.Text))
            {
                MessageBox.Show("Введите название статуса", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                db.STATUSASSETS.Add(new STATUSASSETS { Status = TxtNewStatus.Text.Trim() });
                db.SaveChanges();
                TxtNewStatus.Clear();
                LoadAdminData();
                MessageBox.Show("Статус добавлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteStatus_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not int id) return;

            try
            {
                bool hasAssets = db.PRODUCTION_ASSETS.Any(a => a.status == id);
                if (hasAssets)
                {
                    MessageBox.Show("Невозможно удалить статус, так как есть активы с этим статусом",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var item = db.STATUSASSETS.Find(id); // ✅ Find работает с ID_status
                if (item != null)
                {
                    db.STATUSASSETS.Remove(item);
                    db.SaveChanges();
                    LoadAdminData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void ListSuppliers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListSuppliers.SelectedItem is SUPPLIERS supplier)
            {
                try
                {
                    var assets = db.PRODUCTION_ASSETS
                        .Where(a => a.supplier_id == supplier.id)
                        .ToList();
                    SupplierAssetsList.ItemsSource = assets;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AddSupplier_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функция добавления поставщика будет реализована позже", "Информация",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UnlinkAsset_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not int assetId) return;

            try
            {
                var asset = db.PRODUCTION_ASSETS.Find(assetId);
                if (asset != null)
                {
                    asset.supplier_id = null;
                    db.SaveChanges();
                    LoadSuppliers();
                    if (ListSuppliers.SelectedItem is SUPPLIERS s)
                        ListSuppliers_SelectionChanged(null, null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Перезагрузка данных при переключении вкладок
            if (MainTabs.SelectedItem == TabAssets)
                LoadAssets();
            if (MainTabs.SelectedItem == TabAdmin && _isAdmin)
                LoadAdminData();
            if (MainTabs.SelectedItem == TabSuppliers)
                LoadSuppliers();
        }

        // 🔹 Освобождение ресурсов
        public void Dispose()
        {
            db?.Dispose();
        }
    }
}