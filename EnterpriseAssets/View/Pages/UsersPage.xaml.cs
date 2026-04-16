using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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

            // Применяем шаблон с аватарами
            UsersList.ItemsSource = null;
            UsersList.ItemsSource = filteredUsers.ToList();
        }

        // Метод для получения изображения аватара из байтов
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

        // Метод для создания DataTemplate с аватаром
        private DataTemplate CreateUserDataTemplate()
        {
            var template = new DataTemplate();
            template.VisualTree = new FrameworkElementFactory(typeof(Border));
            var border = template.VisualTree as FrameworkElementFactory;

            border.SetValue(Border.BackgroundProperty, Brushes.White);
            border.SetValue(Border.BorderBrushProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E1E5EB")));
            border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            border.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 0, 10));
            border.SetValue(FrameworkElement.PaddingProperty, new Thickness(15));
            border.SetValue(FrameworkElement.CursorProperty, Cursors.Hand);
            border.AddHandler(UIElement.MouseLeftButtonUpEvent, new MouseButtonEventHandler(UserCard_Click));

            var borderEffect = new FrameworkElementFactory(typeof(System.Windows.Media.Effects.DropShadowEffect));
            borderEffect.SetValue(System.Windows.Media.Effects.DropShadowEffect.ShadowDepthProperty, 2);
            borderEffect.SetValue(System.Windows.Media.Effects.DropShadowEffect.BlurRadiusProperty, 8);
            borderEffect.SetValue(System.Windows.Media.Effects.DropShadowEffect.OpacityProperty, 0.1);
            border.SetValue(Border.EffectProperty, borderEffect);

            // Grid
            var grid = new FrameworkElementFactory(typeof(Grid));
            var columnDef1 = new FrameworkElementFactory(typeof(ColumnDefinition));
            columnDef1.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Auto));
            var columnDef2 = new FrameworkElementFactory(typeof(ColumnDefinition));
            columnDef2.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
            grid.AppendChild(columnDef1);
            grid.AppendChild(columnDef2);

            // Avatar Border
            var avatarBorder = new FrameworkElementFactory(typeof(Border));
            avatarBorder.SetValue(Border.WidthProperty, 50.0);
            avatarBorder.SetValue(Border.HeightProperty, 50.0);
            avatarBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(25));
            avatarBorder.SetValue(Border.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F0FE")));
            avatarBorder.SetValue(Border.BorderBrushProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4A6FA5")));
            avatarBorder.SetValue(Border.BorderThicknessProperty, new Thickness(2));
            avatarBorder.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 15, 0));

            // Avatar Grid
            var avatarGrid = new FrameworkElementFactory(typeof(Grid));

            // Image
            var image = new FrameworkElementFactory(typeof(Image));
            image.SetValue(Image.WidthProperty, 50.0);
            image.SetValue(Image.HeightProperty, 50.0);
            image.SetValue(Image.StretchProperty, Stretch.UniformToFill);
            image.SetBinding(Image.SourceProperty, new System.Windows.Data.Binding("Photo") { Converter = new ByteArrayToImageConverter() });

            // Placeholder
            var placeholder = new FrameworkElementFactory(typeof(TextBlock));
            placeholder.SetValue(TextBlock.TextProperty, "👤");
            placeholder.SetValue(TextBlock.FontSizeProperty, 28.0);
            placeholder.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4A6FA5")));
            placeholder.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            placeholder.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);

            avatarGrid.AppendChild(image);
            avatarGrid.AppendChild(placeholder);
            avatarBorder.AppendChild(avatarGrid);

            // Info StackPanel
            var infoPanel = new FrameworkElementFactory(typeof(StackPanel));
            infoPanel.SetValue(StackPanel.VerticalAlignmentProperty, VerticalAlignment.Center);

            var fullName = new FrameworkElementFactory(typeof(TextBlock));
            fullName.SetValue(TextBlock.FontSizeProperty, 16.0);
            fullName.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
            fullName.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2C3E50")));
            fullName.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("full_name"));

            var username = new FrameworkElementFactory(typeof(TextBlock));
            username.SetValue(TextBlock.FontSizeProperty, 14.0);
            username.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7F8C8D")));
            username.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 2, 0, 0));
            username.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("username"));

            var wrapPanel = new FrameworkElementFactory(typeof(WrapPanel));
            wrapPanel.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 5, 0, 0));

            var role = new FrameworkElementFactory(typeof(TextBlock));
            role.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("ROLES.name"));
            role.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4A6FA5")));
            role.SetValue(TextBlock.FontSizeProperty, 12.0);
            role.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
            role.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 10, 0));

            var dot = new FrameworkElementFactory(typeof(TextBlock));
            dot.SetValue(TextBlock.TextProperty, "•");
            dot.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#95A5A6")));
            dot.SetValue(TextBlock.FontSizeProperty, 12.0);
            dot.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 10, 0));

            var emailIcon = new FrameworkElementFactory(typeof(TextBlock));
            emailIcon.SetValue(TextBlock.TextProperty, "📧");
            emailIcon.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#95A5A6")));
            emailIcon.SetValue(TextBlock.FontSizeProperty, 12.0);
            emailIcon.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 5, 0));

            var email = new FrameworkElementFactory(typeof(TextBlock));
            email.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("email"));
            email.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5D6D7E")));
            email.SetValue(TextBlock.FontSizeProperty, 12.0);
            email.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 15, 0));

            var phoneIcon = new FrameworkElementFactory(typeof(TextBlock));
            phoneIcon.SetValue(TextBlock.TextProperty, "📞");
            phoneIcon.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#95A5A6")));
            phoneIcon.SetValue(TextBlock.FontSizeProperty, 12.0);
            phoneIcon.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 5, 0));

            var phone = new FrameworkElementFactory(typeof(TextBlock));
            phone.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("phone"));
            phone.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5D6D7E")));
            phone.SetValue(TextBlock.FontSizeProperty, 12.0);

            wrapPanel.AppendChild(role);
            wrapPanel.AppendChild(dot);
            wrapPanel.AppendChild(emailIcon);
            wrapPanel.AppendChild(email);
            wrapPanel.AppendChild(phoneIcon);
            wrapPanel.AppendChild(phone);

            infoPanel.AppendChild(fullName);
            infoPanel.AppendChild(username);
            infoPanel.AppendChild(wrapPanel);

            grid.AppendChild(avatarBorder);
            grid.AppendChild(infoPanel);
            border.AppendChild(grid);

            template.DataType = typeof(USERS);
            return template;
        }

        private void AddUser_Click(object sender, RoutedEventArgs e)
        {
            // Получаем ID текущего пользователя из глобального контекста
            // Вам нужно передавать ID текущего авторизованного пользователя
            int currentUserId = GetCurrentUserId(); // Реализуйте этот метод
            var dialog = new View.UserManage(currentUserId);
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true) RefreshAllData();
        }

        private void UserCard_Click(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border?.DataContext is USERS user)
            {
                int currentUserId = GetCurrentUserId(); // Реализуйте этот метод
                var dialog = new View.UserManage(user, currentUserId);
                dialog.Owner = Window.GetWindow(this);
                if (dialog.ShowDialog() == true) RefreshAllData();
            }
        }

        // Метод для получения ID текущего пользователя
        private int GetCurrentUserId()
        {
            // Здесь должна быть ваша логика получения ID авторизованного пользователя
            // Например, из статического класса App.CurrentUser или из Properties.Settings
            return App.CurrentUser?.id ?? 0; // Реализуйте по своему
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

    // Конвертер для преобразования byte[] в ImageSource
    public class ByteArrayToImageConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is byte[] bytes && bytes.Length > 0)
            {
                try
                {
                    using (var stream = new MemoryStream(bytes))
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
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
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