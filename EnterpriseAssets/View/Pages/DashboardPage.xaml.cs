using EnterpriseAssets.Model.DataBase;
using EnterpriseAssets.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace EnterpriseAssets.View.Pages
{
    public partial class DashboardPage : Page
    {
        private DB_AssetManage db = new DB_AssetManage();
        private USERS _currentUser;
        private string _userRole;
        private ActionLogger _actionLogger = new ActionLogger();

        public DashboardPage()
        {
            InitializeComponent();
        }
        public DashboardPage(USERS user) : this()
        {
            _currentUser = user;
            _userRole = user?.ROLES?.name ?? "Пользователь";
            LoadDashboard();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (_currentUser == null)
            {
                LoadDemoDashboard();
            }
        }

        public void SetCurrentUser(int userId)
        {
            try
            {
                _currentUser = db.USERS.Include("ROLES").FirstOrDefault(u => u.id == userId);
                _userRole = _currentUser?.ROLES?.name ?? "Пользователь";
                LoadDashboard();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки пользователя: {ex.Message}");
                LoadDemoDashboard();
            }
        }

        private void LoadDashboard()
        {
            WelcomeText.Text = $"Здравствуйте, {_currentUser?.full_name ?? "Пользователь"}!";
            RoleText.Text = $"Ваша роль: {_userRole}";
            DateText.Text = $"Сегодня: {DateTime.Now:dd MMMM yyyy, dddd}";

            LoadStatistics();
            ShowRoleContent();
            LoadRecentActions(); 
        }

        private void LoadDemoDashboard()
        {
            WelcomeText.Text = "Здравствуйте, Гость!";
            RoleText.Text = "Ваша роль: Демо-режим";
            DateText.Text = $"Сегодня: {DateTime.Now:dd MMMM yyyy, dddd}";

            Stat1Value.Text = "0";
            Stat2Value.Text = "0";
            Stat3Value.Text = "0";
            Stat4Value.Text = "0";

            Stat1Label.Text = "Пользователей";
            Stat2Label.Text = "Оборудования";
            Stat3Label.Text = "Активов";
            Stat4Label.Text = "Заказов";

            ShowDemoContent();
        }

        private void LoadStatistics()
        {
            try
            {
                int usersCount = db.USERS.Count();
                Stat1Value.Text = usersCount.ToString();
                Stat1Label.Text = "Пользователей";

                int equipmentCount = db.EQUIPMENT.Count();
                Stat2Value.Text = equipmentCount.ToString();
                Stat2Label.Text = "Оборудования";

                // Проверяем наличие таблиц, если нет - ставим 0
                int assetsCount = 0;
                try { assetsCount = db.EQUIPMENT.Count(); } catch { assetsCount = 0; }
                Stat3Value.Text = assetsCount.ToString();
                Stat3Label.Text = "Активов";

                int purchasesCount = 0;
                try { purchasesCount = db.EQUIPMENT_PURCHASES.Count(); } catch { purchasesCount = 0; }
                Stat4Value.Text = purchasesCount.ToString();
                Stat4Label.Text = "Закупок";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки статистики: {ex.Message}");
                Stat1Value.Text = "0";
                Stat2Value.Text = "0";
                Stat3Value.Text = "0";
                Stat4Value.Text = "0";
            }
        }


        private void LoadRecentActions()
        {
            try
            {
                // Получаем последние действия из таблицы USER_LOGS
                var logs = db.USER_LOGS
                    .Include("USERS")
                    .OrderByDescending(l => l.created_at)
                    .Take(10)
                    .ToList();

                if (logs.Any())
                {
                    var result = logs.Select(l => new
                    {
                        Icon = GetIconForAction(l.action_type),
                        Action = $"{l.USERS?.full_name ?? "Пользователь"}: {l.description}",
                        Time = GetTimeString(l.created_at)
                    }).ToList();

                    RecentActionsList.ItemsSource = result;
                }
                else
                {
                    // Если логов нет, показываем демо-данные
                    RecentActionsList.ItemsSource = new[]
                    {
                new { Icon = "👤", Action = "Система готова к работе", Time = "только что" },
                new { Icon = "📋", Action = "Начните добавлять данные", Time = "только что" }
            };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки логов: {ex.Message}");
                RecentActionsList.ItemsSource = new[]
                {
            new { Icon = "👤", Action = "Добро пожаловать!", Time = "только что" }
        };
            }
        }

        private string GetIconForAction(string actionType)
        {
            return actionType switch
            {
                "LOGIN" => "🔑",
                "LOGOUT" => "🚪",
                "CREATE" => "➕",
                "UPDATE" => "✏️",
                "DELETE" => "🗑️",
                "VIEW" => "👁️",
                _ => "📌"
            };
        }

        private string GetTimeString(DateTime time)
        {
            var diff = DateTime.Now - time;
            if (diff.TotalMinutes < 1) return "только что";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} мин назад";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} ч назад";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} дн назад";
            return time.ToString("dd.MM.yyyy");
        }

        // Вспомогательный класс
        public class RecentActionItem
        {
            public string Icon { get; set; }
            public string Action { get; set; }
            public DateTime Time { get; set; }
        }

        private void ShowRoleContent()
        {
            AdminContent.Visibility = Visibility.Collapsed;
            DirectorContent.Visibility = Visibility.Collapsed;
            WorkshopManagerContent.Visibility = Visibility.Collapsed;
            MasterContent.Visibility = Visibility.Collapsed;
            StorekeeperContent.Visibility = Visibility.Collapsed;
            OperatorContent.Visibility = Visibility.Collapsed;

            switch (_userRole)
            {
                case "Администратор":
                    AdminContent.Visibility = Visibility.Visible;
                    LoadAdminData();
                    break;
                case "Директор":
                    DirectorContent.Visibility = Visibility.Visible;
                    LoadDirectorData();
                    break;
                case "Начальник цеха":
                    WorkshopManagerContent.Visibility = Visibility.Visible;
                    LoadWorkshopManagerData();
                    break;
                case "Мастер":
                    MasterContent.Visibility = Visibility.Visible;
                    LoadMasterData();
                    break;
                case "Кладовщик":
                    StorekeeperContent.Visibility = Visibility.Visible;
                    LoadStorekeeperData();
                    break;
                case "Оператор":
                    OperatorContent.Visibility = Visibility.Visible;
                    LoadOperatorData();
                    break;
                default:
                    OperatorContent.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void ShowDemoContent()
        {
            AdminContent.Visibility = Visibility.Collapsed;
            DirectorContent.Visibility = Visibility.Collapsed;
            WorkshopManagerContent.Visibility = Visibility.Collapsed;
            MasterContent.Visibility = Visibility.Collapsed;
            StorekeeperContent.Visibility = Visibility.Collapsed;
            OperatorContent.Visibility = Visibility.Visible;
        }

        private void LoadAdminData()
        {
            var recentActions = new List<RecentAction>
            {
                new RecentAction { Icon = "👤", Action = "Добавлен новый пользователь", Time = "2 часа назад" },
                new RecentAction { Icon = "🔑", Action = "Обновлены права доступа", Time = "5 часов назад" },
                new RecentAction { Icon = "🔧", Action = "Добавлен новый мастер", Time = "вчера" }
            };
            RecentActionsList.ItemsSource = recentActions;
        }

        private void LoadDirectorData()
        {
            var kpis = new List<KPI>
            {
                new KPI { Name = "Общая стоимость активов", Value = "45 200 000 ₸" },
                new KPI { Name = "Количество оборудования", Value = "156 ед." },
                new KPI { Name = "Загруженность мастеров", Value = "78%" }
            };
            KPIList.ItemsSource = kpis;
        }

        private void LoadWorkshopManagerData()
        {
            var activeTasks = new List<ActiveTask>
            {
                new ActiveTask { Title = "Ремонт станка ЧПУ", Description = "Требуется замена подшипников", Status = "В работе", StatusColor = "#F39C12" },
                new ActiveTask { Title = "Плановое ТО", Description = "Обслуживание конвейерной линии", Status = "Запланировано", StatusColor = "#3498DB" }
            };
            ActiveTasksList.ItemsSource = activeTasks;
        }

        private void LoadMasterData()
        {
            var tasks = new List<MasterTask>
            {
                new MasterTask { TaskId = 1, TaskName = "Ремонт станка", Equipment = "Токарный станок", Deadline = "Дедлайн: 25.12.2024" },
                new MasterTask { TaskId = 2, TaskName = "Замена масла", Equipment = "Гидравлический пресс", Deadline = "Дедлайн: 28.12.2024" }
            };
            MasterTasksList.ItemsSource = tasks;
        }

        private void LoadStorekeeperData()
        {
            var lowStock = new List<LowStockItem>
            {
                new LowStockItem { ItemName = "Подшипники SKF 6204", Quantity = "Остаток: 5 шт." },
                new LowStockItem { ItemName = "Моторное масло", Quantity = "Остаток: 15 л" }
            };
            LowStockList.ItemsSource = lowStock;
        }

        private void LoadOperatorData()
        {
            var recentDocs = new List<RecentDocument>
            {
                new RecentDocument { DocNumber = "Заказ №П-2024-001", Date = "20.12.2024", Status = "В обработке", StatusColor = "#F39C12" },
                new RecentDocument { DocNumber = "Накладная №Н-2024-089", Date = "19.12.2024", Status = "Завершен", StatusColor = "#27AE60" }
            };
            RecentDocumentsList.ItemsSource = recentDocs;
        }

        // Обработчики кнопок
        private void AdminUsersBtn_Click(object sender, RoutedEventArgs e) => NavigateToPage("Users");
        private void AdminRolesBtn_Click(object sender, RoutedEventArgs e) => NavigateToPage("Roles");
        private void AdminMastersBtn_Click(object sender, RoutedEventArgs e) => NavigateToPage("Masters");
        private void AdminReportsBtn_Click(object sender, RoutedEventArgs e) => ShowMessage("Функция отчетов в разработке");

        private void DirectorReportsBtn_Click(object sender, RoutedEventArgs e) => ShowMessage("Функция финансовых отчетов в разработке");
        private void DirectorAnalyticsBtn_Click(object sender, RoutedEventArgs e) => ShowMessage("Функция аналитики в разработке");
        private void DirectorAssetsBtn_Click(object sender, RoutedEventArgs e) => NavigateToPage("Assets");

        private void WorkshopEquipmentBtn_Click(object sender, RoutedEventArgs e) => NavigateToPage("Equipment");
        private void WorkshopMastersBtn_Click(object sender, RoutedEventArgs e) => NavigateToPage("Masters");
        private void WorkshopTasksBtn_Click(object sender, RoutedEventArgs e) => ShowMessage("Функция управления задачами в разработке");

        private void MasterMyTasksBtn_Click(object sender, RoutedEventArgs e) => ShowMessage("Список ваших задач");
        private void StartTask_Click(object sender, RoutedEventArgs e) => ShowMessage("Задача взята в работу");

        private void StorekeeperStockBtn_Click(object sender, RoutedEventArgs e) => NavigateToPage("Warehouse");
        private void StorekeeperIncomingBtn_Click(object sender, RoutedEventArgs e) => NavigateToPage("Purchases");
        private void StorekeeperOutgoingBtn_Click(object sender, RoutedEventArgs e) => ShowMessage("Функция отгрузок в разработке");
        private void StorekeeperReportBtn_Click(object sender, RoutedEventArgs e) => ShowMessage("Функция складских отчетов в разработке");

        private void OperatorPurchaseBtn_Click(object sender, RoutedEventArgs e) => NavigateToPage("Purchases");
        private void OperatorMaintenanceBtn_Click(object sender, RoutedEventArgs e) => NavigateToPage("Maintenance");
        private void OperatorDocumentsBtn_Click(object sender, RoutedEventArgs e) => ShowMessage("Функция управления документами в разработке");

        private void ShowMessage(string message)
        {
            MessageBox.Show(message, "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void NavigateToPage(string pageName)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.NavigateToPage(pageName);
            }
        }
    }


    // Модели данных
    public class RecentAction { public string Icon { get; set; } public string Action { get; set; } public string Time { get; set; } }
    public class KPI { public string Name { get; set; } public string Value { get; set; } }
    public class ActiveTask { public string Title { get; set; } public string Description { get; set; } public string Status { get; set; } public string StatusColor { get; set; } }
    public class MasterTask { public int TaskId { get; set; } public string TaskName { get; set; } public string Equipment { get; set; } public string Deadline { get; set; } }
    public class LowStockItem { public string ItemName { get; set; } public string Quantity { get; set; } }
    public class RecentDocument { public string DocNumber { get; set; } public string Date { get; set; } public string Status { get; set; } public string StatusColor { get; set; } }
}