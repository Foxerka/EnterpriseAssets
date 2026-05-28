using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using EnterpriseAssets.Model.DataBase;
using EnterpriseAssets.Services;

namespace EnterpriseAssets.View
{
    public partial class UserManage : Window
    {
        private DB_AssetManage db = new DB_AssetManage();
        private USERS _currentUser;
        private bool _isNewUser;
        private bool _isChangingPassword = false;
        private byte[] _currentPhotoBytes;
        private int _currentLoggedInUserId;
        private bool _isUpdatingPhone = false;

        public UserManage(USERS user, int currentLoggedInUserId = 0)
        {
            InitializeComponent();
            _currentUser = user;
            _isNewUser = false;
            _currentLoggedInUserId = currentLoggedInUserId;
            LoadUserData();
            LoadRoles();
            LoadUserPhoto();
            SetupInputMasks();

            PasswordSection.Visibility = Visibility.Collapsed;
            ConfirmPasswordSection.Visibility = Visibility.Collapsed;
        }

        public UserManage(int currentLoggedInUserId = 0)
        {
            InitializeComponent();
            _currentUser = new USERS();
            _isNewUser = true;
            _currentLoggedInUserId = currentLoggedInUserId;
            Title = "Добавление нового пользователя";
            LoadRoles();
            SetupInputMasks();

            PasswordSection.Visibility = Visibility.Visible;
            ConfirmPasswordSection.Visibility = Visibility.Visible;
            BtnChangePassword.Visibility = Visibility.Collapsed;
        }

        private void SetupInputMasks()
        {
            // Маска для имени пользователя (латиница, цифры, подчеркивание)
            TxtUsername.PreviewTextInput += (s, e) =>
            {
                e.Handled = !Regex.IsMatch(e.Text, @"^[a-zA-Z0-9_]+$");
            };
            TxtUsername.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Space) e.Handled = true;
            };

            // Маска для полного имени (буквы, пробелы, дефис)
            TxtFullName.PreviewTextInput += (s, e) =>
            {
                e.Handled = !Regex.IsMatch(e.Text, @"^[a-zA-Zа-яА-Яё-Ё\s\-]+$");
            };

            // 🔹 Маска для телефона
            TxtPhone.PreviewTextInput += Phone_PreviewTextInput;
            TxtPhone.PreviewKeyDown += Phone_PreviewKeyDown;
            TxtPhone.TextChanged += Phone_TextChanged;
            DataObject.AddPastingHandler(TxtPhone, Phone_Pasting);
        }

        // 🔹 Разрешаем ввод только цифр
        private void Phone_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        // 🔹 Обработка клавиш: разрешаем только цифры, Backspace, Delete, навигацию
        private void Phone_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Разрешаем служебные клавиши
            if (e.Key == Key.Back || e.Key == Key.Delete ||
                e.Key == Key.Left || e.Key == Key.Right ||
                e.Key == Key.Tab || e.Key == Key.Home || e.Key == Key.End)
                return;

            // Разрешаем цифры с основной клавиатуры и цифрового блока
            if ((e.Key >= Key.D0 && e.Key <= Key.D9) ||
                (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9))
                return;

            // Блокируем всё остальное (буквы, символы и т.д.)
            e.Handled = true;
        }

        // 🔹 Обработка вставки (Ctrl+V)
        private void Phone_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = e.DataObject.GetData(typeof(string)) as string;
                if (!string.IsNullOrEmpty(text))
                {
                    // Извлекаем только цифры из вставляемого текста
                    string digitsOnly = new string(text.Where(char.IsDigit).ToArray());

                    e.CancelCommand(); // Отменяем стандартную вставку

                    var textBox = sender as System.Windows.Controls.TextBox;
                    if (textBox != null)
                    {
                        // Получаем текущие цифры из поля
                        string currentDigits = new string(textBox.Text.Where(char.IsDigit).ToArray());

                        // Добавляем новые цифры
                        string newDigits = currentDigits + digitsOnly;
                        if (newDigits.Length > 11)
                            newDigits = newDigits.Substring(0, 11);

                        // Форматируем и обновляем поле
                        _isUpdatingPhone = true;
                        textBox.Text = FormatPhoneNumber(newDigits);
                        textBox.SelectionStart = textBox.Text.Length;
                        _isUpdatingPhone = false;
                    }
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        // 🔹 Форматирование при изменении текста
        private void Phone_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_isUpdatingPhone) return;

            var textBox = sender as System.Windows.Controls.TextBox;
            if (textBox == null) return;

            _isUpdatingPhone = true;

            try
            {
                // Считаем, сколько цифр было ДО позиции курсора (для корректного позиционирования)
                int digitsBeforeCursor = textBox.Text.Take(textBox.SelectionStart).Count(char.IsDigit);

                // Получаем только цифры
                string digits = new string(textBox.Text.Where(char.IsDigit).ToArray());

                // Форматируем номер
                string formatted = FormatPhoneNumber(digits);

                // Обновляем текст, если изменился
                if (textBox.Text != formatted)
                {
                    textBox.Text = formatted;

                    // 🔹 Расчёт новой позиции курсора
                    int newCursorPos = 0;
                    int digitCounter = 0;

                    // Проходим по отформатированной строке и считаем позиции цифр
                    for (int i = 0; i < formatted.Length; i++)
                    {
                        if (char.IsDigit(formatted[i]))
                        {
                            if (digitCounter < digitsBeforeCursor)
                                digitCounter++;
                            else
                                break;
                        }
                        newCursorPos++;
                    }

                    // Если курсор был в конце — ставим в конец
                    if (digitsBeforeCursor >= digits.Length)
                        newCursorPos = formatted.Length;

                    textBox.SelectionStart = newCursorPos;
                }
            }
            finally
            {
                _isUpdatingPhone = false;
            }
        }

        // 🔹 Форматирование номера: +7 (123) 456-78-90
        private string FormatPhoneNumber(string digits)
        {
            if (string.IsNullOrEmpty(digits))
                return string.Empty;

            // Очищаем от не-цифр и ограничиваем 11 символами
            digits = new string(digits.Where(char.IsDigit).ToArray());
            if (digits.Length > 11)
                digits = digits.Substring(0, 11);

            if (digits.Length == 0)
                return string.Empty;

            // Нормализация: если начинается с 8 → заменяем на 7
            if (digits[0] == '8')
                digits = "7" + (digits.Length > 1 ? digits.Substring(1) : "");
            // Если не начинается с 7 → добавляем 7
            else if (digits[0] != '7')
                digits = "7" + digits;

            // Повторная обрезка после нормализации
            if (digits.Length > 11)
                digits = digits.Substring(0, 11);

            // 🔹 Построение формата через StringBuilder (эффективно и читаемо)
            var result = new StringBuilder();
            result.Append('+');

            // Код страны: 1 цифра
            result.Append(digits[0]);
            if (digits.Length == 1) return result.ToString();

            // Код оператора: 3 цифры в скобках
            result.Append(" (");
            for (int i = 1; i < Math.Min(digits.Length, 4); i++)
                result.Append(digits[i]);

            if (digits.Length <= 4) return result.Append(')').ToString();
            result.Append(") ");

            // Первая часть номера: 3 цифры
            for (int i = 4; i < Math.Min(digits.Length, 7); i++)
                result.Append(digits[i]);

            if (digits.Length <= 7) return result.ToString();
            result.Append('-');

            // Вторая часть: 2 цифры
            for (int i = 7; i < Math.Min(digits.Length, 9); i++)
                result.Append(digits[i]);

            if (digits.Length <= 9) return result.ToString();
            result.Append('-');

            // Третья часть: последние 2 цифры
            for (int i = 9; i < digits.Length; i++)
                result.Append(digits[i]);

            return result.ToString();
        }

        // 🔹 Получение «чистого» номера для сохранения в БД
        private string UnformatPhoneNumber(string formattedPhone)
        {
            if (string.IsNullOrWhiteSpace(formattedPhone))
                return string.Empty;

            return new string(formattedPhone.Where(char.IsDigit).ToArray());
        }

        private void LoadUserData()
        {
            if (_currentUser == null) return;

            TxtUsername.Text = _currentUser.username;
            TxtFullName.Text = _currentUser.full_name;
            TxtEmail.Text = _currentUser.email;

            // Применяем форматирование к телефону
            string phoneDigits = _currentUser.phone ?? "";
            TxtPhone.Text = FormatPhoneNumber(phoneDigits);

            UserFullName.Text = _currentUser.full_name ?? "Новый пользователь";
            UserRole.Text = GetUserRole(_currentUser.role_id);

            if (_currentUser.created_at.HasValue)
                TxtCreatedAt.Text = $"📅 Дата создания: {_currentUser.created_at:dd.MM.yyyy HH:mm}";
            else
                TxtCreatedAt.Text = "📅 Новый пользователь";

            TxtLastLogin.Text = "🕐 Последний вход: не выполнялся";
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
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(openFileDialog.FileName);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    AvatarImage.Source = bitmap;
                    AvatarImage.Visibility = Visibility.Visible;
                    AvatarPlaceholder.Visibility = Visibility.Collapsed;
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

        private bool ValidateFields()
        {
            if (string.IsNullOrWhiteSpace(TxtUsername.Text))
            {
                ShowWarning("Введите имя пользователя (латиница, цифры)");
                TxtUsername.Focus();
                return false;
            }

            if (!Regex.IsMatch(TxtUsername.Text, @"^[a-zA-Z0-9_]+$"))
            {
                ShowWarning("Имя пользователя может содержать только латинские буквы, цифры и подчеркивание");
                TxtUsername.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(TxtFullName.Text))
            {
                ShowWarning("Введите полное имя");
                TxtFullName.Focus();
                return false;
            }

            if (CmbRole.SelectedItem == null)
            {
                ShowWarning("Выберите роль пользователя");
                CmbRole.Focus();
                return false;
            }

            return true;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateFields()) return;

                // Сохраняем телефон в базовом формате (только цифры)
                string phoneDigits = UnformatPhoneNumber(TxtPhone.Text);
                string formattedPhone = string.IsNullOrEmpty(phoneDigits) ? null : FormatPhoneNumber(phoneDigits);

                if (_isNewUser)
                {
                    if (string.IsNullOrWhiteSpace(TxtPassword.Password))
                    {
                        ShowWarning("Введите пароль");
                        TxtPassword.Focus();
                        return;
                    }

                    if (TxtPassword.Password.Length < 6)
                    {
                        ShowWarning("Пароль должен содержать минимум 6 символов");
                        TxtPassword.Focus();
                        return;
                    }

                    if (TxtPassword.Password != TxtConfirmPassword.Password)
                    {
                        ShowWarning("Пароли не совпадают");
                        TxtPassword.Focus();
                        return;
                    }

                    _currentUser.username = TxtUsername.Text;
                    _currentUser.full_name = TxtFullName.Text;
                    _currentUser.email = TxtEmail.Text;
                    _currentUser.phone = phoneDigits; // Сохраняем только цифры
                    _currentUser.created_at = DateTime.Now;
                    _currentUser.password = TxtPassword.Password;
                    _currentUser.photo = _currentPhotoBytes;

                    if (CmbRole.SelectedItem is ROLES selectedRole)
                    {
                        _currentUser.role_id = selectedRole.id;
                    }

                    db.USERS.Add(_currentUser);
                }
                else
                {
                    var userInDb = db.USERS.Find(_currentUser.id);

                    if (userInDb == null)
                    {
                        ShowError("Пользователь не найден в базе данных");
                        return;
                    }

                    userInDb.username = TxtUsername.Text;
                    userInDb.full_name = TxtFullName.Text;
                    userInDb.email = TxtEmail.Text;
                    userInDb.phone = phoneDigits; // Сохраняем только цифры

                    if (_currentPhotoBytes != null)
                    {
                        userInDb.photo = _currentPhotoBytes;
                    }
                    else if (_currentPhotoBytes == null && AvatarImage.Source == null)
                    {
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
                            ShowWarning("Введите новый пароль");
                            TxtPassword.Focus();
                            return;
                        }

                        if (TxtPassword.Password.Length < 6)
                        {
                            ShowWarning("Пароль должен содержать минимум 6 символов");
                            TxtPassword.Focus();
                            return;
                        }

                        if (TxtPassword.Password != TxtConfirmPassword.Password)
                        {
                            ShowWarning("Пароли не совпадают");
                            TxtPassword.Focus();
                            return;
                        }

                        userInDb.password = TxtPassword.Password;
                    }
                }

                db.SaveChanges();

                var logger = new UserLogger(_currentLoggedInUserId);
                if (_isNewUser)
                {
                    logger.LogCreate("USER", _currentUser.id, TxtUsername.Text);
                }
                else
                {
                    logger.LogUpdate("USER", _currentUser.id, TxtUsername.Text, "Обновлены данные пользователя");
                }

                ShowSuccess("Данные успешно сохранены");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка сохранения: {ex.Message}");
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
                ShowWarning("Вы не можете удалить свою собственную учетную запись.\nЭто действие запрещено для обеспечения безопасности системы.");
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
                        ShowError("Пользователь не найден");
                        return;
                    }

                    var hasMasters = db.MASTERS.Any(m => m.user_id == userToDelete.id);

                    if (hasMasters)
                    {
                        ShowWarning("Невозможно удалить пользователя, так как он является МОЛ.\nСначала удалите или переназначьте связанные записи.");
                        return;
                    }

                    db.USERS.Remove(userToDelete);
                    db.SaveChanges();

                    ShowSuccess("Пользователь успешно удален");
                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    ShowError($"Ошибка удаления: {ex.Message}");
                }
            }
        }

        private void BtnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            if (_isChangingPassword)
            {
                PasswordSection.Visibility = Visibility.Collapsed;
                ConfirmPasswordSection.Visibility = Visibility.Collapsed;
                BtnChangePassword.Content = "🔑 Сменить пароль";
                _isChangingPassword = false;

                TxtPassword.Password = "";
                TxtConfirmPassword.Password = "";
            }
            else
            {
                PasswordSection.Visibility = Visibility.Visible;
                ConfirmPasswordSection.Visibility = Visibility.Visible;
                BtnChangePassword.Content = "❌ Отмена";
                _isChangingPassword = true;

                TxtPassword.Password = "";
                TxtConfirmPassword.Password = "";
                TxtPassword.Focus();
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
    }
}