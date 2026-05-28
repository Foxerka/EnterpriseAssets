using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using EnterpriseAssets.Model.DataBase;

namespace EnterpriseAssets.View
{
    public partial class WorkshopManage : Window
    {
        private DB_AssetManage db = new DB_AssetManage();
        private WORKSHOPS _currentWorkshop;
        private bool _isNewWorkshop;

        // Конструктор для существующего места хранения
        public WorkshopManage(WORKSHOPS workshop)
        {
            InitializeComponent();
            _currentWorkshop = workshop;
            _isNewWorkshop = false;
            Title = "Редактирование места хранения";
            WorkshopTitle.Text = workshop.name ?? "Место хранения";
            LoadResponsiblePersons();
            LoadWorkshopData();
            BtnDelete.Visibility = Visibility.Visible;
            CreatedAtSection.Visibility = Visibility.Visible;
        }

        // Конструктор для нового места хранения
        public WorkshopManage()
        {
            InitializeComponent();
            _currentWorkshop = new WORKSHOPS();
            _isNewWorkshop = true;
            Title = "Добавление нового места хранения";
            WorkshopTitle.Text = "Новое место хранения";
            LoadResponsiblePersons();
            BtnDelete.Visibility = Visibility.Collapsed;
            CreatedAtSection.Visibility = Visibility.Collapsed;
        }

        private void LoadResponsiblePersons()
        {
            try
            {
                CmbManager.Items.Clear();

                // Ищем роль "МОЛ" (Материально-ответственное лицо)
                // Если такой роли нет, ищем "Мастер" или создайте роль "МОЛ" в БД
                var molRole = db.ROLES
                    .FirstOrDefault(r => r.name.Trim().ToLower() == "мол" || r.name.Trim().ToLower() == "мастер");

                if (molRole == null)
                {
                    var allRoles = string.Join(", ", db.ROLES.Select(r => $"'{r.name}'"));
                    MessageBox.Show(
                        $"Роль 'МОЛ' не найдена.\nДоступные роли: {allRoles}\n\nСоздайте роль 'МОЛ' в справочнике ролей.",
                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);

                    // Добавляем пустой список, чтобы форма работала
                    CmbManager.ItemsSource = new List<dynamic>();
                    return;
                }

                // Загружаем МОЛ (пользователей с ролью МОЛ)
                var responsiblePersons = db.USERS
                    .Where(u => u.role_id == molRole.id)
                    .Join(db.MASTERS,
                        user => user.id,
                        master => master.user_id,
                        (user, master) => new {
                            MasterId = master.id,
                            FullName = user.full_name,
                            Username = user.username
                        })
                    .OrderBy(m => m.FullName)
                    .ToList()
                    .Select(m => new {
                        MasterId = m.MasterId,
                        DisplayName = string.IsNullOrWhiteSpace(m.FullName)
                            ? m.Username
                            : m.FullName
                    })
                    .ToList();

                // Формируем источник для ComboBox
                var comboBoxItems = new List<dynamic>();
                comboBoxItems.Add(new { MasterId = (int?)null, DisplayName = "— Не назначен —" });
                comboBoxItems.AddRange(responsiblePersons);

                CmbManager.ItemsSource = comboBoxItems;
                CmbManager.SelectedValuePath = "MasterId";
                CmbManager.DisplayMemberPath = "DisplayName";

                // Устанавливаем выбранное значение
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
                MessageBox.Show($"Ошибка загрузки ответственных лиц: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadWorkshopData()
        {
            if (_currentWorkshop == null) return;

            TxtName.Text = _currentWorkshop.name;
            TxtLocation.Text = _currentWorkshop.location;
            WorkshopTitle.Text = _currentWorkshop.name ?? "Место хранения";

            if (_currentWorkshop.manager_id.HasValue)
            {
                CmbManager.SelectedValue = _currentWorkshop.manager_id.Value;
            }
            else
            {
                CmbManager.SelectedIndex = 0;
            }

            if (_currentWorkshop.created_at.HasValue)
            {
                TxtCreatedAt.Text = $"Создано: {_currentWorkshop.created_at:dd.MM.yyyy}";
            }
        }

        private bool ValidateFields()
        {
            if (string.IsNullOrWhiteSpace(TxtName.Text))
            {
                MessageBox.Show("Введите название места хранения", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtName.Focus();
                return false;
            }
            return true;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateFields()) return;

                _currentWorkshop.name = TxtName.Text.Trim();
                _currentWorkshop.location = TxtLocation.Text?.Trim();

                if (CmbManager.SelectedValue is int masterId)
                {
                    _currentWorkshop.manager_id = masterId;
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
                MessageBox.Show("Нельзя удалить место, которое ещё не сохранено", "Предупреждение",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Удалить место хранения «{_currentWorkshop.name}»?\n\nЭто действие нельзя отменить.",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var workshop = db.WORKSHOPS.Find(_currentWorkshop.id);
                if (workshop == null) return;

                bool hasEquipment = db.EQUIPMENT.Any(eq => eq.Workshop_id == workshop.id);
                bool hasAssets = db.PRODUCTION_ASSETS.Any(a => a.workshop_id == workshop.id);

                if (hasEquipment || hasAssets)
                {
                    var reasons = new List<string>();
                    if (hasEquipment) reasons.Add($"• Оборудование: {db.EQUIPMENT.Count(eq => eq.Workshop_id == workshop.id)}");
                    if (hasAssets) reasons.Add($"• Активы: {db.PRODUCTION_ASSETS.Count(a => a.workshop_id == workshop.id)}");

                    MessageBox.Show(
                        $"Нельзя удалить место хранения, к нему привязано:\n{string.Join("\n", reasons)}\n\nСначала переназначьте или удалите связанные записи.",
                        "Ошибка удаления",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                db.WORKSHOPS.Remove(workshop);
                db.SaveChanges();

                MessageBox.Show("Место хранения удалено", "Успех",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.GetBaseException().Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            db?.Dispose();
            base.OnClosed(e);
        }
    }
}