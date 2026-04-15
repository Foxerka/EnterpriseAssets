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
    public partial class UsersPage : Page
    {
        private DB_AssetManage db = new();

        private List<USERS> _allUsers;
        private List<RoleViewModel> _allRoles;
        private List<MasterViewModel> _allMasters;

        public UsersPage()
        {
            InitializeComponent();

            // Подписываемся на события поиска
            RolesSearchBox.TextChanged += RolesSearchBox_TextChanged;
            MastersSearchBox.TextChanged += MastersSearchBox_TextChanged;

            this.Loaded += UsersPage_Loaded;
            RefreshAllData();
        }

        private void RefreshAllData()
        {
            try
            {
                _allUsers = null;
                _allRoles = null;
                _allMasters = null;

                db.Dispose();
                db = new DB_AssetManage();

                _allUsers = db.USERS.Include("ROLES").ToList();

                _allRoles = db.ROLES
                    .Select(r => new RoleViewModel
                    {
                        Id = r.id,
                        Name = r.name,
                        Description = r.description,
                        UsersCount = db.USERS.Count(u => u.role_id == r.id)
                    })
                    .OrderBy(r => r.Name)
                    .ToList();

                LoadMasters();
                ApplyAllFilters();

                RolesList.ItemsSource = null;
                RolesList.ItemsSource = _allRoles;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления данных: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadMasters()
        {
            try
            {
                _allMasters = (from m in db.MASTERS
                               join u in db.USERS on m.user_id equals u.id into userJoin
                               from u in userJoin.DefaultIfEmpty()
                               join s in db.SPECIALTY on m.specialty equals s.ID_specialty into specialtyJoin
                               from s in specialtyJoin.DefaultIfEmpty()
                               join q in db.QUALIFICATION on m.qualifications equals q.ID_Qualification into qualJoin
                               from q in qualJoin.DefaultIfEmpty()
                               select new MasterViewModel
                               {
                                   Id = m.id,
                                   UserName = u != null ? u.full_name : "Не назначен",
                                   SpecialtyName = s != null ? s.Speciality : "Не указана",
                                   QualificationName = q != null ? q.Qualification1 : "Не указана",
                                   SkillLevel = m.skill_level ?? "Средний",
                                   IsAvailable = m.is_available ?? false,
                                   HireDate = m.hire_date,
                                   WorkActsCount = db.WORK_ACTS.Count(w => w.master_id == m.id),
                                   CompletionActsCount = db.COMPLETION_ACTS.Count(c => c.work_act_id.HasValue &&
                                                                                      db.WORK_ACTS.Any(w => w.id == c.work_act_id && w.master_id == m.id)),
                                   EquipmentCount = db.EQUIPMENT.Count(e => e.assigned_to == m.user_id)
                               }).ToList();

                ApplyMasterFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки мастеров: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UsersPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Устанавливаем начальное состояние после загрузки всех элементов
            SetActiveTab("Users");
        }

        private void SetActiveTab(string tabName)
        {
            // Скрываем все вкладки и фильтры
            if (UsersTabContent != null) UsersTabContent.Visibility = Visibility.Collapsed;
            if (RolesTabContent != null) RolesTabContent.Visibility = Visibility.Collapsed;
            if (MastersTabContent != null) MastersTabContent.Visibility = Visibility.Collapsed;

            if (UsersFilters != null) UsersFilters.Visibility = Visibility.Collapsed;
            if (RolesFilters != null) RolesFilters.Visibility = Visibility.Collapsed;
            if (MastersFilters != null) MastersFilters.Visibility = Visibility.Collapsed;

            // Показываем нужную вкладку и фильтры
            switch (tabName)
            {
                case "Users":
                    if (UsersTabContent != null) UsersTabContent.Visibility = Visibility.Visible;
                    if (UsersFilters != null) UsersFilters.Visibility = Visibility.Visible;
                    ApplyUserFilters();
                    break;
                case "Roles":
                    if (RolesTabContent != null) RolesTabContent.Visibility = Visibility.Visible;
                    if (RolesFilters != null) RolesFilters.Visibility = Visibility.Visible;
                    ApplyRolesFilter();
                    break;
                case "Masters":
                    if (MastersTabContent != null) MastersTabContent.Visibility = Visibility.Visible;
                    if (MastersFilters != null) MastersFilters.Visibility = Visibility.Visible;
                    ApplyMasterFilters();
                    break;
            }
        }

        // ===== Переключение вкладок =====
        private void TabUsers_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return; // Пропускаем если страница ещё не загружена
            SetActiveTab("Users");
        }

        private void TabRoles_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            SetActiveTab("Roles");
        }

        private void TabMasters_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            SetActiveTab("Masters");
        }

        // ===== Переключение вкладок =====
        

        // ===== Поиск =====
        private void UsersSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearUsersSearchButton.Visibility = string.IsNullOrEmpty(UsersSearchBox.Text)
                ? Visibility.Collapsed
                : Visibility.Visible;
            ApplyUserFilters();
        }

        private void ClearUsersSearch_Click(object sender, RoutedEventArgs e)
        {
            UsersSearchBox.Text = string.Empty;
            ApplyUserFilters();
        }

        // ===== Поиск для ролей =====
        private void RolesSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearRolesSearchButton.Visibility = string.IsNullOrEmpty(RolesSearchBox.Text)
                ? Visibility.Collapsed
                : Visibility.Visible;
            ApplyRolesFilter();
        }

        private void ClearRolesSearch_Click(object sender, RoutedEventArgs e)
        {
            RolesSearchBox.Text = string.Empty;
            ApplyRolesFilter();
        }

        // ===== Поиск для мастеров =====
        private void MastersSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearMastersSearchButton.Visibility = string.IsNullOrEmpty(MastersSearchBox.Text)
                ? Visibility.Collapsed
                : Visibility.Visible;
            ApplyMasterFilters();
        }

        private void ClearMastersSearch_Click(object sender, RoutedEventArgs e)
        {
            MastersSearchBox.Text = string.Empty;
            ApplyMasterFilters();
        }


        // ===== Пользователи =====
        private void RefreshUsers_Click(object sender, RoutedEventArgs e)
        {
            RefreshAllData();
        }

        private void SortUsers(object sender, SelectionChangedEventArgs e)
        {
            ApplyUserFilters();
        }

        private void FilterUsers(object sender, SelectionChangedEventArgs e)
        {
            ApplyUserFilters();
        }

        private void ApplyUserFilters()
        {
            if (_allUsers == null) return;

            IEnumerable<USERS> filteredUsers = _allUsers;

            // Поиск
            string searchText = UsersSearchBox.Text?.Trim().ToLower() ?? "";
            if (!string.IsNullOrEmpty(searchText))
            {
                filteredUsers = filteredUsers.Where(u =>
                    (u.username?.ToLower().Contains(searchText) ?? false) ||
                    (u.full_name?.ToLower().Contains(searchText) ?? false) ||
                    (u.email?.ToLower().Contains(searchText) ?? false) ||
                    (u.phone?.ToLower().Contains(searchText) ?? false)
                );
            }

            // Фильтр по роли
            var selectedRole = (RoleFilter.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (selectedRole != "Все роли" && !string.IsNullOrEmpty(selectedRole))
            {
                filteredUsers = filteredUsers.Where(u => u.ROLES != null && u.ROLES.name == selectedRole);
            }

            // Сортировка
            var selectedSort = (UsersSortFilter.SelectedItem as ComboBoxItem)?.Content.ToString();
            filteredUsers = selectedSort switch
            {
                "По полному имени" => filteredUsers.OrderBy(u => u.full_name),
                "По роли" => filteredUsers.OrderBy(u => u.ROLES?.name ?? ""),
                _ => filteredUsers.OrderBy(u => u.username)
            };

            UsersList.ItemsSource = null;
            UsersList.ItemsSource = filteredUsers.ToList();
        }

        private void AddUser_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new View.UserManage();
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true) RefreshAllData();
        }

        private void UserCard_Click(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border?.Tag != null && int.TryParse(border.Tag.ToString(), out int userId))
            {
                var user = _allUsers?.FirstOrDefault(u => u.id == userId);
                if (user != null)
                {
                    var dialog = new View.UserManage(user);
                    dialog.Owner = Window.GetWindow(this);
                    if (dialog.ShowDialog() == true) RefreshAllData();
                }
            }
        }

        // ===== Роли =====
        private void ApplyRolesFilter()
        {
            if (_allRoles == null) return;

            IEnumerable<RoleViewModel> filteredRoles = _allRoles;

            string searchText = RolesSearchBox.Text?.Trim().ToLower() ?? "";
            if (!string.IsNullOrEmpty(searchText))
            {
                filteredRoles = filteredRoles.Where(r =>
                    (r.Name?.ToLower().Contains(searchText) ?? false) ||
                    (r.Description?.ToLower().Contains(searchText) ?? false)
                );
            }

            RolesList.ItemsSource = null;
            RolesList.ItemsSource = filteredRoles.ToList();
        }

        private void AddRole_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new View.RoleManage();
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true) RefreshAllData();
        }

        private void RoleCard_Click(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border?.Tag != null && int.TryParse(border.Tag.ToString(), out int roleId))
            {
                var fullRole = db.ROLES.FirstOrDefault(r => r.id == roleId);
                if (fullRole != null)
                {
                    var dialog = new View.RoleManage(fullRole);
                    dialog.Owner = Window.GetWindow(this);
                    if (dialog.ShowDialog() == true) RefreshAllData();
                }
            }
        }

        // ===== Мастера =====
        private void FilterMasters(object sender, RoutedEventArgs e)
        {
            ApplyMasterFilters();
        }

        private void ApplyMasterFilters()
        {
            if (_allMasters == null) return;

            IEnumerable<MasterViewModel> filteredMasters = _allMasters;

            // Поиск
            string searchText = MastersSearchBox.Text?.Trim().ToLower() ?? "";
            if (!string.IsNullOrEmpty(searchText))
            {
                filteredMasters = filteredMasters.Where(m =>
                    (m.UserName?.ToLower().Contains(searchText) ?? false) ||
                    (m.SpecialtyName?.ToLower().Contains(searchText) ?? false) ||
                    (m.QualificationName?.ToLower().Contains(searchText) ?? false)
                );
            }

            // Фильтр доступности
            if (ShowOnlyAvailable.IsChecked == true)
            {
                filteredMasters = filteredMasters.Where(m => m.IsAvailable);
            }

            filteredMasters = filteredMasters.OrderBy(m => m.UserName);

            MastersList.ItemsSource = null;
            MastersList.ItemsSource = filteredMasters.ToList();
        }

        private void AssignMaster_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new View.MasterManage();
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true) RefreshAllData();
        }

        private void MastersReport_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функция отчета в разработке", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MasterCard_Click(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border?.Tag != null && int.TryParse(border.Tag.ToString(), out int masterId))
            {
                var fullMaster = db.MASTERS.FirstOrDefault(m => m.id == masterId);
                if (fullMaster != null)
                {
                    var dialog = new View.MasterManage(fullMaster);
                    dialog.Owner = Window.GetWindow(this);
                    if (dialog.ShowDialog() == true) RefreshAllData();
                }
            }
        }

        private void ApplyAllFilters()
        {
            ApplyUserFilters();
            ApplyRolesFilter();
            ApplyMasterFilters();
        }

        ~UsersPage()
        {
            db?.Dispose();
        }
    }

    // ViewModel для ролей
    public class RoleViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int UsersCount { get; set; }
    }

    // ViewModel для мастеров
    public class MasterViewModel
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string SpecialtyName { get; set; }
        public string QualificationName { get; set; }
        public string SkillLevel { get; set; }
        public bool IsAvailable { get; set; }
        public DateTime? HireDate { get; set; }
        public int WorkActsCount { get; set; }
        public int CompletionActsCount { get; set; }
        public int EquipmentCount { get; set; }

        public string StatusText => IsAvailable ? "Доступен" : "Занят";

        public Brush StatusColor => IsAvailable ?
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")) :
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));

        public Brush AvailabilityBorderColor => IsAvailable ?
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")) :
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E1E5EB"));

        public Brush SkillLevelColor
        {
            get
            {
                return SkillLevel?.ToLower() switch
                {
                    "высокий" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")),
                    "средний" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F39C12")),
                    "низкий" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C")),
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB"))
                };
            }
        }
    }
}