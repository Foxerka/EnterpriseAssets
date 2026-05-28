using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using EnterpriseAssets.Model.DataBase;
using System.Data.Entity;

namespace EnterpriseAssets.View
{
    public partial class MasterManage : Window
    {
        private DB_AssetManage db = new DB_AssetManage();
        private MASTERS _currentMaster;
        private bool _isNewMaster;

        public MasterManage()
        {
            InitializeComponent();
            _currentMaster = new MASTERS();
            _isNewMaster = true;
            Title = "Назначение нового МОЛ";
            BtnDelete.Visibility = Visibility.Collapsed;
            StatsPanel.Visibility = Visibility.Collapsed;

            LoadData();
            DpHireDate.SelectedDate = DateTime.Today;
        }

        public MasterManage(MASTERS master)
        {
            InitializeComponent();
            _currentMaster = db.MASTERS
                .Include("QUALIFICATION")
                .Include("SPECIALTY1")
                .Include("USERS")
                .FirstOrDefault(m => m.id == master.id);

            if (_currentMaster == null)
            {
                MessageBox.Show("МОЛ не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                DialogResult = false;
                Close();
                return;
            }

            _isNewMaster = false;
            Title = $"Редактирование МОЛ: {_currentMaster.USERS?.full_name ?? "Не указан"}";
            LoadData();
            LoadMasterData();
            LoadStatistics();
        }

        private void LoadData()
        {
            // Получаем ID роли "МОЛ" (если нет такой роли, создайте в БД)
            var molRole = db.ROLES.FirstOrDefault(r => r.name == "МОЛ");
            if (molRole == null)
            {
                // Если нет роли МОЛ, используем роль с id=3 (кладовщик) или создайте новую
                MessageBox.Show("Роль 'МОЛ' не найдена в базе данных. Обратитесь к администратору.",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int molRoleId = molRole.id;

            // Получаем ID пользователей, которые уже являются МОЛ
            var existingMasterIds = db.MASTERS
                .Where(m => m.user_id.HasValue)
                .Select(m => m.user_id.Value)
                .ToList();

            if (!_isNewMaster && _currentMaster.user_id.HasValue)
            {
                existingMasterIds.Remove(_currentMaster.user_id.Value);
            }

            // Загружаем пользователей с ролью "МОЛ"
            var availableUsers = db.USERS
                .Where(u => u.role_id == molRoleId)
                .Where(u => !existingMasterIds.Contains(u.id))
                .OrderBy(u => u.full_name)
                .ToList();

            CmbUsers.ItemsSource = availableUsers;
            CmbUsers.DisplayMemberPath = "full_name";
            CmbUsers.SelectedValuePath = "id";

            // Загрузка справочников
            CmbSpecialty.ItemsSource = db.SPECIALTY.OrderBy(s => s.Speciality).ToList();
            CmbQualification.ItemsSource = db.QUALIFICATION.OrderBy(q => q.Qualification1).ToList();
        }

        private void LoadMasterData()
        {
            if (_currentMaster.USERS != null)
                CmbUsers.SelectedItem = _currentMaster.USERS;

            if (_currentMaster.hire_date.HasValue)
                DpHireDate.SelectedDate = _currentMaster.hire_date.Value;

            ChkIsAvailable.IsChecked = _currentMaster.is_available ?? true;

            if (!string.IsNullOrEmpty(_currentMaster.skill_level))
            {
                var skillItem = CmbSkillLevel.Items.Cast<ComboBoxItem>()
                    .FirstOrDefault(i => i.Tag?.ToString() == _currentMaster.skill_level.ToLower());
                if (skillItem != null)
                    CmbSkillLevel.SelectedItem = skillItem;
            }

            if (_currentMaster.specialty != null)
                CmbSpecialty.SelectedValue = _currentMaster.specialty;

            if (_currentMaster.QUALIFICATION != null)
                CmbQualification.SelectedValue = _currentMaster.QUALIFICATION.ID_Qualification;
        }

        private void LoadStatistics()
        {
            if (_isNewMaster || _currentMaster.id == 0)
            {
                StatsPanel.Visibility = Visibility.Collapsed;
                return;
            }

            StatsPanel.Visibility = Visibility.Visible;

            // Статистика по рабочим актам
            TxtWorkActs.Text = db.WORK_ACTS.Count(w => w.master_id == _currentMaster.id).ToString();

            // Статистика по завершенным актам
            TxtCompletionActs.Text = db.COMPLETION_ACTS
                .Count(c => c.work_act_id.HasValue &&
                    db.WORK_ACTS.Any(w => w.id == c.work_act_id && w.master_id == _currentMaster.id)).ToString();

            // Статистика по оборудованию
            TxtEquipment.Text = db.EQUIPMENT.Count(e => e.assigned_to == _currentMaster.user_id).ToString();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateFields()) return;

                var selectedUser = CmbUsers.SelectedItem as USERS;
                var selectedSpecialty = CmbSpecialty.SelectedItem as SPECIALTY;
                var selectedQualification = CmbQualification.SelectedItem as QUALIFICATION;
                var selectedSkill = (CmbSkillLevel.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "средний";

                // Проверка на дубликат
                if (_isNewMaster)
                {
                    var exists = db.MASTERS.Any(m => m.user_id == selectedUser.id);
                    if (exists)
                    {
                        ShowWarning("Этот пользователь уже является МОЛ");
                        return;
                    }
                }
                else
                {
                    if (_currentMaster.user_id != selectedUser.id)
                    {
                        var exists = db.MASTERS.Any(m => m.user_id == selectedUser.id && m.id != _currentMaster.id);
                        if (exists)
                        {
                            ShowWarning("Этот пользователь уже является МОЛ");
                            return;
                        }
                    }
                }

                // Сохранение данных
                if (_isNewMaster)
                {
                    _currentMaster.user_id = selectedUser.id;
                    _currentMaster.hire_date = DpHireDate.SelectedDate.Value;
                    _currentMaster.is_available = ChkIsAvailable.IsChecked ?? true;
                    _currentMaster.skill_level = selectedSkill;
                    _currentMaster.specialty = selectedSpecialty.ID_specialty;
                    _currentMaster.qualifications = selectedQualification.ID_Qualification;

                    db.MASTERS.Add(_currentMaster);
                }
                else
                {
                    var masterInDb = db.MASTERS.Find(_currentMaster.id);
                    if (masterInDb != null)
                    {
                        masterInDb.user_id = selectedUser.id;
                        masterInDb.hire_date = DpHireDate.SelectedDate.Value;
                        masterInDb.is_available = ChkIsAvailable.IsChecked ?? true;
                        masterInDb.skill_level = selectedSkill;
                        masterInDb.specialty = selectedSpecialty.ID_specialty;
                        masterInDb.qualifications = selectedQualification.ID_Qualification;
                    }
                }

                db.SaveChanges();
                ShowSuccess("Данные успешно сохранены");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка сохранения: {ex.Message}");
            }
        }

        private bool ValidateFields()
        {
            if (CmbUsers.SelectedItem == null)
            {
                ShowWarning("Выберите пользователя");
                CmbUsers.Focus();
                return false;
            }

            if (DpHireDate.SelectedDate == null)
            {
                ShowWarning("Выберите дату принятия");
                DpHireDate.Focus();
                return false;
            }

            if (CmbSpecialty.SelectedItem == null)
            {
                ShowWarning("Выберите специальность");
                CmbSpecialty.Focus();
                return false;
            }

            if (CmbQualification.SelectedItem == null)
            {
                ShowWarning("Выберите квалификацию");
                CmbQualification.Focus();
                return false;
            }

            return true;
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_isNewMaster)
            {
                DialogResult = false;
                Close();
                return;
            }

            // Проверка связей
            var hasWorkActs = db.WORK_ACTS.Any(w => w.master_id == _currentMaster.id);
            if (hasWorkActs)
            {
                ShowWarning("Невозможно удалить МОЛ, так как есть связанные рабочие акты.\nСначала переназначьте или удалите акты.");
                return;
            }

            var result = MessageBox.Show($"Удалить МОЛ {_currentMaster.USERS?.full_name}?\n\nЭто действие нельзя отменить.",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var masterToDelete = db.MASTERS.Find(_currentMaster.id);
                    if (masterToDelete != null)
                    {
                        db.MASTERS.Remove(masterToDelete);
                        db.SaveChanges();
                        ShowSuccess("МОЛ успешно удален");
                        DialogResult = true;
                        Close();
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"Ошибка удаления: {ex.Message}");
                }
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnAddSpecialty_Click(object sender, RoutedEventArgs e)
        {
            var input = Microsoft.VisualBasic.Interaction.InputBox("Введите название специальности:",
                "Добавление специальности", "");
            if (!string.IsNullOrWhiteSpace(input))
            {
                try
                {
                    var newSpecialty = new SPECIALTY { Speciality = input.Trim() };
                    db.SPECIALTY.Add(newSpecialty);
                    db.SaveChanges();

                    CmbSpecialty.ItemsSource = db.SPECIALTY.OrderBy(s => s.Speciality).ToList();
                    CmbSpecialty.SelectedItem = newSpecialty;
                    ShowSuccess("Специальность добавлена");
                }
                catch (Exception ex)
                {
                    ShowError($"Ошибка: {ex.Message}");
                }
            }
        }

        private void BtnAddQualification_Click(object sender, RoutedEventArgs e)
        {
            var input = Microsoft.VisualBasic.Interaction.InputBox("Введите название квалификации:",
                "Добавление квалификации", "");
            if (!string.IsNullOrWhiteSpace(input))
            {
                try
                {
                    var newQual = new QUALIFICATION { Qualification1 = input.Trim() };
                    db.QUALIFICATION.Add(newQual);
                    db.SaveChanges();

                    CmbQualification.ItemsSource = db.QUALIFICATION.OrderBy(q => q.Qualification1).ToList();
                    CmbQualification.SelectedItem = newQual;
                    ShowSuccess("Квалификация добавлена");
                }
                catch (Exception ex)
                {
                    ShowError($"Ошибка: {ex.Message}");
                }
            }
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