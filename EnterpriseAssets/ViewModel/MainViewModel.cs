using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using EnterpriseAssets.Model;
using EnterpriseAssets.View;

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
            }
        }

        public string PageTitle
        {
            get => _pageTitle;
            set
            {
                _pageTitle = value;
                OnPropertyChanged();
            }
        }

        public string RoleName => GetRoleName();

        public ICommand LogoutCommand { get; }
        public ICommand NavigateCommand { get; }

        public MainViewModel(User user)
        {
            CurrentUser = user;
            LogoutCommand = new RelayCommand(ExecuteLogout);
            NavigateCommand = new RelayCommand(ExecuteNavigate);
        }

        private string GetRoleName()
        {
            // Здесь должна быть логика получения названия роли
            // Пока заглушка
            return "Администратор";
        }

        private void ExecuteLogout(object parameter)
        {
            // Открываем окно авторизации
            var loginWindow = new LoginWindow();
            loginWindow.Show();

            // Закрываем главное окно
            foreach (Window window in Application.Current.Windows)
            {
                if (window is MainWindow)
                {
                    window.Close();
                    break;
                }
            }
        }

        private void ExecuteNavigate(object parameter)
        {
            if (parameter is string pageName)
            {
                PageTitle = GetPageTitle(pageName);
                // Здесь будет логика навигации
            }
        }

        private string GetPageTitle(string pageName)
        {
            return pageName switch
            {
                "Dashboard" => "Главная панель",
                "Assets" => "Производственные активы",
                // ... остальные как в GetPageTitle выше
                _ => "Учет активов предприятия"
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}