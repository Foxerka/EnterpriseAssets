using EnterpriseAssets.Model.DataBase;
using System;
using System.Collections.Generic;
using System.Data.Common.CommandTrees.ExpressionBuilder;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EnterpriseAssets.View
{
    /// <summary>
    /// Окно добавления / редактирования закупки
    /// </summary>
    public partial class PurchaseManage : Window
    {
        private DB_AssetManage _context;
        private int? _purchaseId; // null = добавление, иначе редактирование
        private int _currentUserId; // ID текущего пользователя (из USERS)
        private int? _currentMasterId; // ID мастера, соответствующего текущему пользователю

        /// <summary>
        /// Конструктор для создания новой закупки
        /// </summary>
        /// <param name="currentUserId">ID текущего пользователя</param>
        public PurchaseManage(int currentUserId) : this()
        {
            _purchaseId = null;
            _currentUserId = currentUserId;
            LoadDataForNew();
        }

        /// <summary>
        /// Конструктор для редактирования закупки
        /// </summary>
        /// <param name="purchaseId">ID закупки</param>
        /// <param name="currentUserId">ID текущего пользователя</param>
        public PurchaseManage(int purchaseId, int currentUserId) : this()
        {
            _purchaseId = purchaseId;
            _currentUserId = currentUserId;
            LoadDataForEdit();
        }

        // Приватный конструктор для инициализации компонентов и контекста
        private PurchaseManage()
        {
            InitializeComponent();
            _context = new DB_AssetManage();
            this.Loaded += (s, e) => {
                // Если создание новой, скрываем кнопку удалить и секцию даты создания
                if (_purchaseId == null)
                {
                    BtnDelete.Visibility = Visibility.Collapsed;
                    CreatedAtSection.Visibility = Visibility.Collapsed;
                }
                else
                {
                    BtnDelete.Visibility = Visibility.Visible;
                    CreatedAtSection.Visibility = Visibility.Visible;
                }
            };
        }

        // Загрузка данных для нового окна
        private void LoadDataForNew()
        {
            try
            {
                // Заполнение справочников
                LoadEquipment();
                LoadSuppliers();
                LoadManagers();
                LoadStatuses();

                // Установка значений по умолчанию
                DpOrderDate.SelectedDate = DateTime.Today;
                // Статус "Черновик" (предполагаем ID=1)
                SelectComboBoxItemByValue(CmbStatus, 1);
                // Менеджер по умолчанию: ищем мастера для текущего пользователя
                SetDefaultManager();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Загрузка данных для редактирования
        private void LoadDataForEdit()
        {
            try
            {
                // Заполнение справочников
                LoadEquipment();
                LoadSuppliers();
                LoadManagers();
                LoadStatuses();

                // Загружаем данные закупки
                var purchase = _context.EQUIPMENT_PURCHASES
                    .Include("EQUIPMENT")
                    .Include("SUPPLIERS")
                    .Include("MASTERS")
                    .Include("STATUS_PURCHASE")
                    .FirstOrDefault(p => p.id == _purchaseId);

                if (purchase == null)
                {
                    MessageBox.Show("Закупка не найдена", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Close();
                    return;
                }

                // Заполнение полей
                WindowTitle.Text = $"Редактирование закупки №{purchase.purchase_number}";
                // Актив
                if (purchase.asset_id.HasValue)
                    SelectComboBoxItemByValue(CmbEquipment, purchase.asset_id.Value);
                // Поставщик
                if (purchase.supplier_id.HasValue)
                    SelectComboBoxItemByValue(CmbSupplier, purchase.supplier_id.Value);
                // Количество
                TxtQuantity.Text = purchase.quantity?.ToString();
                // Цена
                TxtUnitPrice.Text = purchase.unit_price?.ToString();
                // Итог
                UpdateTotalCost();
                // Даты
                if (purchase.order_date.HasValue)
                    DpOrderDate.SelectedDate = purchase.order_date.Value;
                if (purchase.expected_delivery.HasValue)
                    DpExpectedDelivery.SelectedDate = purchase.expected_delivery.Value;
                if (purchase.actual_delivery.HasValue)
                    DpActualDelivery.SelectedDate = purchase.actual_delivery.Value;
                // Менеджер
                if (purchase.purchase_manager_id.HasValue)
                    SelectComboBoxItemByValue(CmbManager, purchase.purchase_manager_id.Value);
                // Статус
                if (purchase.status.HasValue)
                    SelectComboBoxItemByValue(CmbStatus, purchase.status.Value);
                // Примечания
                TxtNotes.Text = purchase.notes;
                // Дата создания (только для просмотра)
                if (purchase.created_at.HasValue)
                    TxtCreatedAt.Text = $"Создано: {purchase.created_at.Value:dd.MM.yyyy HH:mm}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        // Загрузка списка активов
        private void LoadEquipment()
        {
            var equipmentList = _context.EQUIPMENT
                .OrderBy(e => e.equipment_type)
                .ToList();

            CmbEquipment.Items.Clear();
            foreach (var eq in equipmentList)
            {
                string display = $"{eq.equipment_type} ({eq.manufacturer})";
                CmbEquipment.Items.Add(new ComboBoxItem
                {
                    Content = display,
                    Tag = eq.ID
                });
            }
        }

        // Загрузка поставщиков
        private void LoadSuppliers()
        {
            var suppliers = _context.SUPPLIERS
                .Where(s => s.is_active == true || s.is_active == null)
                .OrderBy(s => s.name)
                .ToList();

            CmbSupplier.Items.Clear();
            foreach (var s in suppliers)
            {
                CmbSupplier.Items.Add(new ComboBoxItem
                {
                    Content = s.name,
                    Tag = s.id
                });
            }
        }

        // Загрузка менеджеров (мастеров)
        private void LoadManagers()
        {
            var managers = _context.MASTERS
                .Include("USERS")
                .Where(m => m.USERS != null)
                .OrderBy(m => m.USERS.full_name)
                .ToList();

            CmbManager.Items.Clear();
            foreach (var m in managers)
            {
                CmbManager.Items.Add(new ComboBoxItem
                {
                    Content = m.USERS?.full_name ?? $"Мастер #{m.id}",
                    Tag = m.id
                });
            }
        }

        // Загрузка статусов
        private void LoadStatuses()
        {
            var statuses = _context.STATUS_PURCHASE
                .OrderBy(s => s.ID_status)
                .ToList();

            CmbStatus.Items.Clear();
            foreach (var s in statuses)
            {
                CmbStatus.Items.Add(new ComboBoxItem
                {
                    Content = s.Status,
                    Tag = s.ID_status
                });
            }
        }

        // Установка менеджера по умолчанию (текущий пользователь)
        private void SetDefaultManager()
        {
            // Ищем мастера с user_id == _currentUserId
            var master = _context.MASTERS.FirstOrDefault(m => m.user_id == _currentUserId);
            if (master != null)
            {
                _currentMasterId = master.id;
                SelectComboBoxItemByValue(CmbManager, master.id);
            }
            else
            {
                // Если мастер не найден, выбираем первый элемент (если есть)
                if (CmbManager.Items.Count > 0)
                    CmbManager.SelectedIndex = 0;
            }
        }

        // Вспомогательный метод: выбрать элемент ComboBox по значению Tag
        private void SelectComboBoxItemByValue(ComboBox comboBox, object value)
        {
            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (item.Tag != null && item.Tag.Equals(value))
                {
                    comboBox.SelectedItem = item;
                    break;
                }
            }
        }

        // Обновление итоговой стоимости
        private void UpdateTotalCost()
        {
            if (decimal.TryParse(TxtQuantity.Text, out decimal qty) &&
                decimal.TryParse(TxtUnitPrice.Text, out decimal price))
            {
                decimal total = qty * price;
                TxtTotalCost.Text = $"{total:N2} ₽";
            }
            else
            {
                TxtTotalCost.Text = "0 ₽";
            }
        }

        // Обработчик изменения количества или цены
        private void TxtQuantityOrPrice_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateTotalCost();
        }

        // Ограничение ввода чисел (только цифры) для количества
        private void TxtNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        // Ограничение ввода чисел с десятичной точкой/запятой для цены
        private void TxtDecimal_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем цифры, точку и запятую
            e.Handled = !Regex.IsMatch(e.Text, @"^[\d.,]+$");
        }

        // Обработчик выбора актива (может быть полезно)
        private void CmbEquipment_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Здесь можно, например, подгружать цену по умолчанию, но не обязательно
        }

        // Кнопка "Сохранить"
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Проверка обязательных полей
            if (CmbEquipment.SelectedItem == null)
            {
                MessageBox.Show("Выберите актив", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (CmbSupplier.SelectedItem == null)
            {
                MessageBox.Show("Выберите поставщика", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(TxtQuantity.Text))
            {
                MessageBox.Show("Введите количество", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(TxtUnitPrice.Text))
            {
                MessageBox.Show("Введите цену за единицу", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!decimal.TryParse(TxtQuantity.Text, out decimal quantity) || quantity <= 0)
            {
                MessageBox.Show("Количество должно быть положительным числом", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!decimal.TryParse(TxtUnitPrice.Text, out decimal unitPrice) || unitPrice < 0)
            {
                MessageBox.Show("Цена должна быть неотрицательным числом", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                EQUIPMENT_PURCHASES purchase;

                if (_purchaseId == null)
                {
                    // Новая закупка
                    purchase = new EQUIPMENT_PURCHASES();
                    purchase.purchase_number = GeneratePurchaseNumber();
                    purchase.created_at = DateTime.Now;
                    _context.EQUIPMENT_PURCHASES.Add(purchase);
                }
                else
                {
                    // Редактирование
                    purchase = _context.EQUIPMENT_PURCHASES.Find(_purchaseId);
                    if (purchase == null)
                    {
                        MessageBox.Show("Закупка не найдена", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                // Заполнение полей из формы
                purchase.asset_id = (int)((ComboBoxItem)CmbEquipment.SelectedItem).Tag;
                purchase.supplier_id = (int)((ComboBoxItem)CmbSupplier.SelectedItem).Tag;
                purchase.quantity = (int)quantity; // в базе int? или decimal? В модели int? (quantity int?). Если надо decimal, измените.
                purchase.unit_price = unitPrice;
                purchase.total_cost = quantity * unitPrice;
                purchase.order_date = DpOrderDate.SelectedDate;
                purchase.expected_delivery = DpExpectedDelivery.SelectedDate;
                purchase.actual_delivery = DpActualDelivery.SelectedDate;
                if (CmbManager.SelectedItem != null)
                    purchase.purchase_manager_id = (int)((ComboBoxItem)CmbManager.SelectedItem).Tag;
                if (CmbStatus.SelectedItem != null)
                    purchase.status = (int)((ComboBoxItem)CmbStatus.SelectedItem).Tag;
                purchase.notes = TxtNotes.Text;

                // Сохранить изменения
                _context.SaveChanges();

                // Успех
                MessageBox.Show("Закупка сохранена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true; // для обратной связи
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Генерация номера закупки (пример: PO-20250318-001)
        private string GeneratePurchaseNumber()
        {
            string prefix = "PO";
            string datePart = DateTime.Today.ToString("yyyyMMdd");

            // Диапазон дат: с начала сегодняшнего дня до начала следующего
            DateTime startOfDay = DateTime.Today;
            DateTime endOfDay = startOfDay.AddDays(1);

            var todayPurchases = _context.EQUIPMENT_PURCHASES
                .Where(p => p.created_at >= startOfDay && p.created_at < endOfDay)
                .ToList();

            int count = todayPurchases.Count + 1;
            return $"{prefix}-{datePart}-{count:D3}";
        }

        // Кнопка "Удалить"
        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_purchaseId == null) return;

            var result = MessageBox.Show("Вы действительно хотите удалить эту закупку?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var purchase = _context.EQUIPMENT_PURCHASES.Find(_purchaseId);
                    if (purchase != null)
                    {
                        _context.EQUIPMENT_PURCHASES.Remove(purchase);
                        _context.SaveChanges();
                        MessageBox.Show("Закупка удалена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        this.DialogResult = true;
                        this.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Кнопка "Отмена"
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        // Освобождение ресурсов
        protected override void OnClosed(EventArgs e)
        {
            _context?.Dispose();
            base.OnClosed(e);
        }
    }
}