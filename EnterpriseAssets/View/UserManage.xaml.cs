using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using EnterpriseAssets.Model.DataBase;

namespace EnterpriseAssets.View
{
    public partial class UserManage : Window
    {
        private DB_AssetManage db = new DB_AssetManage();
        private USERS _currentUser;
        private bool _isNewUser;
        private bool _isChangingPassword = false;
        private byte[] _currentPhotoBytes;
        private int _currentLoggedInUserId; // ID текущего авторизованного пользователя

        // Конструктор для существующего пользователя
        public UserManage(USERS user, int currentLoggedInUserId = 0)
        {
            InitializeComponent();
            _currentUser = user;
            _isNewUser = false;
            _currentLoggedInUserId = currentLoggedInUserId;
            LoadUserData();
            LoadRoles();
            LoadUserPhoto();

            // Скрываем секцию пароля для существующего пользователя
            PasswordSection.Visibility = Visibility.Collapsed;
            ConfirmPasswordSection.Visibility = Visibility.Collapsed;
        }

        // Конструктор для нового пользователя
        public UserManage(int currentLoggedInUserId = 0)
        {
            InitializeComponent();
            _currentUser = new USERS();
            _isNewUser = true;
            _currentLoggedInUserId = currentLoggedInUserId;
            Title = "Добавление нового пользователя";
            LoadRoles();

            // Показываем секцию пароля для нового пользователя
            PasswordSection.Visibility = Visibility.Visible;
            ConfirmPasswordSection.Visibility = Visibility.Visible;
            BtnChangePassword.Visibility = Visibility.Collapsed; // Скрываем кнопку смены пароля
        }

        private void LoadUserData()
        {
            if (_currentUser == null) return;

            // Заполняем поля
            TxtUsername.Text = _currentUser.username;
            TxtFullName.Text = _currentUser.full_name;
            TxtEmail.Text = _currentUser.email;
            TxtPhone.Text = _currentUser.phone;

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

        private void LoadUserPhoto()
        {
            if (_currentUser?.photo != null && _currentUser.photo.Length > 0)
            {
                try
                {
                    using (var stream = new MemoryStream(_currentUser.photo))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = stream;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();

                        AvatarImage.Source = bitmap;
                        AvatarImage.Visibility = Visibility.Visible;
                        AvatarPlaceholder.Visibility = Visibility.Collapsed;
                        _currentPhotoBytes = _currentUser.photo;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка загрузки фото: {ex.Message}");
                    SetDefaultAvatar();
                }
            }
            else
            {
                SetDefaultAvatar();
            }
        }

        private void SetDefaultAvatar()
        {
            AvatarImage.Source = null;
            AvatarImage.Visibility = Visibility.Collapsed;
            AvatarPlaceholder.Visibility = Visibility.Visible;
            _currentPhotoBytes = null;
        }

        private void LoadRoles()
        {
            var roles = db.ROLES.ToList();
            CmbRole.ItemsSource = roles;

            if (_currentUser?.role_id != null)
            {
                var selectedRole = roles.FirstOrDefault(r => r.id == _currentUser.role_id);
                if (selectedRole != null)
                {
                    CmbRole.SelectedItem = selectedRole;
                }
            }
        }

        private string GetUserRole(int? roleId)
        {
            if (!roleId.HasValue) return "Не назначена";
            var role = db.ROLES.FirstOrDefault(r => r.id == roleId.Value);
            return role?.name ?? "Не назначена";
        }

        private void BtnChangeAvatar_Click(object sender, RoutedEventArgs e)
        {
            // Показываем контекстное меню при нажатии на карандашик
            AvatarContextMenu.IsOpen = true;
        }

        private void MenuItemSetPhoto_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Выберите фото для профиля",
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Загружаем изображение
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(openFileDialog.FileName);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    AvatarImage.Source = bitmap;
                    AvatarImage.Visibility = Visibility.Visible;
                    AvatarPlaceholder.Visibility = Visibility.Collapsed;

                    // Сохраняем изображение в байты
                    _currentPhotoBytes = File.ReadAllBytes(openFileDialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке фото: {ex.Message}",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void MenuItemEditPhoto_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPhotoBytes == null || _currentPhotoBytes.Length == 0)
            {
                MessageBox.Show("Сначала установите фото", "Информация",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new ImageEditorDialog(_currentPhotoBytes);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                _currentPhotoBytes = dialog.EditedImageBytes;

                // Обновляем отображение
                using (var stream = new MemoryStream(_currentPhotoBytes))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = stream;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    AvatarImage.Source = bitmap;
                    AvatarImage.Visibility = Visibility.Visible;
                    AvatarPlaceholder.Visibility = Visibility.Collapsed;
                }

                MessageBox.Show("Фото успешно отредактировано", "Успех",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void MenuItemDeletePhoto_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPhotoBytes != null || AvatarImage.Source != null)
            {
                var result = MessageBox.Show(
                    "Вы уверены, что хотите удалить фото профиля?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    SetDefaultAvatar();
                }
            }
            else
            {
                MessageBox.Show("Фото профиля не установлено",
                              "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Валидация общих полей
                if (string.IsNullOrWhiteSpace(TxtUsername.Text))
                {
                    MessageBox.Show("Введите имя пользователя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(TxtFullName.Text))
                {
                    MessageBox.Show("Введите полное имя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 2. Логика для НОВОГО пользователя
                if (_isNewUser)
                {
                    if (string.IsNullOrWhiteSpace(TxtPassword.Password))
                    {
                        MessageBox.Show("Введите пароль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (TxtPassword.Password.Length < 6)
                    {
                        MessageBox.Show("Пароль должен содержать минимум 6 символов", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (TxtPassword.Password != TxtConfirmPassword.Password)
                    {
                        MessageBox.Show("Пароли не совпадают", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Заполняем нового пользователя
                    _currentUser.username = TxtUsername.Text;
                    _currentUser.full_name = TxtFullName.Text;
                    _currentUser.email = TxtEmail.Text;
                    _currentUser.phone = TxtPhone.Text;
                    _currentUser.created_at = DateTime.Now;
                    _currentUser.password = TxtPassword.Password;
                    _currentUser.photo = _currentPhotoBytes; // Сохраняем фото

                    if (CmbRole.SelectedItem is ROLES selectedRole)
                    {
                        _currentUser.role_id = selectedRole.id;
                    }

                    db.USERS.Add(_currentUser);
                }
                // 3. Логика для СУЩЕСТВУЮЩЕГО пользователя
                else
                {
                    // ВАЖНО: Загружаем актуальную запись из БД в текущем контексте
                    var userInDb = db.USERS.Find(_currentUser.id);

                    if (userInDb == null)
                    {
                        MessageBox.Show("Пользователь не найден в базе данных", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Обновляем данные у загруженного объекта (который отслеживается контекстом)
                    userInDb.username = TxtUsername.Text;
                    userInDb.full_name = TxtFullName.Text;
                    userInDb.email = TxtEmail.Text;
                    userInDb.phone = TxtPhone.Text;

                    // Обновляем фото, если оно было изменено
                    if (_currentPhotoBytes != null)
                    {
                        userInDb.photo = _currentPhotoBytes;
                    }
                    else if (_currentPhotoBytes == null && AvatarImage.Source == null)
                    {
                        // Если фото было удалено
                        userInDb.photo = null;
                    }

                    if (CmbRole.SelectedItem is ROLES selectedRole)
                    {
                        userInDb.role_id = selectedRole.id;
                    }

                    if (_isChangingPassword)
                    {
                        if (string.IsNullOrWhiteSpace(TxtPassword.Password))
                        {
                            MessageBox.Show("Введите новый пароль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        if (TxtPassword.Password.Length < 6)
                        {
                            MessageBox.Show("Пароль должен содержать минимум 6 символов", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        if (TxtPassword.Password != TxtConfirmPassword.Password)
                        {
                            MessageBox.Show("Пароли не совпадают", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        userInDb.password = TxtPassword.Password;
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
            if (_isNewUser)
            {
                DialogResult = false;
                Close();
                return;
            }

            if (_currentLoggedInUserId > 0 && _currentUser.id == _currentLoggedInUserId)
            {
                MessageBox.Show(
                    "Вы не можете удалить свою собственную учетную запись.\n" +
                    "Это действие запрещено для обеспечения безопасности системы.",
                    "Ошибка удаления",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить пользователя {_currentUser.full_name}?\nЭто действие нельзя отменить.",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var userToDelete = db.USERS.Find(_currentUser.id);

                    if (userToDelete == null)
                    {
                        MessageBox.Show("Пользователь не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var hasMasters = db.MASTERS.Any(m => m.user_id == userToDelete.id);

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

                    db.USERS.Remove(userToDelete);
                    db.SaveChanges();

                    MessageBox.Show("Пользователь успешно удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            if (_isChangingPassword)
            {
                // Скрываем поля пароля
                PasswordSection.Visibility = Visibility.Collapsed;
                ConfirmPasswordSection.Visibility = Visibility.Collapsed;
                BtnChangePassword.Content = "🔑 Сменить пароль";
                _isChangingPassword = false;

                // Очищаем поля
                TxtPassword.Password = "";
                TxtConfirmPassword.Password = "";
            }
            else
            {
                // Показываем поля пароля
                PasswordSection.Visibility = Visibility.Visible;
                ConfirmPasswordSection.Visibility = Visibility.Visible;
                BtnChangePassword.Content = "❌ Отмена";
                _isChangingPassword = true;

                // Очищаем поля перед вводом нового пароля
                TxtPassword.Password = "";
                TxtConfirmPassword.Password = "";

                // Фокус на поле пароля
                TxtPassword.Focus();
            }
        }
    }
}