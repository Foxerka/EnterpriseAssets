using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EnterpriseAssets.Model.DataBase;

namespace EnterpriseAssets.View
{
    public partial class AssetManage : Window
    {
        private DB_AssetManage db = new DB_AssetManage();
        private PRODUCTION_ASSETS _currentAsset;
        private bool _isNewAsset;

        // 🔹 Конструктор для нового актива
        public AssetManage()
        {
            InitializeComponent();
            _currentAsset = new PRODUCTION_ASSETS();
            _isNewAsset = true;
            WindowTitle.Text = "Добавление актива";
            BtnDelete.Visibility = Visibility.Collapsed;
            CreatedAtSection.Visibility = Visibility.Collapsed;
            InitializeData();
        }

        // 🔹 Конструктор для редактирования
        public AssetManage(PRODUCTION_ASSETS asset)
        {
            InitializeComponent();
            _currentAsset = asset;
            _isNewAsset = false;
            WindowTitle.Text = "Редактирование актива";
            BtnDelete.Visibility = Visibility.Visible;
            CreatedAtSection.Visibility = Visibility.Visible;
            InitializeData();
            LoadAssetData();
        }

        // 🔹 Загрузка справочников в ComboBox
        private void InitializeData()
        {
            LoadAssetTypes();
            LoadCategories();
            LoadUnits();
            LoadWorkshops();
            LoadSuppliers();
            LoadStatuses();
        }

        private void LoadAssetTypes()
        {
            try
            {
                var types = db.ASSETTYPE.OrderBy(t => t.AssetType1).ToList();

                CmbAssetType.ItemsSource = types;
                CmbAssetType.DisplayMemberPath = "AssetType1";
                CmbAssetType.SelectedValuePath = "ID_ASSETTYPE";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка загрузки типов: {ex.Message}");
                MessageBox.Show($"Ошибка загрузки типов активов: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadCategories()
        {
            try
            {
                var categories = db.CATEGORY.OrderBy(c => c.Category1).ToList();

                CmbCategory.ItemsSource = categories;
                CmbCategory.DisplayMemberPath = "Category1";
                CmbCategory.SelectedValuePath = "ID_category";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка загрузки категорий: {ex.Message}");
            }
        }

        private void LoadUnits()
        {
            try
            {
                var units = db.Unit.OrderBy(u => u.unit1).ToList();

                CmbUnit.ItemsSource = units;
                CmbUnit.DisplayMemberPath = "unit1";
                CmbUnit.SelectedValuePath = "ID";


            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка загрузки единиц: {ex.Message}");
            }
        }

        private void LoadWorkshops()
        {
            try
            {
                var workshops = db.WORKSHOPS.OrderBy(w => w.name).ToList();

                CmbWorkshop.ItemsSource = workshops;
                CmbWorkshop.DisplayMemberPath = "name";
                CmbWorkshop.SelectedValuePath = "id";

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка загрузки цехов: {ex.Message}");
            }
        }

        private void LoadSuppliers()
        {
            try
            {
                var suppliers = db.SUPPLIERS.OrderBy(s => s.name).ToList();

                CmbSupplier.ItemsSource = suppliers;
                CmbSupplier.DisplayMemberPath = "name";
                CmbSupplier.SelectedValuePath = "id";

                System.Diagnostics.Debug.WriteLine($"✅ Поставщики загружены: {suppliers.Count} записей");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка загрузки поставщиков: {ex.Message}");
            }
        }

        private void LoadStatuses()
        {
            try
            {
                var statuses = db.STATUSASSETS.OrderBy(s => s.Status).ToList();

                CmbStatus.ItemsSource = statuses;
                CmbStatus.DisplayMemberPath = "Status";
                CmbStatus.SelectedValuePath = "ID_status";

                System.Diagnostics.Debug.WriteLine($"✅ Статусы загружены: {statuses.Count} записей");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка загрузки статусов: {ex.Message}");
            }
        }

        // 🔹 Заполнение полей данными актива (для редактирования)
        private void LoadAssetData()
        {
            if (_currentAsset == null) return;

            TxtName.Text = _currentAsset.name;
            TxtDescription.Text = _currentAsset.description;
            TxtSerialNumber.Text = _currentAsset.serial_number;
            TxtQuantity.Text = _currentAsset.quantity?.ToString();
            TxtMinQuantity.Text = _currentAsset.min_quantity?.ToString();
            TxtWarehouseLocation.Text = _currentAsset.warehouse_location;
            TxtPurchaseCost.Text = _currentAsset.purchase_cost?.ToString("F2");
            TxtCurrentValue.Text = _currentAsset.current_value?.ToString("F2");

            if (_currentAsset.purchase_date.HasValue)
                DpPurchaseDate.SelectedDate = _currentAsset.purchase_date.Value;

            CmbAssetType.SelectedValue = _currentAsset.asset_type;
            CmbCategory.SelectedValue = _currentAsset.id_category;
            CmbUnit.SelectedValue = _currentAsset.unit;
            CmbWorkshop.SelectedValue = _currentAsset.workshop_id;
            CmbSupplier.SelectedValue = _currentAsset.supplier_id;
            CmbStatus.SelectedValue = _currentAsset.status;

            if (_currentAsset.created_at.HasValue)
            {
                TxtCreatedAt.Text = $"Создан: {_currentAsset.created_at:dd.MM.yyyy HH:mm}";
            }

            UpdateStatusVisibility();
        }

        private void UpdateStatusVisibility()
        {
            if (CmbAssetType.SelectedItem is ASSETTYPE selectedType)
            {
                if (string.Equals(selectedType.AssetType1?.Trim(), "Оборудование", StringComparison.OrdinalIgnoreCase))
                {
                    StatusSection.Visibility = Visibility.Visible;
                    return;
                }
            }
            StatusSection.Visibility = Visibility.Collapsed;
            CmbStatus.SelectedValue = null;
        }

        private void CmbAssetType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateStatusVisibility();
        }

        // 🔹 Валидация: только целые числа (для количества)
        private void TxtQuantity_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[0-9]+$");
        }

        // 🔹 Валидация: числа с точкой (для стоимости)
        private void TxtDecimal_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[0-9]*\.?[0-9]{0,2}$");
        }

        // 🔹 Сохранение актива
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация
                if (string.IsNullOrWhiteSpace(TxtName.Text))
                {
                    MessageBox.Show("Введите название актива", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtName.Focus();
                    return;
                }

                if (!decimal.TryParse(TxtQuantity.Text, out decimal quantity) || quantity < 0)
                {
                    MessageBox.Show("Введите корректное количество (≥ 0)", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtQuantity.Focus();
                    return;
                }

                // Заполнение свойств
                _currentAsset.name = TxtName.Text.Trim();
                _currentAsset.description = string.IsNullOrWhiteSpace(TxtDescription.Text) ? null : TxtDescription.Text.Trim();
                _currentAsset.serial_number = string.IsNullOrWhiteSpace(TxtSerialNumber.Text) ? null : TxtSerialNumber.Text.Trim();

                _currentAsset.quantity = quantity;
                _currentAsset.min_quantity = string.IsNullOrWhiteSpace(TxtMinQuantity.Text)
                    ? (decimal?)null : decimal.Parse(TxtMinQuantity.Text);

                _currentAsset.warehouse_location = string.IsNullOrWhiteSpace(TxtWarehouseLocation.Text)
                    ? null : TxtWarehouseLocation.Text.Trim();

                _currentAsset.purchase_cost = string.IsNullOrWhiteSpace(TxtPurchaseCost.Text)
                    ? (decimal?)null : decimal.Parse(TxtPurchaseCost.Text);

                _currentAsset.current_value = string.IsNullOrWhiteSpace(TxtCurrentValue.Text)
                    ? (decimal?)null : decimal.Parse(TxtCurrentValue.Text);

                _currentAsset.purchase_date = DpPurchaseDate.SelectedDate;

                // ✅ Безопасное получение SelectedValue
                _currentAsset.asset_type = CmbAssetType.SelectedValue is int at ? at : (int?)null;
                _currentAsset.id_category = CmbCategory.SelectedValue is int cat ? cat : (int?)null;
                _currentAsset.unit = CmbUnit.SelectedValue is int u ? u : (int?)null;
                _currentAsset.workshop_id = CmbWorkshop.SelectedValue is int w ? w : (int?)null;
                _currentAsset.supplier_id = CmbSupplier.SelectedValue is int s ? s : (int?)null;
                _currentAsset.status = CmbStatus.SelectedValue is int st ? st : (int?)null;

                if (_isNewAsset)
                {
                    _currentAsset.created_at = DateTime.Now;
                    db.PRODUCTION_ASSETS.Add(_currentAsset);
                }
                else
                {
                    // ✅ Для редактирования: прикрепляем и помечаем как изменённый
                    var tracked = db.PRODUCTION_ASSETS.Local.FirstOrDefault(a => a.id == _currentAsset.id);
                    if (tracked != null)
                    {
                        db.Entry(tracked).CurrentValues.SetValues(_currentAsset);
                    }
                    else
                    {
                        db.PRODUCTION_ASSETS.Attach(_currentAsset);
                        db.Entry(_currentAsset).State = EntityState.Modified;
                    }
                }

                db.SaveChanges();

                MessageBox.Show("Актив успешно сохранён", "Успех",
                              MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                var errors = string.Join("\n", ex.EntityValidationErrors
                    .SelectMany(ev => ev.ValidationErrors)
                    .Select(v => $"{v.PropertyName}: {v.ErrorMessage}"));
                MessageBox.Show($"Ошибка валидации:\n{errors}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.GetBaseException().Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 🔹 Удаление актива с проверкой зависимостей
        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            // Нельзя удалить несуществующую запись
            if (_isNewAsset || _currentAsset.id <= 0)
            {
                MessageBox.Show("Нельзя удалить актив, который ещё не сохранён", "Предупреждение",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить актив «{_currentAsset.name}»?\n\n" +
                $"Это действие нельзя отменить.",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                // Находим актуальную запись в БД
                var assetToDelete = db.PRODUCTION_ASSETS
                    .FirstOrDefault(a => a.id == _currentAsset.id);

                if (assetToDelete == null)
                {
                    MessageBox.Show("Актив не найден в базе данных", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 🔍 Проверка зависимостей в WORK_ACTS (прямая ссылка asset_id)
                bool hasDirectWorkActs = db.WORK_ACTS.Any(wa => wa.asset_id == assetToDelete.id);

                // 🔍 Проверка зависимостей в WORK_ACTS_MATERIALS (расход материалов)
                bool hasMaterialUsage = db.WORK_ACTS_MATERIALS.Any(wam => wam.asset_id == assetToDelete.id);

                if (hasDirectWorkActs || hasMaterialUsage)
                {
                    var reasons = new List<string>();
                    if (hasDirectWorkActs)
                    {
                        var count = db.WORK_ACTS.Count(wa => wa.asset_id == assetToDelete.id);
                        reasons.Add($"• Акты выполненных работ: {count} записей (поле asset_id)");
                    }
                    if (hasMaterialUsage)
                    {
                        var count = db.WORK_ACTS_MATERIALS.Count(wam => wam.asset_id == assetToDelete.id);
                        reasons.Add($"• Использован в работах как материал: {count} записей");
                    }

                    MessageBox.Show(
                        $"❌ Невозможно удалить актив, так как он используется:\n\n" +
                        string.Join("\n", reasons) +
                        $"\n\n💡 Решение: сначала удалите или измените связанные записи в актах работ.",
                        "Ошибка удаления",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                // ✅ Все проверки пройдены — удаляем
                db.PRODUCTION_ASSETS.Remove(assetToDelete);
                db.SaveChanges();

                MessageBox.Show("✅ Актив успешно удалён", "Успех",
                              MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ошибка удаления: {ex.GetBaseException().Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // 🔹 Освобождение ресурсов
        protected override void OnClosed(EventArgs e)
        {
            db?.Dispose();
            base.OnClosed(e);
        }
    }
}