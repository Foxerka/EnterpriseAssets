using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using EnterpriseAssets.Model.DataBase;

namespace EnterpriseAssets.View
{
    public partial class UserManage : Window
    {
        private DB_AssetManage db = new DB_AssetManage();
        private USERS _currentUser;
        private bool _isNewUser;

        // Конструктор для существующего пользователя
        public UserManage(USERS user)
        {
            InitializeComponent();
            _currentUser = user;
            _isNewUser = false;
            LoadUserData();
            LoadRoles();
        }

        // Конструктор для нового пользователя
        public UserManage()
        {
            InitializeComponent();
            _currentUser = new USERS();
            _isNewUser = true;
            Title = "Добавление нового пользователя";
            LoadRoles();
        }

        private void LoadUserData()
        {
            if (_currentUser == null) return;

            // Заполняем поля
            TxtUsername.Text = _currentUser.username;
            TxtFullName.Text = _currentUser.full_name;
            TxtEmail.Text = _currentUser.email;
            TxtPhone.Text = _currentUser.phone;
            ChkIsActive.IsChecked = true; // Добавьте поле is_active в модель, если есть

            // Заголовок
            UserFullName.Text = _currentUser.full_name ?? "Новый пользователь";
            UserRole.Text = GetUserRole(_currentUser.role_id);

            // Дополнительная информация
            if (_currentUser.created_at.HasValue)
                TxtCreatedAt.Text = $"Дата создания: {_currentUser.created_at:dd.MM.yyyy HH:mm}";
            else
                TxtCreatedAt.Text = "Новый пользователь";

            TxtLastLogin.Text = "Последний вход: не выполнялся";
        }

        private void LoadRoles()
        {
            var roles = db.ROLES.ToList();
            CmbRole.ItemsSource = roles;

            if (_currentUser?.role_id != null)
            {
                CmbRole.SelectedValue = _currentUser.role_id;
            }
        }

        private string GetUserRole(int? roleId)
        {
            if (!roleId.HasValue) return "Не назначена";
            var role = db.ROLES.FirstOrDefault(r => r.id == roleId.Value);
            return role?.name ?? "Не назначена";
        }

        private Brush GetRoleColor()
        {
            return _currentUser?.role_id switch
            {
                1 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C")), // Админ
                2 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB")), // Директор
                3 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F39C12")), // Нач. цеха
                4 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")), // Мастер
                5 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9B59B6")), // Кладовщик
                6 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1ABC9C")), // Оператор
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#95A5A6"))
            };
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация
                if (string.IsNullOrWhiteSpace(TxtUsername.Text))
                {
                    MessageBox.Show("Введите имя пользователя", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(TxtFullName.Text))
                {
                    MessageBox.Show("Введите полное имя", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Сохраняем данные
                _currentUser.username = TxtUsername.Text;
                _currentUser.full_name = TxtFullName.Text;
                _currentUser.email = TxtEmail.Text;
                _currentUser.phone = TxtPhone.Text;

                if (CmbRole.SelectedItem is ROLES selectedRole)
                {
                    _currentUser.role_id = selectedRole.id;
                }

                if (_isNewUser)
                {
                    // Для нового пользователя задаем пароль по умолчанию
                    _currentUser.password = "default123";
                    _currentUser.created_at = DateTime.Now;
                    db.USERS.Add(_currentUser);
                }

                db.SaveChanges();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_isNewUser)
            {
                // Если это новый пользователь, просто закрываем окно
                DialogResult = false;
                Close();
                return;
            }

            var result = MessageBox.Show(
                "Вы уверены, что хотите удалить пользователя?\nЭто действие нельзя отменить.",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Проверяем, есть ли связанные записи
                    var hasMasters = db.MASTERS.Any(m => m.user_id == _currentUser.id);

                    if (hasMasters)
                    {
                        MessageBox.Show(
                            "Невозможно удалить пользователя, так как он является мастером.\n" +
                            "Сначала удалите или переназначьте связанные записи.",
                            "Ошибка удаления",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return;
                    }

                    db.USERS.Remove(_currentUser);
                    db.SaveChanges();

                    MessageBox.Show("Пользователь успешно удален", "Успех",
                                  MessageBoxButton.OK, MessageBoxImage.Information);

                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnResetPassword_Click(object sender, RoutedEventArgs e)
        {
            if (_isNewUser)
            {
                MessageBox.Show("Для нового пользователя пароль будет установлен при сохранении",
                              "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                "Сбросить пароль пользователя?\nНовый пароль будет отправлен на email.",
                "Сброс пароля",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Генерируем новый пароль
                    string newPassword = GenerateRandomPassword();
                    _currentUser.password = newPassword;
                    db.SaveChanges();

                    MessageBox.Show(
                        $"Пароль успешно сброшен.\nНовый пароль: {newPassword}\n\n" +
                        "Рекомендуем сообщить пароль пользователю.",
                        "Сброс пароля",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка сброса пароля: {ex.Message}", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}