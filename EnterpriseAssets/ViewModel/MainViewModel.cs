using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using EnterpriseAssets.Model;

namespace EnterpriseAssets.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private User _currentUser;
        private string _pageTitle = "Главная панель";

        public User CurrentUser
        {
            get => _currentUser;
            set
            {
                _currentUser = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FullName));
                OnPropertyChanged(nameof(RoleName));
            }
        }

        public string FullName => CurrentUser?.FullName ?? "Пользователь";

        public string RoleName => CurrentUser?.RoleName ?? "Пользователь";

        public string PageTitle
        {
            get => _pageTitle;
            set
            {
                _pageTitle = value;
                OnPropertyChanged();
            }
        }

        public ICommand LogoutCommand { get; }

        public MainViewModel(User user)
        {
            CurrentUser = user;
            LogoutCommand = new RelayCommand(ExecuteLogout);
        }

        private void ExecuteLogout(object parameter)
        {
            // Открываем окно авторизации
            var loginWindow = new View.LoginWindow();
            loginWindow.Show();

            // Закрываем главное окно
            foreach (Window window in Application.Current.Windows)
            {
                if (window is View.MainWindow)
                {
                    window.Close();
                    break;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}