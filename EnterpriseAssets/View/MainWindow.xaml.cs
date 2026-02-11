using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using EnterpriseAssets.ViewModel;

namespace EnterpriseAssets.View
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;
        private Button? _currentNavButton;
        private bool _isPanelCollapsed = false;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as MainViewModel;
            if (_viewModel != null)
            {
                // Устанавливаем первую кнопку активной
                SetActiveNavButton(BtnDashboard);
                NavigateToPage("Dashboard");
            }
        }

        private void BtnMenuToggle_Click(object sender, RoutedEventArgs e)
        {
            if (_isPanelCollapsed)
            {
                ExpandPanel();// Разворачиваем панель
            }
            else
            {
                CollapsePanel();// Сворачиваем панель
            }
        }

        private void CollapsePanel()
        {
            // Изменяем ширину без анимации (можно добавить GridLengthAnimation для плавной анимации(выключена пока))
            LeftColumn.Width = new GridLength(50);

            // Скрываем тексты
            PanelTitle.Visibility = Visibility.Collapsed;
            PanelTitle.Margin = new Thickness(0);

            // Скрываем названия групп
            var groups = new[] { GroupBasic, GroupAssets, GroupProduction, GroupPurchases,
                                GroupMaintenance, GroupReports, GroupAdmin };
            foreach (var group in groups)
            {
                if (group != null)
                {
                    group.Visibility = Visibility.Collapsed;
                    group.Margin = new Thickness(0);
                }
            }

            // Скрываем тексты кнопок
            var buttonTexts = new[] {
                BtnDashboardText, BtnAssetsText, BtnEquipmentText, BtnWarehouseText,
                BtnWorkActsText, BtnSessionsText, BtnProductsText, BtnPurchasesText,
                BtnSuppliersText, BtnMaintenanceText, BtnReportsText, BtnAnalyticsText,
                BtnUsersText
            };
            foreach (var text in buttonTexts)
            {
                if (text != null)
                {
                    text.Visibility = Visibility.Collapsed;
                    text.Margin = new Thickness(0);
                }
            }

            // Центрируем иконки кнопок
            var buttons = new[] {
                BtnDashboard, BtnAssets, BtnEquipment, BtnWarehouse,
                BtnWorkActs, BtnSessions, BtnProducts, BtnPurchases,
                BtnSuppliers, BtnMaintenance, BtnReports, BtnAnalytics,
                BtnUsers
            };
            foreach (var button in buttons)
            {
                if (button?.Content is StackPanel stackPanel)
                {
                    stackPanel.HorizontalAlignment = HorizontalAlignment.Center;
                    // Уменьшаем отступ для иконок
                    stackPanel.Margin = new Thickness(0, 0, 0, 0);
                }
            }

            // Уменьшаем отступы внутри панели
            NavStackPanel.Margin = new Thickness(0, 20, 0, 0);

            // Скрываем детали пользователя
            if (UserDetailsPanel != null)
            {
                UserDetailsPanel.Visibility = Visibility.Collapsed;
                UserDetailsPanel.Margin = new Thickness(0);
            }

            if (UserNameText != null)
            {
                UserNameText.Visibility = Visibility.Collapsed;
            }

            if (UserRoleText != null)
            {
                UserRoleText.Visibility = Visibility.Collapsed;
            }

            if (LogoutText != null)
            {
                LogoutText.Visibility = Visibility.Collapsed;
            }

            if (UserInfoPanel != null)
            {
                UserInfoPanel.HorizontalAlignment = HorizontalAlignment.Center;
                UserInfoPanel.Margin = new Thickness(0, 0, 0, 10);
            }

            if (LogoutStackPanel != null)
            {
                LogoutStackPanel.HorizontalAlignment = HorizontalAlignment.Center;
                LogoutStackPanel.Margin = new Thickness(0);
            }

            // Изменяем иконку кнопки сворачивания
            MenuToggleIcon.Text = "▶";

            // Уменьшаем отступы кнопки выхода
            if (BtnLogout != null)
            {
                BtnLogout.Padding = new Thickness(5);
            }

            _isPanelCollapsed = true;
        }

        private void ExpandPanel()
        {
            // Возвращаем ширину
            LeftColumn.Width = new GridLength(220);

            // Показываем тексты
            PanelTitle.Visibility = Visibility.Visible;
            PanelTitle.Margin = new Thickness(10, 0, 0, 0);

            // Показываем названия групп
            var groups = new[] { GroupBasic, GroupAssets, GroupProduction, GroupPurchases,
                                GroupMaintenance, GroupReports, GroupAdmin };
            foreach (var group in groups)
            {
                if (group != null)
                {
                    group.Visibility = Visibility.Visible;
                    group.Margin = new Thickness(15, 20, 0, 5);
                }
            }

            // Показываем тексты кнопок
            var buttonTexts = new[] {
                BtnDashboardText, BtnAssetsText, BtnEquipmentText, BtnWarehouseText,
                BtnWorkActsText, BtnSessionsText, BtnProductsText, BtnPurchasesText,
                BtnSuppliersText, BtnMaintenanceText, BtnReportsText, BtnAnalyticsText,
                BtnUsersText
            };
            foreach (var text in buttonTexts)
            {
                if (text != null)
                {
                    text.Visibility = Visibility.Visible;
                    text.Margin = new Thickness(10, 0, 0, 0);
                }
            }

            // Возвращаем выравнивание кнопок
            var buttons = new[] {
                BtnDashboard, BtnAssets, BtnEquipment, BtnWarehouse,
                BtnWorkActs, BtnSessions, BtnProducts, BtnPurchases,
                BtnSuppliers, BtnMaintenance, BtnReports, BtnAnalytics,
                BtnUsers
            };
            foreach (var button in buttons)
            {
                if (button?.Content is StackPanel stackPanel)
                {
                    stackPanel.HorizontalAlignment = HorizontalAlignment.Left;
                    stackPanel.Margin = new Thickness(0, 0, 0, 0);
                }
            }

            // Возвращаем отступы
            NavStackPanel.Margin = new Thickness(0, 20, 0, 0);

            // Показываем детали пользователя
            if (UserDetailsPanel != null)
            {
                UserDetailsPanel.Visibility = Visibility.Visible;
                UserDetailsPanel.Margin = new Thickness(10, 0, 0, 0);
            }

            if (UserNameText != null)
            {
                UserNameText.Visibility = Visibility.Visible;
            }

            if (UserRoleText != null)
            {
                UserRoleText.Visibility = Visibility.Visible;
            }

            if (LogoutText != null)
            {
                LogoutText.Visibility = Visibility.Visible;
            }

            if (UserInfoPanel != null)
            {
                UserInfoPanel.HorizontalAlignment = HorizontalAlignment.Left;
                UserInfoPanel.Margin = new Thickness(0, 0, 0, 10);
            }

            if (LogoutStackPanel != null)
            {
                LogoutStackPanel.HorizontalAlignment = HorizontalAlignment.Center;
            }

            // Изменяем иконку кнопки сворачивания
            MenuToggleIcon.Text = "◀";

            // Возвращаем отступы кнопки выхода
            if (BtnLogout != null)
            {
                BtnLogout.Padding = new Thickness(10, 5, 10, 5);
            }

            _isPanelCollapsed = false;
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                SetActiveNavButton(button);
                string pageTag = button.Tag?.ToString() ?? "Dashboard";
                NavigateToPage(pageTag);
            }
        }

        private void SetActiveNavButton(Button? activeButton)
        {
            // Сброс стиля для всех кнопок
            ResetNavButtons();

            // Установка активного стиля для выбранной кнопки
            if (activeButton != null)
            {
                var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4A6FA5"));
                activeButton.Background = brush;

                var textBlocks = FindVisualChildren<TextBlock>(activeButton);
                foreach (var textBlock in textBlocks)
                {
                    textBlock.Foreground = Brushes.White;
                }

                _currentNavButton = activeButton;
            }
        }

        private void ResetNavButtons()
        {
            var buttons = new[]
            {
                BtnDashboard, BtnAssets, BtnEquipment, BtnWarehouse,
                BtnWorkActs, BtnSessions, BtnProducts, BtnPurchases,
                BtnSuppliers, BtnMaintenance, BtnReports, BtnAnalytics,
                BtnUsers
            };

            foreach (var button in buttons)
            {
                if (button != null)
                {
                    button.Background = Brushes.Transparent;

                    var textBlocks = FindVisualChildren<TextBlock>(button);
                    foreach (var textBlock in textBlocks)
                    {
                        textBlock.Foreground = Brushes.White;
                    }
                }
            }
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        private void NavigateToPage(string pageName)
        {
            if (_viewModel != null)
            {
                _viewModel.PageTitle = GetPageTitle(pageName);
            }

            // Временное содержимое для демонстрации
            var textBlock = new TextBlock
            {
                Text = $"Страница: {GetPageTitle(pageName)}\n",
                FontSize = 18,
                Foreground = Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };

            var contentGrid = new Grid();
            contentGrid.Children.Add(textBlock);

            MainFrame.Content = contentGrid;
        }

        private string GetPageTitle(string pageName)
        {
            return pageName switch
            {
                "Dashboard" => "Главная панель",
                "Assets" => "Производственные активы",
                "Equipment" => "Оборудование",
                "Warehouse" => "Управление складом",
                "WorkActs" => "Акты выполненных работ",
                "Sessions" => "Рабочие сессии станков",
                "Products" => "Готовая продукция",
                "Purchases" => "Закупки оборудования",
                "Suppliers" => "Поставщики",
                "Maintenance" => "Техническое обслуживание",
                "Reports" => "Отчеты",
                "Analytics" => "Аналитика",
                "Users" => "Управление пользователями",
                _ => "Учет активов предприятия"
            };
        }
    }
}