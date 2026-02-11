using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using EnterpriseAssets.Model;

namespace EnterpriseAssets.ViewModel
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private string _login = "admin";
        private string _password = "admin";
        private string _errorMessage;
        private bool _isLoading;

        public string Login
        {
            get => _login;
            set
            {
                _login = value;
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

        public bool CanLogin => !string.IsNullOrWhiteSpace(Login) &&
                               !string.IsNullOrWhiteSpace(Password) &&
                               !IsLoading;

        public ICommand LoginCommand { get; }
        public ICommand CloseCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(ExecuteLogin, CanExecuteLogin);
            CloseCommand = new RelayCommand(ExecuteClose);
        }

        private bool CanExecuteLogin(object parameter)
        {
            return CanLogin;
        }

        private void ExecuteLogin(object parameter)
        {
            ErrorMessage = string.Empty;
            IsLoading = true;

            // Имитация проверки логина/пароля (без БД)
            if (AuthenticateUser())
            {
                // Создаем пользователя
                var currentUser = new User
                {
                    Login = Login,
                    FullName = "Администратор системы" // В реальном приложении получаем из БД
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
            else
            {
                ErrorMessage = "Неверный логин или пароль";
                IsLoading = false;
            }
        }

        private void ExecuteClose(object parameter)
        {
            Application.Current.Shutdown();
        }

        private bool AuthenticateUser()
        {
            // Временная заглушка для авторизации
            // В реальном приложении здесь будет обращение к БД
            System.Threading.Thread.Sleep(500); // Имитация задержки

            return Login == "admin" && Password == "admin";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}