using System.Windows;
using EnterpriseAssets.ViewModel;

namespace EnterpriseAssets.View
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            Loaded += LoginWindow_Loaded;
        }

        private void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Устанавливаем начальные значения
            var viewModel = DataContext as LoginViewModel;
            if (viewModel != null)
            {
                PasswordBox.Password = viewModel.Password;
            }

            LoginBox.Focus();
            LoginBox.SelectAll();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel viewModel)
            {
                viewModel.Password = PasswordBox.Password;
            }
        }
    }
}