using EnterpriseAssets.Model.DataBase;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EnterpriseAssets.View
{
    public partial class PurchaseManage : Window
    {
        private DB_AssetManage db = new DB_AssetManage();
        private EQUIPMENT_PURCHASES _currentPurchase;
        private bool _isNewPurchase;
        private bool _isViewOnly;

        public PurchaseManage()
        {
            InitializeComponent();
            _currentPurchase = new EQUIPMENT_PURCHASES();
            _isNewPurchase = true;
            _isViewOnly = false;
            InitializeForm();
        }

        public PurchaseManage(EQUIPMENT_PURCHASES purchase, bool isViewOnly = false)
        {
            InitializeComponent();
            _currentPurchase = purchase;
            _isNewPurchase = false;
            _isViewOnly = isViewOnly;
            InitializeForm();
            LoadPurchaseData();
        }

        private void InitializeForm()
        {
            if (_isViewOnly)
            {
                WindowTitle.Text = "Просмотр закупки";
                SetReadOnly(true);
                BtnDelete.Visibility = Visibility.Collapsed;
                BtnSave.Content = "Закрыть";
            }
            else if (_isNewPurchase)
            {
                WindowTitle.Text = "Новая закупка оборудования";
                BtnDelete.Visibility = Visibility.Collapsed;
                CreatedAtSection.Visibility = Visibility.Collapsed;
                DpOrderDate.SelectedDate = DateTime.Now;
            }
            else
            {
                WindowTitle.Text = "Редактирование закупки";
                BtnDelete.Visibility = Visibility.Visible;
                CreatedAtSection.Visibility = Visibility.Visible;
            }

            LoadAssets();
            LoadSuppliers();
            LoadManagers();
            LoadStatuses();
        }

        private void SetReadOnly(bool isReadOnly)
        {
            CmbEquipment.IsEnabled = !isReadOnly;
            CmbSupplier.IsEnabled = !isReadOnly;
            TxtQuantity.IsEnabled = !isReadOnly;
            TxtUnitPrice.IsEnabled = !isReadOnly;
            DpOrderDate.IsEnabled = !isReadOnly;
            DpExpectedDelivery.IsEnabled = !isReadOnly;
            DpActualDelivery.IsEnabled = !isReadOnly;
            CmbManager.IsEnabled = !isReadOnly;
            CmbStatus.IsEnabled = !isReadOnly;
            TxtNotes.IsEnabled = !isReadOnly;
            BtnSave.Visibility = isReadOnly ? Visibility.Collapsed : Visibility.Visible;
        }

        private void LoadAssets()
        {
            try
            {
                // 🔹 Шаг 1: Загружаем данные из БД (БЕЗ форматирования)
                var assets = db.PRODUCTION_ASSETS
                    .Include("ASSETTYPE")
                    .Where(a => a.name != null)
                    .ToList();  // ✅ Сначала загружаем в память

                // 🔹 Шаг 2: Форматируем УЖЕ в памяти
                var assetsWithDisplay = assets.Select(a => new
                {
                    Id = a.id,
                    DisplayName = $"{a.name} ({GetAssetTypeShortName(a.ASSETTYPE?.AssetType1)})",
                    FullType = a.ASSETTYPE?.AssetType1
                }).ToList();

                CmbEquipment.ItemsSource = assetsWithDisplay;
                CmbEquipment.DisplayMemberPath = "DisplayName";
                CmbEquipment.SelectedValuePath = "Id";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки активов: {ex.Message}", "Ошибка");
            }
        }

        private string GetAssetTypeShortName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return "Актив";

            var name = typeName.Trim();
            if (name.Length > 10) name = name.Substring(0, 10);

            return name switch
            {
                "Оборудование" => "Оборуд.",
                "Материалы" => "Матер.",
                "Запчасти" => "Запчасть",
                _ => name
            };
        }

        private void LoadSuppliers()
        {
            var suppliers = db.SUPPLIERS.OrderBy(s => s.name).ToList();
            CmbSupplier.ItemsSource = suppliers;
            CmbSupplier.DisplayMemberPath = "name";
            CmbSupplier.SelectedValuePath = "id";
        }

        private void LoadManagers()
        {
            // 🔹 Ищем роль "Менеджер" с явной проверкой на null
            var masterRole = db.ROLES
                .FirstOrDefault(r => r.name != null && r.name.Trim().ToLower() == "менеджер");

            if (masterRole != null)
            {
                var managers = db.USERS
                    .Where(u => u.role_id == masterRole.id)
                    .Join(db.MASTERS,
                        u => u.id,
                        m => m.user_id,
                        (u, m) => new
                        {
                            MasterId = m.id,
                            FullName = u.full_name != null ? u.full_name : u.username
                        })
                    .OrderBy(m => m.FullName)
                    .ToList();

                var items = new List<dynamic>();
                items.Add(new { MasterId = (int?)null, FullName = "— Не назначен —" });
                items.AddRange(managers);

                CmbManager.ItemsSource = items;
                CmbManager.DisplayMemberPath = "FullName";
                CmbManager.SelectedValuePath = "MasterId";
            }
        }

        private void LoadStatuses()
        {
            var statuses = db.STATUS_PURCHASE.OrderBy(s => s.Status).ToList();
            CmbStatus.ItemsSource = statuses;
            CmbStatus.DisplayMemberPath = "Status";
            CmbStatus.SelectedValuePath = "ID_status";
        }

        private void LoadPurchaseData()
        {
            if (_currentPurchase == null) return;

            CmbEquipment.SelectedValue = _currentPurchase.asset_id;
            CmbSupplier.SelectedValue = _currentPurchase.supplier_id;
            TxtQuantity.Text = _currentPurchase.quantity?.ToString();
            TxtUnitPrice.Text = _currentPurchase.unit_price?.ToString("F2");
            UpdateTotalCost();

            if (_currentPurchase.order_date.HasValue) DpOrderDate.SelectedDate = _currentPurchase.order_date.Value;
            if (_currentPurchase.expected_delivery.HasValue) DpExpectedDelivery.SelectedDate = _currentPurchase.expected_delivery.Value;
            if (_currentPurchase.actual_delivery.HasValue) DpActualDelivery.SelectedDate = _currentPurchase.actual_delivery.Value;

            CmbManager.SelectedValue = _currentPurchase.purchase_manager_id;
            CmbStatus.SelectedValue = _currentPurchase.status;
            TxtNotes.Text = _currentPurchase.notes;

            if (_currentPurchase.created_at.HasValue)
                TxtCreatedAt.Text = $"Создано: {_currentPurchase.created_at:dd.MM.yyyy HH:mm}";
        }

        private void CmbEquipment_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Можно авто-подставить поставщика по умолчанию из оборудования
        }

        private void TxtQuantityOrPrice_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateTotalCost();
        }

        private void UpdateTotalCost()
        {
            if (decimal.TryParse(TxtQuantity.Text, out decimal qty) &&
                decimal.TryParse(TxtUnitPrice.Text, out decimal price))
            {
                var total = qty * price;
                TxtTotalCost.Text = $"{total:C}";
            }
            else
            {
                TxtTotalCost.Text = "0 ₽";
            }
        }

        private void TxtNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[0-9]+$");
        }

        private void TxtDecimal_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[0-9]*\.?[0-9]{0,2}$");
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_isViewOnly) { DialogResult = false; Close(); return; }

            try
            {
                // 🔹 Валидация
                if (CmbEquipment.SelectedValue == null)
                {
                    MessageBox.Show("Выберите актив", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (CmbSupplier.SelectedValue == null)
                {
                    MessageBox.Show("Выберите поставщика", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(TxtQuantity.Text, out int qty) || qty <= 0)
                {
                    MessageBox.Show("Введите корректное количество (> 0)", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(TxtUnitPrice.Text, out decimal price) || price <= 0)
                {
                    MessageBox.Show("Введите корректную цену (> 0)", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 🔹 Заполнение данных
                _currentPurchase.asset_id = CmbEquipment.SelectedValue as int?;
                _currentPurchase.supplier_id = CmbSupplier.SelectedValue as int?;
                _currentPurchase.quantity = qty;
                _currentPurchase.unit_price = price;
                _currentPurchase.total_cost = qty * price;
                _currentPurchase.order_date = DpOrderDate.SelectedDate;
                _currentPurchase.expected_delivery = DpExpectedDelivery.SelectedDate;
                _currentPurchase.actual_delivery = DpActualDelivery.SelectedDate;
                _currentPurchase.purchase_manager_id = CmbManager.SelectedValue as int?;
                _currentPurchase.status = CmbStatus.SelectedValue as int?;
                _currentPurchase.notes = string.IsNullOrWhiteSpace(TxtNotes.Text) ? null : TxtNotes.Text.Trim();

                if (_isNewPurchase)
                {
                    _currentPurchase.purchase_number = $"PUR-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
                    _currentPurchase.created_at = DateTime.Now;

                    System.Diagnostics.Debug.WriteLine($"🔹 Creating new purchase:");
                    System.Diagnostics.Debug.WriteLine($"   asset_id: {_currentPurchase.asset_id}");
                    System.Diagnostics.Debug.WriteLine($"   supplier_id: {_currentPurchase.supplier_id}");
                    System.Diagnostics.Debug.WriteLine($"   quantity: {_currentPurchase.quantity}");
                    System.Diagnostics.Debug.WriteLine($"   unit_price: {_currentPurchase.unit_price}");
                    System.Diagnostics.Debug.WriteLine($"   total_cost: {_currentPurchase.total_cost}");
                    System.Diagnostics.Debug.WriteLine($"   purchase_number: {_currentPurchase.purchase_number}");

                    db.EQUIPMENT_PURCHASES.Add(_currentPurchase);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"🔹 Updating purchase ID: {_currentPurchase.id}");
                    db.Entry(_currentPurchase).State = EntityState.Modified;
                }

                db.SaveChanges();

                System.Diagnostics.Debug.WriteLine("✅ Purchase saved successfully!");

                MessageBox.Show("Закупка успешно сохранена", "Успех",
                               MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
            {
                // 🔹 Детальная информация об ошибках валидации EF
                var errors = new System.Text.StringBuilder();
                errors.AppendLine("Ошибки валидации данных:\n");

                foreach (var validationErrors in dbEx.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        errors.AppendLine($"Свойство: {validationError.PropertyName}");
                        errors.AppendLine($"Ошибка: {validationError.ErrorMessage}\n");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"❌ DbEntityValidationException:\n{errors}");

                MessageBox.Show(errors.ToString(), "Ошибка валидации данных",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException dbUpdateEx)
            {
                // 🔹 Ошибка обновления БД (foreign key, constraints, etc.)
                var errorMessage = new System.Text.StringBuilder();
                errorMessage.AppendLine("Ошибка сохранения в базу данных:\n");
                errorMessage.AppendLine(dbUpdateEx.Message);

                if (dbUpdateEx.InnerException != null)
                {
                    errorMessage.AppendLine("\nВнутренняя ошибка:");
                    errorMessage.AppendLine(dbUpdateEx.InnerException.Message);

                    if (dbUpdateEx.InnerException.InnerException != null)
                    {
                        errorMessage.AppendLine("\nДетали:");
                        errorMessage.AppendLine(dbUpdateEx.InnerException.InnerException.Message);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"❌ DbUpdateException:\n{errorMessage}");

                MessageBox.Show(errorMessage.ToString(), "Ошибка сохранения",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                // 🔹 Другие ошибки
                var errorMsg = $"Произошла ошибка:\n\n{ex.Message}\n\nСтек вызовов:\n{ex.StackTrace}";

                System.Diagnostics.Debug.WriteLine($"❌ General Exception:\n{errorMsg}");

                MessageBox.Show(errorMsg, "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_isNewPurchase || _isViewOnly) return;
            var result = MessageBox.Show("Удалить закупку?", "Подтверждение", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                try { db.EQUIPMENT_PURCHASES.Remove(_currentPurchase); db.SaveChanges(); DialogResult = true; Close(); }
                catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка"); }
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
        protected override void OnClosed(EventArgs e) { db?.Dispose(); base.OnClosed(e); }
    }
}