using EnterpriseAssets.Model;
using EnterpriseAssets.Model.DataBase;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace EnterpriseAssets.ViewModel
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private string _username = "admin";
        private string _password = "admin123";
        private string _errorMessage;
        private bool _isLoading;
        private bool _isDatabaseConnected;

        private DB_AssetManage db = new DB_AssetManage();

        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanLogin));
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanLogin));
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanLogin));
            }
        }

        public bool IsDatabaseConnected
        {
            get => _isDatabaseConnected;
            set
            {
                _isDatabaseConnected = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanLogin));
            }
        }

        // CanLogin теперь зависит от IsDatabaseConnected
        public bool CanLogin => !string.IsNullOrWhiteSpace(Username) &&
                               !string.IsNullOrWhiteSpace(Password) &&
                               !IsLoading &&
                               IsDatabaseConnected; // Добавили проверку подключения к БД

        public ICommand LoginCommand { get; }
        public ICommand CloseCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(ExecuteLogin, CanExecuteLogin);
            CloseCommand = new RelayCommand(ExecuteClose);

            // По умолчанию считаем, что БД не подключена
            IsDatabaseConnected = false;
        }

        private bool CanExecuteLogin(object parameter)
        {
            return CanLogin;
        }

        private void ExecuteLogin(object parameter)
        {
            ErrorMessage = string.Empty;
            IsLoading = true;

            // Запускаем асинхронную авторизацию
            Task.Run(() => AuthenticateUserAsync());
        }

        private async Task AuthenticateUserAsync()
        {
            try
            {
                // Ищем пользователя в базе данных по username и password
                var user = await Task.Run(() =>
                    db.USERS.FirstOrDefault(u => u.username == Username && u.password == Password));

                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (user != null)
                    {
                        LoginSuccess(user);
                    }
                    else
                    {
                        ErrorMessage = "Неверное имя пользователя или пароль";
                        IsLoading = false;
                    }
                });
            }
            catch (System.Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ErrorMessage = $"Ошибка при авторизации: {ex.Message}";
                    IsLoading = false;
                });
            }
        }

        private void LoginSuccess(USERS dbUser)
        {
            // Получаем роль пользователя
            string roleName = GetUserRole(dbUser.role_id);

            // Создаем объект User для ViewModel
            var currentUser = new User
            {
                Id = dbUser.id,
                Username = dbUser.username,
                FullName = dbUser.full_name,
                Email = dbUser.email,
                Phone = dbUser.phone,
                RoleId = dbUser.role_id,
                RoleName = roleName
            };

            // Открываем главное окно
            Application.Current.Dispatcher.Invoke(() =>
            {
                var mainWindow = new View.MainWindow();
                var mainViewModel = new MainViewModel(currentUser);
                mainWindow.DataContext = mainViewModel;
                mainWindow.Show();

                // Закрываем окно авторизации
                foreach (Window window in Application.Current.Windows)
                {
                    if (window is View.LoginWindow)
                    {
                        window.Close();
                        break;
                    }
                }
            });
        }

        private string GetUserRole(int? roleId)
        {
            if (!roleId.HasValue)
                return "Пользователь";

            try
            {
                var role = db.ROLES.FirstOrDefault(r => r.id == roleId.Value);
                return role?.name ?? "Пользователь";
            }
            catch
            {
                return "Пользователь";
            }
        }

        private void ExecuteClose(object parameter)
        {
            Application.Current.Shutdown();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}