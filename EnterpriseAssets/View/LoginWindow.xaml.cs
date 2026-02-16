using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Controls;
using EnterpriseAssets.Model.DataBase;
using EnterpriseAssets.ViewModel;

namespace EnterpriseAssets.View
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            
            // Подписываемся на событие загрузки
            Loaded += LoginWindow_Loaded;


            // Убеждаемся, что затемнение видимо при старте
            if (LoadingOverlay != null)
            {
                LoadingOverlay.Visibility = Visibility.Visible;
                LoadingOverlay.Opacity = 1;
            }
        }

        private async void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Запускаем проверку БД после полной загрузки окна
            await CheckDatabaseConnectionAsync();
        }

        private async Task CheckDatabaseConnectionAsync()
        {
            try
            {
                // Показываем затемнение с текстом проверки
                ShowOverlay(true, "Проверка подключения к базе данных...");

                // Асинхронная проверка подключения
                bool isConnected = await Task.Run(() =>
                {
                    try
                    {
                        using (var db = new DB_AssetManage())
                        {
                            // Пробуем выполнить простой запрос
                            return db.Database.Exists();
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка при проверке БД: {ex.Message}");
                        return false;
                    }
                });

                if (isConnected)
                {
                    // Успешное подключение - скрываем затемнение
                    await Task.Delay(500); // Небольшая задержка для визуального эффекта
                    
                    // Обновляем статус в ViewModel
                    if (DataContext is LoginViewModel viewModel)
                    {
                        viewModel.IsDatabaseConnected = true;
                    }
                    
                    HideOverlay();
                }
                else
                {
                    // Ошибка подключения - показываем сообщение об ошибке
                    ShowConnectionError("Не удалось подключиться к базе данных.\nПроверьте подключение к серверу.");
                }
            }
            catch (Exception ex)
            {
                ShowConnectionError($"Ошибка: {ex.Message}");
            }
        }

        private void ShowOverlay(bool show, string message = "")
        {
            Dispatcher.Invoke(() =>
            {
                if (LoadingOverlay != null)
                {
                    LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
                    
                    if (show)
                    {
                        LoadingText.Text = message;
                        LoadingText.Foreground = System.Windows.Media.Brushes.White;
                        RetryButton.Visibility = Visibility.Collapsed;
                        
                        // Плавное появление
                        var fadeIn = new DoubleAnimation
                        {
                            From = 0,
                            To = 1,
                            Duration = TimeSpan.FromSeconds(0.3)
                        };
                        LoadingOverlay.BeginAnimation(Border.OpacityProperty, fadeIn);
                    }
                }
            });
        }

        private void HideOverlay()
        {
            Dispatcher.Invoke(() =>
            {
                if (LoadingOverlay != null)
                {
                    // Плавное исчезновение
                    var fadeOut = new DoubleAnimation
                    {
                        From = 1,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0.3)
                    };
                    
                    fadeOut.Completed += (s, e) =>
                    {
                        LoadingOverlay.Visibility = Visibility.Collapsed;
                        
                        // Устанавливаем фокус на поле логина после скрытия затемнения
                        if (LoginBox != null)
                        {
                            LoginBox.Focus();
                            LoginBox.SelectAll();
                        }
                    };
                    
                    LoadingOverlay.BeginAnimation(Border.OpacityProperty, fadeOut);
                }
            });
        }

        private void ShowConnectionError(string errorMessage)
        {
            Dispatcher.Invoke(() =>
            {
                if (LoadingOverlay != null)
                {
                    LoadingText.Text = errorMessage;
                    
                    // Используем красный цвет для ошибки
                    var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E74C3C");
                    LoadingText.Foreground = new System.Windows.Media.SolidColorBrush(color);
                    
                    RetryButton.Visibility = Visibility.Visible;

                    // Обновляем статус в ViewModel
                    if (DataContext is LoginViewModel viewModel)
                    {
                        viewModel.IsDatabaseConnected = false;
                        viewModel.ErrorMessage = errorMessage;
                    }
                    
                    // Убеждаемся, что затемнение видимо
                    LoadingOverlay.Visibility = Visibility.Visible;
                }
            });
        }

        private async void RetryButton_Click(object sender, RoutedEventArgs e)
        {
            // Сбрасываем цвет текста
            if (LoadingText != null)
            {
                LoadingText.Foreground = System.Windows.Media.Brushes.White;
            }
            
            // Повторяем проверку
            await CheckDatabaseConnectionAsync();
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