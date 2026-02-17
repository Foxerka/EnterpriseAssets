using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using EnterpriseAssets.Model.DataBase;
using System.Data.Entity; // Для Include

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
            Title = "Назначение нового мастера";
            BtnDelete.Visibility = Visibility.Collapsed;
            StatsPanel.Visibility = Visibility.Collapsed;

            LoadData();
            DpHireDate.SelectedDate = DateTime.Today;
        }

        public MasterManage(MASTERS master)
        {
            InitializeComponent();
            // Загружаем актуальные данные с связанными сущностями
            _currentMaster = db.MASTERS
            .Include("QUALIFICATION")  // с маленькой буквы
            .Include("SPECIALTY1")
            .FirstOrDefault(m => m.id == master.id);

            if (_currentMaster == null)
            {
                MessageBox.Show("Мастер не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                DialogResult = false;
                Close();
                return;
            }

            _isNewMaster = false;
            Title = $"Редактирование мастера: {_currentMaster.USERS?.full_name}";
            LoadData();
            LoadMasterData();
            LoadStatistics();
        }

        private void LoadData()
        {
            // Получаем ID роли "Мастер"
            int masterRoleId = db.ROLES.First(r => r.name == "Мастер").id;

            // Получаем ID пользователей, которые уже являются мастерами
            var existingMasterIds = db.MASTERS
                .Where(m => m.user_id.HasValue)
                .Select(m => m.user_id.Value)
                .ToList();

            // Если редактируем, исключаем текущего мастера из списка исключений
            if (!_isNewMaster && _currentMaster.user_id.HasValue)
            {
                existingMasterIds.Remove(_currentMaster.user_id.Value);
            }

            // Загружаем пользователей с ролью "Мастер", исключая уже назначенных
            var availableUsers = db.USERS
                .Where(u => u.role_id == masterRoleId) // Только мастера по роли
                .Where(u => !existingMasterIds.Contains(u.id)) // Исключаем уже назначенных
                .OrderBy(u => u.full_name)
                .ToList();

            CmbUsers.ItemsSource = availableUsers;
            CmbUsers.DisplayMemberPath = "full_name";
            CmbUsers.SelectedValuePath = "id";

            // 2. Загрузка справочников
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
                CmbSpecialty.SelectedItem = _currentMaster.specialty;

            if (_currentMaster.QUALIFICATION != null)
                CmbQualification.SelectedItem = _currentMaster.QUALIFICATION;
        }

        private void LoadStatistics()
        {
            if (_isNewMaster || _currentMaster.id == 0)
            {
                StatsPanel.Visibility = Visibility.Collapsed;
                return;
            }

            StatsPanel.Visibility = Visibility.Visible;
            TxtWorkActs.Text = db.WORK_ACTS.Count(w => w.master_id == _currentMaster.id).ToString();
            TxtCompletionActs.Text = db.COMPLETION_ACTS.Count(c => c.work_act_id.HasValue &&
                db.WORK_ACTS.Any(w => w.id == c.work_act_id && w.master_id == _currentMaster.id)).ToString();
            TxtEquipment.Text = db.EQUIPMENT.Count(e => e.assigned_to == _currentMaster.user_id).ToString();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация
                if (CmbUsers.SelectedItem == null)
                {
                    MessageBox.Show("Выберите пользователя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (DpHireDate.SelectedDate == null)
                {
                    MessageBox.Show("Выберите дату приема", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (CmbSpecialty.SelectedItem == null)
                {
                    MessageBox.Show("Выберите специальность", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (CmbQualification.SelectedItem == null)
                {
                    MessageBox.Show("Выберите квалификацию", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ОБЪЯВЛЯЕМ ПЕРЕМЕННУЮ ОДИН РАЗ В НАЧАЛЕ
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
                        MessageBox.Show("Этот пользователь уже является мастером", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                else
                {
                    // При редактировании проверяем, не занят ли новый пользователь
                    if (_currentMaster.user_id != selectedUser.id)
                    {
                        var exists = db.MASTERS.Any(m => m.user_id == selectedUser.id && m.id != _currentMaster.id);
                        if (exists)
                        {
                            MessageBox.Show("Этот пользователь уже является мастером", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
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

                MessageBox.Show("Данные успешно сохранены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                MessageBox.Show("Невозможно удалить мастера, так как есть связанные рабочие акты.",
                    "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Удалить мастера {_currentMaster.USERS?.full_name}?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var masterToDelete = db.MASTERS.Find(_currentMaster.id);
                    if (masterToDelete != null)
                    {
                        db.MASTERS.Remove(masterToDelete);
                        db.SaveChanges();
                        MessageBox.Show("Мастер удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        DialogResult = true;
                        Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
            var input = Microsoft.VisualBasic.Interaction.InputBox("Введите название специальности:", "Добавление специальности", "");
            if (!string.IsNullOrWhiteSpace(input))
            {
                try
                {
                    var newSpecialty = new SPECIALTY { Speciality = input };
                    db.SPECIALTY.Add(newSpecialty);
                    db.SaveChanges();

                    CmbSpecialty.ItemsSource = db.SPECIALTY.OrderBy(s => s.Speciality).ToList();
                    CmbSpecialty.SelectedItem = newSpecialty;
                    MessageBox.Show("Специальность добавлена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnAddQualification_Click(object sender, RoutedEventArgs e)
        {
            var input = Microsoft.VisualBasic.Interaction.InputBox("Введите название квалификации:", "Добавление квалификации", "");
            if (!string.IsNullOrWhiteSpace(input))
            {
                try
                {
                    var newQual = new QUALIFICATION { Qualification1 = input };
                    db.QUALIFICATION.Add(newQual);
                    db.SaveChanges();

                    CmbQualification.ItemsSource = db.QUALIFICATION.OrderBy(q => q.Qualification1).ToList();
                    CmbQualification.SelectedItem = newQual;
                    MessageBox.Show("Квалификация добавлена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}