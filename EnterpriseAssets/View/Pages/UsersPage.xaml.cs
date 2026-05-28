using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using EnterpriseAssets.Model.DataBase;

namespace EnterpriseAssets.View.Pages
{
    public partial class UsersPage : Page
    {
        private DB_AssetManage db = new();
        private List<USERS> _allUsers;
        private List<RoleViewModel> _allRoles;
        private List<ResponsiblePersonViewModel> _allResponsiblePersons;
        private int _currentUserId;

        public UsersPage()
        {
            InitializeComponent();

            RolesSearchBox.TextChanged += RolesSearchBox_TextChanged;
            MastersSearchBox.TextChanged += MastersSearchBox_TextChanged;

            this.Loaded += UsersPage_Loaded;
            RefreshAllData();
        }

        public UsersPage(int currentUserId) : this()
        {
            _currentUserId = currentUserId;
        }

        private void RefreshAllData()
        {
            try
            {
                _allUsers = null;
                _allRoles = null;
                _allResponsiblePersons = null;

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

                LoadResponsiblePersons();
                LoadRoleFilter(); // Загружаем роли для фильтра
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

        private void LoadRoleFilter()
        {
            try
            {
                // Очищаем ComboBox
                RoleFilter.Items.Clear();

                // Добавляем пункт "Все роли"
                RoleFilter.Items.Add(new ComboBoxItem { Content = "Все роли", IsSelected = true });

                // Загружаем все роли из базы данных
                var roles = db.ROLES
                    .OrderBy(r => r.name)
                    .ToList();

                // Добавляем каждую роль в ComboBox
                foreach (var role in roles)
                {
                    RoleFilter.Items.Add(new ComboBoxItem { Content = role.name });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки ролей для фильтра: {ex.Message}");
                // Если ошибка, добавляем стандартные варианты
                RoleFilter.Items.Add(new ComboBoxItem { Content = "Все роли", IsSelected = true });
                RoleFilter.Items.Add(new ComboBoxItem { Content = "Администратор" });
                RoleFilter.Items.Add(new ComboBoxItem { Content = "Директор" });
                RoleFilter.Items.Add(new ComboBoxItem { Content = "Начальник цеха" });
                RoleFilter.Items.Add(new ComboBoxItem { Content = "Мастер" });
                RoleFilter.Items.Add(new ComboBoxItem { Content = "Кладовщик" });
                RoleFilter.Items.Add(new ComboBoxItem { Content = "Оператор" });
            }
        }

        private void LoadResponsiblePersons()
        {
            try
            {
                _allResponsiblePersons = (from m in db.MASTERS
                                          join u in db.USERS on m.user_id equals u.id into userJoin
                                          from u in userJoin.DefaultIfEmpty()
                                          join s in db.SPECIALTY on m.specialty equals s.ID_specialty into specialtyJoin
                                          from s in specialtyJoin.DefaultIfEmpty()
                                          join q in db.QUALIFICATION on m.qualifications equals q.ID_Qualification into qualJoin
                                          from q in qualJoin.DefaultIfEmpty()
                                          select new ResponsiblePersonViewModel
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

                ApplyResponsiblePersonFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки МОЛ: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UsersPage_Loaded(object sender, RoutedEventArgs e)
        {
            SetActiveTab("Users");
        }

        private void SetActiveTab(string tabName)
        {
            if (UsersTabContent != null) UsersTabContent.Visibility = Visibility.Collapsed;
            if (RolesTabContent != null) RolesTabContent.Visibility = Visibility.Collapsed;
            if (MastersTabContent != null) MastersTabContent.Visibility = Visibility.Collapsed;

            if (UsersFilters != null) UsersFilters.Visibility = Visibility.Collapsed;
            if (RolesFilters != null) RolesFilters.Visibility = Visibility.Collapsed;
            if (MastersFilters != null) MastersFilters.Visibility = Visibility.Collapsed;

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
                    ApplyResponsiblePersonFilters();
                    break;
            }
        }

        // ===== Переключение вкладок =====
        private void TabUsers_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
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

        private void MastersSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearMastersSearchButton.Visibility = string.IsNullOrEmpty(MastersSearchBox.Text)
                ? Visibility.Collapsed
                : Visibility.Visible;
            ApplyResponsiblePersonFilters();
        }

        private void ClearMastersSearch_Click(object sender, RoutedEventArgs e)
        {
            MastersSearchBox.Text = string.Empty;
            ApplyResponsiblePersonFilters();
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

            // Фильтр по роли - теперь загружен из БД
            var selectedRoleItem = RoleFilter.SelectedItem as ComboBoxItem;
            var selectedRole = selectedRoleItem?.Content?.ToString();

            if (selectedRole != "Все роли" && !string.IsNullOrEmpty(selectedRole))
            {
                filteredUsers = filteredUsers.Where(u => u.ROLES != null && u.ROLES.name == selectedRole);
            }

            var selectedSort = (UsersSortFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();
            filteredUsers = selectedSort switch
            {
                "По полному имени" => filteredUsers.OrderBy(u => u.full_name),
                "По роли" => filteredUsers.OrderBy(u => u.ROLES?.name ?? ""),
                _ => filteredUsers.OrderBy(u => u.username)
            };

            UsersList.ItemsSource = null;
            UsersList.ItemsSource = filteredUsers.ToList();
        }

        private ImageSource GetAvatarImage(byte[] photoBytes)
        {
            if (photoBytes == null || photoBytes.Length == 0)
                return null;

            try
            {
                using (var stream = new MemoryStream(photoBytes))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = stream;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    return bitmap;
                }
            }
            catch
            {
                return null;
            }
        }

        private void AddUser_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new View.UserManage(_currentUserId);
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true) RefreshAllData();
        }

        private void UserCard_Click(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border?.DataContext is USERS user)
            {
                var dialog = new View.UserManage(user, _currentUserId);
                dialog.Owner = Window.GetWindow(this);
                if (dialog.ShowDialog() == true) RefreshAllData();
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

        // ===== МОЛ (Материально-ответственные лица) =====
        private void FilterMasters(object sender, RoutedEventArgs e)
        {
            ApplyResponsiblePersonFilters();
        }

        private void ApplyResponsiblePersonFilters()
        {
            if (_allResponsiblePersons == null) return;

            IEnumerable<ResponsiblePersonViewModel> filteredPersons = _allResponsiblePersons;

            string searchText = MastersSearchBox.Text?.Trim().ToLower() ?? "";
            if (!string.IsNullOrEmpty(searchText))
            {
                filteredPersons = filteredPersons.Where(m =>
                    (m.UserName?.ToLower().Contains(searchText) ?? false) ||
                    (m.SpecialtyName?.ToLower().Contains(searchText) ?? false) ||
                    (m.QualificationName?.ToLower().Contains(searchText) ?? false)
                );
            }

            if (ShowOnlyAvailable.IsChecked == true)
            {
                filteredPersons = filteredPersons.Where(m => m.IsAvailable);
            }

            filteredPersons = filteredPersons.OrderBy(m => m.UserName);

            MastersList.ItemsSource = null;
            MastersList.ItemsSource = filteredPersons.ToList();
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
            ApplyResponsiblePersonFilters();
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

    // ViewModel для МОЛ (Материально-ответственное лицо)
    public class ResponsiblePersonViewModel
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