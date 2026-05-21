using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EnterpriseAssets.Model.DataBase;

namespace EnterpriseAssets.View.Pages
{
    public partial class WorkActsPage : Page
    {
        private DB_AssetManage db = new DB_AssetManage();
        private List<WORK_ACTS> _allActs;
        private int _currentUserId;

        public WorkActsPage()
        {
            InitializeComponent();
        }

        public WorkActsPage(int currentUserId) : this()
        {
            _currentUserId = currentUserId;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadWorkActs();
        }

        private void LoadWorkActs()
        {
            try
            {
                _allActs = db.WORK_ACTS
                    .Include("MASTERS")
                    .Include("MASTERS.USERS")
                    .Include("ActStatus")
                    .Include("EQUIPMENT")
                    .Include("COMPLETION_ACTS")
                    .OrderByDescending(a => a.work_date)
                    .ToList();

                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilter()
        {
            if (_allActs == null) return;

            var filtered = _allActs.AsEnumerable();

            // Поиск
            string search = SearchBox.Text?.Trim().ToLower();
            if (!string.IsNullOrEmpty(search))
            {
                filtered = filtered.Where(a => (a.act_number?.ToLower().Contains(search) ?? false));
            }

            var result = filtered.Select(a => new
            {
                a.id,
                a.act_number,
                act_date = a.work_date,
                description = a.work_type,
                equipment_name = a.EQUIPMENT?.asset_id ?? "Не указано",
                master_name = a.MASTERS?.USERS?.full_name ?? "Не назначен",
                act_status = a.ActStatus?.Status ?? "Черновик",

                // COMPLETION_ACTS - это коллекция, берем первый элемент
                has_completion_act = a.COMPLETION_ACTS != null && a.COMPLETION_ACTS.Any(),
                completion_date = a.COMPLETION_ACTS != null && a.COMPLETION_ACTS.Any()
                    ? a.COMPLETION_ACTS.FirstOrDefault().completion_date
                    : (DateTime?)null,
                quality_check = a.COMPLETION_ACTS != null && a.COMPLETION_ACTS.Any()
                    ? a.COMPLETION_ACTS.FirstOrDefault().quality_check
                    : false,

                Icon = GetStatusIcon(a.ActStatus?.Status),
                StatusColor = GetStatusColor(a.ActStatus?.Status),
                StatusTextColor = GetStatusTextColor(a.ActStatus?.Status),
                HasCompletionAct = a.COMPLETION_ACTS != null && a.COMPLETION_ACTS.Any()
            }).ToList();

            WorkActsList.ItemsSource = result;
        }

        private string GetStatusIcon(string status)
        {
            return status switch
            {
                "Черновик" => "📝",
                "В работе" => "🔧",
                "На проверке" => "👀",
                "Завершен" => "✅",
                "Отменен" => "❌",
                _ => "📄"
            };
        }

        private string GetStatusColor(string status)
        {
            return status switch
            {
                "Черновик" => "#95A5A6",
                "В работе" => "#F39C12",
                "На проверке" => "#3498DB",
                "Завершен" => "#27AE60",
                "Отменен" => "#E74C3C",
                _ => "#95A5A6"
            };
        }

        private string GetStatusTextColor(string status)
        {
            return status switch
            {
                "Завершен" => "#27AE60",
                "Отменен" => "#E74C3C",
                "В работе" => "#F39C12",
                _ => "#7F8C8D"
            };
        }

        private void NewAct_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new WorkActDialog(_currentUserId);
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true)
            {
                LoadWorkActs();
            }
        }

        private void ActCard_Click(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border?.Tag != null && int.TryParse(border.Tag.ToString(), out int actId))
            {
                var act = _allActs.FirstOrDefault(a => a.id == actId);
                if (act != null)
                {
                    var dialog = new WorkActDialog(act, _currentUserId);
                    dialog.Owner = Window.GetWindow(this);
                    if (dialog.ShowDialog() == true)
                    {
                        LoadWorkActs();
                    }
                }
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadWorkActs();
        }
    }
}