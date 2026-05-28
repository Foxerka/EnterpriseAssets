using System;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Validation;
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

        public AssetManage()
        {
            InitializeComponent();
            _currentAsset = new PRODUCTION_ASSETS();
            _isNewAsset = true;
            WindowTitle.Text = "Добавление актива";
            BtnDelete.Visibility = Visibility.Collapsed;
            CreatedAtSection.Visibility = Visibility.Collapsed;
            InitializeData();
            SetupInputMasks();
        }

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
            SetupInputMasks();
        }

        private void SetupInputMasks()
        {
            TxtSerialNumber.PreviewTextInput += (s, e) =>
            {
                e.Handled = !Regex.IsMatch(e.Text, @"^[a-zA-Z0-9\-_]+$");
            };
            TxtSerialNumber.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Space) e.Handled = true;
            };

            TxtName.PreviewTextInput += (s, e) =>
            {
                e.Handled = !Regex.IsMatch(e.Text, @"^[a-zA-Zа-яА-Я0-9\s\-\.\(\)]+$");
            };
        }

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
                ShowError($"Ошибка загрузки типов активов: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки категорий: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки единиц: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки цехов: {ex.Message}");
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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки поставщиков: {ex.Message}");
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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки статусов: {ex.Message}");
            }
        }

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
                TxtCreatedAt.Text = $"📅 Создан: {_currentAsset.created_at:dd.MM.yyyy HH:mm}";
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

        private void TxtQuantity_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[0-9]+$");
        }

        private void TxtDecimal_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            string currentText = textBox?.Text ?? "";

            if (!Regex.IsMatch(e.Text, @"^[0-9\.]$"))
            {
                e.Handled = true;
                return;
            }

            if (e.Text == "." && currentText.Contains("."))
            {
                e.Handled = true;
                return;
            }

            if (currentText.Contains("."))
            {
                int dotIndex = currentText.IndexOf('.');
                int decimalDigits = currentText.Length - dotIndex - 1;
                if (decimalDigits >= 2 && textBox.SelectionStart > dotIndex)
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(TxtName.Text))
            {
                ShowWarning("Введите название актива");
                TxtName.Focus();
                return false;
            }

            if (!decimal.TryParse(TxtQuantity.Text, out decimal quantity) || quantity < 0)
            {
                ShowWarning("Введите корректное количество (≥ 0)");
                TxtQuantity.Focus();
                return false;
            }

            if (!string.IsNullOrWhiteSpace(TxtMinQuantity.Text) && (!decimal.TryParse(TxtMinQuantity.Text, out decimal minQty) || minQty < 0))
            {
                ShowWarning("Введите корректное минимальное количество");
                TxtMinQuantity.Focus();
                return false;
            }

            if (CmbAssetType.SelectedItem == null)
            {
                ShowWarning("Выберите тип актива");
                CmbAssetType.Focus();
                return false;
            }

            return true;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInputs())
                    return;

                // 🔹 ИСПРАВЛЕНИЕ: Для существующего актива загружаем свежую копию из БД
                if (!_isNewAsset)
                {
                    var existingAsset = db.PRODUCTION_ASSETS
                        .FirstOrDefault(a => a.id == _currentAsset.id);

                    if (existingAsset == null)
                    {
                        ShowError("Актив не найден в базе данных");
                        return;
                    }

                    // Обновляем свойства существующего объекта
                    existingAsset.name = TxtName.Text.Trim();
                    existingAsset.description = string.IsNullOrWhiteSpace(TxtDescription.Text) ? null : TxtDescription.Text.Trim();
                    existingAsset.serial_number = string.IsNullOrWhiteSpace(TxtSerialNumber.Text) ? null : TxtSerialNumber.Text.Trim();
                    existingAsset.quantity = decimal.Parse(TxtQuantity.Text);
                    existingAsset.min_quantity = string.IsNullOrWhiteSpace(TxtMinQuantity.Text) ? (decimal?)null : decimal.Parse(TxtMinQuantity.Text);
                    existingAsset.warehouse_location = string.IsNullOrWhiteSpace(TxtWarehouseLocation.Text) ? null : TxtWarehouseLocation.Text.Trim();

                    // Обработка стоимости - заменяем запятую на точку
                    string purchaseCost = TxtPurchaseCost.Text.Replace(',', '.');
                    string currentValue = TxtCurrentValue.Text.Replace(',', '.');

                    existingAsset.purchase_cost = string.IsNullOrWhiteSpace(purchaseCost) ? (decimal?)null : decimal.Parse(purchaseCost, System.Globalization.CultureInfo.InvariantCulture);
                    existingAsset.current_value = string.IsNullOrWhiteSpace(currentValue) ? (decimal?)null : decimal.Parse(currentValue, System.Globalization.CultureInfo.InvariantCulture);
                    existingAsset.purchase_date = DpPurchaseDate.SelectedDate;

                    existingAsset.asset_type = CmbAssetType.SelectedValue as int?;
                    existingAsset.id_category = CmbCategory.SelectedValue as int?;
                    existingAsset.unit = CmbUnit.SelectedValue as int?;
                    existingAsset.workshop_id = CmbWorkshop.SelectedValue as int?;
                    existingAsset.supplier_id = CmbSupplier.SelectedValue as int?;
                    existingAsset.status = CmbStatus.SelectedValue as int?;

                    // Помечаем как изменённый
                    db.Entry(existingAsset).State = EntityState.Modified;
                }
                else
                {
                    // Для нового актива создаём объект
                    _currentAsset.name = TxtName.Text.Trim();
                    _currentAsset.description = string.IsNullOrWhiteSpace(TxtDescription.Text) ? null : TxtDescription.Text.Trim();
                    _currentAsset.serial_number = string.IsNullOrWhiteSpace(TxtSerialNumber.Text) ? null : TxtSerialNumber.Text.Trim();
                    _currentAsset.quantity = decimal.Parse(TxtQuantity.Text);
                    _currentAsset.min_quantity = string.IsNullOrWhiteSpace(TxtMinQuantity.Text) ? (decimal?)null : decimal.Parse(TxtMinQuantity.Text);
                    _currentAsset.warehouse_location = string.IsNullOrWhiteSpace(TxtWarehouseLocation.Text) ? null : TxtWarehouseLocation.Text.Trim();

                    // Обработка стоимости
                    string purchaseCost = TxtPurchaseCost.Text.Replace(',', '.');
                    string currentValue = TxtCurrentValue.Text.Replace(',', '.');

                    _currentAsset.purchase_cost = string.IsNullOrWhiteSpace(purchaseCost) ? (decimal?)null : decimal.Parse(purchaseCost, System.Globalization.CultureInfo.InvariantCulture);
                    _currentAsset.current_value = string.IsNullOrWhiteSpace(currentValue) ? (decimal?)null : decimal.Parse(currentValue, System.Globalization.CultureInfo.InvariantCulture);
                    _currentAsset.purchase_date = DpPurchaseDate.SelectedDate;

                    _currentAsset.asset_type = CmbAssetType.SelectedValue as int?;
                    _currentAsset.id_category = CmbCategory.SelectedValue as int?;
                    _currentAsset.unit = CmbUnit.SelectedValue as int?;
                    _currentAsset.workshop_id = CmbWorkshop.SelectedValue as int?;
                    _currentAsset.supplier_id = CmbSupplier.SelectedValue as int?;
                    _currentAsset.status = CmbStatus.SelectedValue as int?;
                    _currentAsset.created_at = DateTime.Now;

                    db.PRODUCTION_ASSETS.Add(_currentAsset);
                }

                db.SaveChanges();
                ShowSuccess("Актив успешно сохранён");
                DialogResult = true;
                Close();
            }
            catch (DbEntityValidationException ex)
            {
                var errors = string.Join("\n", ex.EntityValidationErrors
                    .SelectMany(ev => ev.ValidationErrors)
                    .Select(v => $"{v.PropertyName}: {v.ErrorMessage}"));
                ShowError($"Ошибка валидации:\n{errors}");
            }
            catch (FormatException ex)
            {
                ShowError($"Ошибка формата числа: {ex.Message}\n\nИспользуйте точку вместо запятой для десятичных чисел.");
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка сохранения: {ex.GetBaseException().Message}");
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_isNewAsset || _currentAsset.id <= 0)
            {
                ShowWarning("Нельзя удалить актив, который ещё не сохранён");
                return;
            }

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить актив «{_currentAsset.name}»?\n\nЭто действие нельзя отменить.",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                // Перезагружаем актив из БД для проверки
                var assetToDelete = db.PRODUCTION_ASSETS
                    .Include("WORK_ACTS")
                    .Include("WORK_ACTS_MATERIALS")
                    .FirstOrDefault(a => a.id == _currentAsset.id);

                if (assetToDelete == null)
                {
                    ShowError("Актив не найден в базе данных");
                    return;
                }

                // Проверяем использование в WORK_ACTS
                var workActs = db.WORK_ACTS.Where(wa => wa.asset_id == assetToDelete.id).ToList();
                bool hasDirectWorkActs = workActs.Any();

                // Проверяем использование в WORK_ACTS_MATERIALS
                var materialUsage = db.WORK_ACTS_MATERIALS.Where(wam => wam.asset_id == assetToDelete.id).ToList();
                bool hasMaterialUsage = materialUsage.Any();

                if (hasDirectWorkActs || hasMaterialUsage)
                {
                    var reasons = new System.Text.StringBuilder();
                    reasons.AppendLine("❌ Невозможно удалить актив, так как он используется:\n");
                    if (hasDirectWorkActs)
                    {
                        reasons.AppendLine($"• Акты выполненных работ: {workActs.Count} записей");
                        foreach (var act in workActs.Take(5))
                        {
                            reasons.AppendLine($"  - Акт №{act.act_number} от {act.work_date:dd.MM.yyyy}");
                        }
                        if (workActs.Count > 5) reasons.AppendLine($"  ... и еще {workActs.Count - 5} записей");
                    }
                    if (hasMaterialUsage)
                    {
                        reasons.AppendLine($"• Использован в работах как материал: {materialUsage.Count} записей");
                    }
                    reasons.AppendLine("\n💡 Решение: сначала удалите или измените связанные записи.");
                    ShowError(reasons.ToString());
                    return;
                }

                db.PRODUCTION_ASSETS.Remove(assetToDelete);
                db.SaveChanges();
                ShowSuccess("✅ Актив успешно удалён");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError($"❌ Ошибка удаления: {ex.GetBaseException().Message}");
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ShowWarning(string message)
        {
            MessageBox.Show(message, "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ShowSuccess(string message)
        {
            MessageBox.Show(message, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        protected override void OnClosed(EventArgs e)
        {
            db?.Dispose();
            base.OnClosed(e);
        }
    }
}