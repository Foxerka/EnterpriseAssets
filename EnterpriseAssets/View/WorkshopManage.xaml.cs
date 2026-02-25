using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Xml.Linq;
using EnterpriseAssets.Model.DataBase;

namespace EnterpriseAssets.View
{
    public partial class WorkshopManage : Window
    {
        private DB_AssetManage db = new DB_AssetManage();
        private WORKSHOPS _currentWorkshop;
        private bool _isNewWorkshop;

        // Конструктор для существующего цеха
        public WorkshopManage(WORKSHOPS workshop)
        {
            InitializeComponent();
            _currentWorkshop = workshop;
            _isNewWorkshop = false;
            Title = "Редактирование цеха";
            LoadManagers();
            LoadWorkshopData();
            BtnDelete.Visibility = Visibility.Visible;
            CreatedAtSection.Visibility = Visibility.Visible;
        }

        // Конструктор для нового цеха
        public WorkshopManage()
        {
            InitializeComponent();
            _currentWorkshop = new WORKSHOPS();
            _isNewWorkshop = true;
            Title = "Добавление нового цеха";
            WorkshopTitle.Text = "Новый цех";
            LoadManagers();
            BtnDelete.Visibility = Visibility.Collapsed;
            CreatedAtSection.Visibility = Visibility.Collapsed;
        }

        private void LoadManagers()
        {
            try
            {
                CmbManager.Items.Clear();
                // 1. Ищем роль "Мастер"
                var masterRole = db.ROLES
                    .FirstOrDefault(r => r.name.Trim().ToLower() == "мастер");

                if (masterRole == null)
                {
                    var allRoles = string.Join(", ", db.ROLES.Select(r => $"'{r.name}'"));
                    MessageBox.Show(
                        $"Роль 'Мастер' не найдена.\nДоступные роли: {allRoles}",
                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 2. Загружаем мастеров БЕЗ использования IsNullOrWhiteSpace в LINQ
                var masters = db.USERS
                    .Where(u => u.role_id == masterRole.id)
                    .Join(db.MASTERS,
                        user => user.id,
                        master => master.user_id,
                        (user, master) => new {
                            MasterId = master.id,
                            FullName = user.full_name,
                            Username = user.username
                        })
                    .OrderBy(m => m.FullName)
                    .ToList()  // ← ВАЖНО: загружаем в память ПЕРЕД проверкой
                    .Select(m => new {
                        MasterId = m.MasterId,
                        DisplayName = string.IsNullOrWhiteSpace(m.FullName)
                            ? m.Username
                            : m.FullName
                    })
                    .ToList();

                // 3. Формируем источник для ComboBox
                var comboBoxItems = new List<dynamic>();
                comboBoxItems.Add(new { MasterId = (int?)null, DisplayName = "— Не назначен —" });
                comboBoxItems.AddRange(masters);

                CmbManager.ItemsSource = comboBoxItems;
                CmbManager.SelectedValuePath = "MasterId";
                CmbManager.DisplayMemberPath = "DisplayName";

                // 4. Устанавливаем выбранное значение
                if (!_isNewWorkshop && _currentWorkshop?.manager_id.HasValue == true)
                {
                    CmbManager.SelectedValue = _currentWorkshop.manager_id.Value;
                }
                else
                {
                    CmbManager.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки мастеров: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadWorkshopData()
        {
            if (_currentWorkshop == null) return;

            TxtName.Text = _currentWorkshop.name;
            TxtLocation.Text = _currentWorkshop.location;
            WorkshopTitle.Text = _currentWorkshop.name ?? "Новый цех";

            if (_currentWorkshop.manager_id.HasValue)
            {
                CmbManager.SelectedValue = _currentWorkshop.manager_id.Value;
            }
            else
            {
                CmbManager.SelectedIndex = 0; // "Не назначен"
            }

            if (_currentWorkshop.created_at.HasValue)
            {
                TxtCreatedAt.Text = $"Создан: {_currentWorkshop.created_at:dd.MM.yyyy}";
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TxtName.Text))
                {
                    MessageBox.Show("Введите название цеха", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtName.Focus();
                    return;
                }

                _currentWorkshop.name = TxtName.Text.Trim();
                _currentWorkshop.location = TxtLocation.Text?.Trim();

                // SelectedValue теперь содержит MASTERS.id (или null)
                if (CmbManager.SelectedValue is int masterId)
                {
                    _currentWorkshop.manager_id = masterId;  // ← Сохраняем MASTERS.id
                }
                else
                {
                    _currentWorkshop.manager_id = null;
                }

                if (_isNewWorkshop)
                {
                    _currentWorkshop.created_at = DateTime.Now;
                    db.WORKSHOPS.Add(_currentWorkshop);
                }

                db.SaveChanges();

                MessageBox.Show("Данные успешно сохранены", "Успех",
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

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_isNewWorkshop || _currentWorkshop.id <= 0)
            {
                MessageBox.Show("Нельзя удалить цех, который ещё не сохранён", "Предупреждение",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Удалить цех «{_currentWorkshop.name}»?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var workshop = db.WORKSHOPS.Find(_currentWorkshop.id);
                if (workshop == null) return;

                // ⚠️ Проверьте точные имена свойств в ваших классах!
                // В EQUIPMENT: public Nullable<int> Workshop_id { get; set; }
                // В PRODUCTION_ASSETS: public Nullable<int> workshop_id { get; set; }

                bool hasEquipment = db.EQUIPMENT.Any(eq => eq.Workshop_id == workshop.id);
                bool hasAssets = db.PRODUCTION_ASSETS.Any(a => a.workshop_id == workshop.id);

                if (hasEquipment || hasAssets)
                {
                    var reasons = new List<string>();
                    if (hasEquipment) reasons.Add($"• Оборудование: {db.EQUIPMENT.Count(eq => eq.Workshop_id == workshop.id)}");
                    if (hasAssets) reasons.Add($"• Активы: {db.PRODUCTION_ASSETS.Count(a => a.workshop_id == workshop.id)}");

                    MessageBox.Show(
                        $"Нельзя удалить цех, к нему привязаны:\n{string.Join("\n", reasons)}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                db.WORKSHOPS.Remove(workshop);
                db.SaveChanges();

                MessageBox.Show("Цех удалён", "Успех",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.GetBaseException().Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}