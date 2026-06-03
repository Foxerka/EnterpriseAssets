using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using EnterpriseAssets.Model.DataBase;
using Microsoft.Win32;

namespace EnterpriseAssets.View
{
    public partial class ReportsPage : Page
    {
        private DB_AssetManage db = new DB_AssetManage();
        private int currentUserId;
        private bool isAdmin;
        private List<REPORTS> allReports;
        private USERS currentUser;

        public ReportsPage(int userId)
        {
            InitializeComponent();
            currentUserId = userId;
            LoadCurrentUser();
            LoadUsers();
            LoadReports();
        }

        private void LoadCurrentUser()
        {
            currentUser = db.USERS.Include("ROLES").FirstOrDefault(u => u.id == currentUserId);
            isAdmin = currentUser?.ROLES?.name == "Администратор" ||
                      currentUser?.ROLES?.name == "Руководитель";
        }

        private void LoadUsers()
        {
            // Загружаем всех мастеров с их пользователями для фильтра
            var masters = db.MASTERS
                .Include("USERS")
                .Where(m => m.user_id != null)
                .OrderBy(m => m.USERS.full_name)
                .ToList();

            if (isAdmin)
            {
                // Админ может видеть всех
                var allUsers = new List<UserFilterItem>
        {
            new UserFilterItem { MasterId = 0, DisplayName = "Все сотрудники" }
        };

                foreach (var master in masters)
                {
                    allUsers.Add(new UserFilterItem
                    {
                        MasterId = master.id,
                        DisplayName = master.USERS?.full_name ?? $"Мастер #{master.id}"
                    });
                }

                CmbUserFilter.ItemsSource = allUsers;
                CmbUserFilter.SelectedIndex = 0;
            }
            else
            {
                // Обычный пользователь видит только себя
                var currentMaster = masters.FirstOrDefault(m => m.user_id == currentUserId);
                if (currentMaster != null)
                {
                    CmbUserFilter.ItemsSource = new List<UserFilterItem>
            {
                new UserFilterItem
                {
                    MasterId = currentMaster.id,
                    DisplayName = currentMaster.USERS?.full_name ?? "Вы"
                }
            };
                    CmbUserFilter.SelectedIndex = 0;
                    CmbUserFilter.IsEnabled = false;
                }
            }
        }

        private void LoadReports()
        {
            try
            {
                var query = db.REPORTS.Include("MASTERS.USERS").AsQueryable();

                // Применяем фильтр по пользователю
                if (CmbUserFilter.SelectedItem is UserFilterItem selectedUser && selectedUser.MasterId > 0)
                {
                    query = query.Where(r => r.generated_by == selectedUser.MasterId);
                }
                else if (!isAdmin)
                {
                    // Если не админ и не выбран пользователь, показываем только его отчеты
                    var currentMaster = db.MASTERS.FirstOrDefault(m => m.user_id == currentUserId);
                    if (currentMaster != null)
                    {
                        query = query.Where(r => r.generated_by == currentMaster.id);
                    }
                }

                // Применяем фильтр по типу - ИСПРАВЛЕНО!
                if (CmbReportType.SelectedItem is ComboBoxItem selectedType)
                {
                    string selectedTypeName = selectedType.Content?.ToString();
                    if (selectedTypeName != "Все типы" && !string.IsNullOrEmpty(selectedTypeName))
                    {
                        query = query.Where(r => r.report_type == selectedTypeName);
                    }
                }

                // Применяем фильтр по периоду
                if (DpPeriodStart.SelectedDate.HasValue)
                {
                    DateTime startDate = DpPeriodStart.SelectedDate.Value.Date;
                    query = query.Where(r => r.period_start.HasValue && r.period_start.Value >= startDate);
                }

                if (DpPeriodEnd.SelectedDate.HasValue)
                {
                    DateTime endDate = DpPeriodEnd.SelectedDate.Value.Date.AddDays(1).AddTicks(-1); // Конец дня
                    query = query.Where(r => r.period_end.HasValue && r.period_end.Value <= endDate);
                }

                allReports = query.OrderByDescending(r => r.created_at).ToList();
                ReportsItemsControl.ItemsSource = allReports;

                // Обновляем счетчики
                TxtTotalReports.Text = db.REPORTS.Count().ToString();
                TxtFilteredReports.Text = allReports.Count.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки отчетов: {ex.Message}",
                              "Ошибка",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        private void CmbUserFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                LoadReports();
            }
        }

        private void CmbReportType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                LoadReports();
            }
        }

        private void DpPeriodStart_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                LoadReports();
            }
        }

        private void DpPeriodEnd_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                LoadReports();
            }
        }

        private void BtnCreateReport_Click(object sender, RoutedEventArgs e)
        {
            var createReportWindow = new CreateReportWindow(currentUserId);
            createReportWindow.ShowDialog();
            LoadReports();
        }

        private void BtnViewReport_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is REPORTS selectedReport)
            {
                ViewReport(selectedReport);
            }
        }

        private void BtnDownloadReport_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is REPORTS selectedReport)
            {
                DownloadReport(selectedReport);
            }
        }

        private void BtnDeleteReport_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is REPORTS selectedReport)
            {
                DeleteReport(selectedReport);
            }
        }

        private void ViewReport(REPORTS selectedReport)
        {
            try
            {
                if (!string.IsNullOrEmpty(selectedReport.file_path) && File.Exists(selectedReport.file_path))
                {
                    System.Diagnostics.Process.Start(selectedReport.file_path);
                }
                else if (!string.IsNullOrEmpty(selectedReport.report_data))
                {
                    var viewWindow = new Window
                    {
                        Title = $"Отчет: {selectedReport.report_type}",
                        Width = 800,
                        Height = 600,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen
                    };

                    var textBox = new TextBox
                    {
                        Text = selectedReport.report_data,
                        IsReadOnly = true,
                        TextWrapping = TextWrapping.Wrap,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                        FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                        FontSize = 12,
                        Padding = new Thickness(10)
                    };

                    viewWindow.Content = textBox;
                    viewWindow.ShowDialog();
                }
                else
                {
                    MessageBox.Show("Данные отчета отсутствуют",
                                  "Информация",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия отчета: {ex.Message}",
                              "Ошибка",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        private void DownloadReport(REPORTS selectedReport)
        {
            try
            {
                if (!string.IsNullOrEmpty(selectedReport.file_path) && File.Exists(selectedReport.file_path))
                {
                    var saveDialog = new SaveFileDialog
                    {
                        Title = "Сохранить отчет",
                        FileName = Path.GetFileName(selectedReport.file_path),
                        Filter = "Все файлы|*.*"
                    };

                    if (saveDialog.ShowDialog() == true)
                    {
                        File.Copy(selectedReport.file_path, saveDialog.FileName, true);
                        MessageBox.Show("Отчет успешно сохранен",
                                      "Успех",
                                      MessageBoxButton.OK,
                                      MessageBoxImage.Information);
                    }
                }
                else if (!string.IsNullOrEmpty(selectedReport.report_data))
                {
                    var saveDialog = new SaveFileDialog
                    {
                        Title = "Сохранить отчет",
                        FileName = $"Отчет_{selectedReport.id}.txt",
                        Filter = "Текстовые файлы|*.txt|Все файлы|*.*"
                    };

                    if (saveDialog.ShowDialog() == true)
                    {
                        File.WriteAllText(saveDialog.FileName, selectedReport.report_data);
                        MessageBox.Show("Отчет успешно сохранен",
                                      "Успех",
                                      MessageBoxButton.OK,
                                      MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show("Данные отчета отсутствуют",
                                  "Информация",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения отчета: {ex.Message}",
                              "Ошибка",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        private void DeleteReport(REPORTS selectedReport)
        {
            var result = MessageBox.Show($"Вы уверены, что хотите удалить отчет \"{selectedReport.report_type}\"?",
                                       "Подтверждение удаления",
                                       MessageBoxButton.YesNo,
                                       MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    if (!string.IsNullOrEmpty(selectedReport.file_path) && File.Exists(selectedReport.file_path))
                    {
                        File.Delete(selectedReport.file_path);
                    }

                    db.REPORTS.Remove(selectedReport);
                    db.SaveChanges();

                    MessageBox.Show("Отчет успешно удален",
                                  "Успех",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);

                    LoadReports();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления отчета: {ex.Message}",
                                  "Ошибка",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Error);
                }
            }
        }
    }

    // Вспомогательный класс для фильтра по пользователям
    public class UserFilterItem
    {
        public int MasterId { get; set; }
        public string DisplayName { get; set; }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}