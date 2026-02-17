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
        private DB_AssetManage db = new DB_AssetManage();
        private List<USERS> _allUsers;
        private List<RoleViewModel> _allRoles; // Изменили тип
        private List<MasterViewModel> _allMasters;

        public UsersPage()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                // Загрузка пользователей с включением связанных данных
                _allUsers = db.USERS
                    .Include("ROLES") // Включаем данные о ролях
                    .ToList();

                ApplyUserSort();

                // Загрузка ролей
                _allRoles = db.ROLES
                    .Select(r => new RoleViewModel
                    {
                        Id = r.id,
                        Name = r.name,
                        Description = r.description,
                        UsersCount = db.USERS.Count(u => u.role_id == r.id)
                    })
                    .OrderBy(r => r.Name) // Сразу сортируем по имени
                    .ToList();

                RolesList.ItemsSource = _allRoles;

                // Загрузка мастеров
                LoadMasters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
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

                MastersList.ItemsSource = _allMasters;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки мастеров: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ===== Обработчики для пользователей =====

        private void RefreshUsers_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void SortUsers(object sender, RoutedEventArgs e)
        {
            ApplyUserSort();
        }

        private void FilterUsers(object sender, SelectionChangedEventArgs e)
        {
            ApplyUserFilter();
        }

        private void ApplyUserSort()
        {
            if (_allUsers == null) return;

            IEnumerable<USERS> sortedUsers = _allUsers;

            // Применяем выбранную сортировку
            if (SortByUsername.IsChecked == true)
                sortedUsers = _allUsers.OrderBy(u => u.username);
            else if (SortByFullName.IsChecked == true)
                sortedUsers = _allUsers.OrderBy(u => u.full_name);
            else if (SortByRole.IsChecked == true)
                sortedUsers = _allUsers.OrderBy(u => u.ROLES != null ? u.ROLES.name : "");
            else
                sortedUsers = _allUsers.OrderBy(u => u.username); // Сортировка по умолчанию

            // Применяем фильтр
            ApplyUserFilter(sortedUsers);
        }

        private void ApplyUserFilter(IEnumerable<USERS> sortedUsers = null)
        {
            if (_allUsers == null) return;

            var source = sortedUsers ?? _allUsers.AsEnumerable();
            var selectedRole = (RoleFilter.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (selectedRole != "Все роли" && !string.IsNullOrEmpty(selectedRole))
            {
                source = source.Where(u => u.ROLES != null && u.ROLES.name == selectedRole);
            }

            UsersList.ItemsSource = source.ToList();
        }

        private void AddUser_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new View.UserManage();
            dialog.Owner = Window.GetWindow(this);

            if (dialog.ShowDialog() == true)
            {
                // Обновляем список после добавления
                LoadData();
                MessageBox.Show("Пользователь успешно добавлен", "Успех",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void UserCard_Click(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border?.Tag != null)
            {
                int userId = (int)border.Tag;
                var user = _allUsers?.FirstOrDefault(u => u.id == userId);
                if (user != null)
                {
                    var dialog = new View.UserManage(user);
                    dialog.Owner = Window.GetWindow(this);

                    if (dialog.ShowDialog() == true)
                    {
                        // Обновляем список после изменений
                        LoadData();
                    }
                }
            }
        }

        // ===== Обработчики для ролей =====
        private void AddRole_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Добавление новой роли", "Добавление",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SortRoles(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_allRoles == null || !_allRoles.Any())
                {
                    MessageBox.Show("Нет данных для сортировки", "Информация",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                List<RoleViewModel> sortedRoles;

                if (SortRolesByName.IsChecked == true)
                    sortedRoles = _allRoles.OrderBy(r => r.Name).ToList();
                else if (SortRolesById.IsChecked == true)
                    sortedRoles = _allRoles.OrderBy(r => r.Id).ToList();
                else
                    sortedRoles = _allRoles;

                RolesList.ItemsSource = sortedRoles;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сортировки: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RoleCard_Click(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border?.Tag != null)
            {
                int roleId = (int)border.Tag;
                var role = _allRoles?.FirstOrDefault(r => r.Id == roleId);
                if (role != null)
                {
                    ShowRoleEditDialog(role);
                }
            }
        }

        private void ShowRoleEditDialog(RoleViewModel role)
        {
            // Диалог редактирования роли
            MessageBox.Show($"Редактирование роли: {role.Name}", "Редактирование роли",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ===== Обработчики для мастеров =====
        private void AssignMaster_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Назначение нового мастера", "Назначение",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MastersReport_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Формирование отчета по мастерам", "Отчет",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SortMasters(object sender, RoutedEventArgs e)
        {
            ApplyMasterSort();
        }

        private void FilterMasters(object sender, RoutedEventArgs e)
        {
            ApplyMasterFilter();
        }

        private void ApplyMasterSort()
        {
            if (_allMasters == null) return;

            IEnumerable<MasterViewModel> sortedMasters = _allMasters;

            if (SortMastersByName.IsChecked == true)
                sortedMasters = _allMasters.OrderBy(m => m.UserName);
            else if (SortMastersBySpecialty.IsChecked == true)
                sortedMasters = _allMasters.OrderBy(m => m.SpecialtyName);
            else if (SortMastersByAvailability.IsChecked == true)
                sortedMasters = _allMasters.OrderByDescending(m => m.IsAvailable);

            MastersList.ItemsSource = sortedMasters.ToList();
        }

        private void ApplyMasterFilter()
        {
            if (_allMasters == null) return;

            var filteredMasters = _allMasters.AsEnumerable();

            if (ShowOnlyAvailable.IsChecked == true)
            {
                filteredMasters = filteredMasters.Where(m => m.IsAvailable);
            }

            MastersList.ItemsSource = filteredMasters.ToList();
        }

        private void MasterCard_Click(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border?.Tag != null)
            {
                int masterId = (int)border.Tag;
                var master = _allMasters?.FirstOrDefault(m => m.Id == masterId);
                if (master != null)
                {
                    ShowMasterEditDialog(master);
                }
            }
        }

        private void ShowMasterEditDialog(MasterViewModel master)
        {
            MessageBox.Show($"Редактирование мастера: {master.UserName}", "Редактирование мастера",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    // ViewModel для ролей
    public class RoleViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int UsersCount { get; set; }
        public int PermissionsCount { get; set; }
        public Brush RoleColor => new SolidColorBrush((Color)ColorConverter.ConvertFromString(GetRoleColor(Name)));

        private string GetRoleColor(string roleName)
        {
            return roleName switch
            {
                "Администратор" => "#E74C3C",
                "Директор" => "#3498DB",
                "Начальник цеха" => "#F39C12",
                "Мастер" => "#27AE60",
                "Кладовщик" => "#9B59B6",
                "Оператор" => "#1ABC9C",
                _ => "#95A5A6"
            };
        }
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