using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using EnterpriseAssets.Model.DataBase;

namespace EnterpriseAssets.View.Pages
{
    public partial class WarehousePage : Page
    {
        private DB_AssetManage db = new DB_AssetManage();
        private List<WorkshopViewModel> _allWorkshops;

        public WarehousePage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                var workshops = db.WORKSHOPS.ToList();
                _allWorkshops = workshops.Select(w => new WorkshopViewModel(w, db)).ToList();
                ApplyFilter();
                UpdateStats();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilter()
        {
            if (_allWorkshops == null) return;

            var filtered = _allWorkshops.AsEnumerable();

            if (TxtSearch != null && !string.IsNullOrWhiteSpace(TxtSearch.Text))
            {
                string search = TxtSearch.Text.ToLower();
                filtered = filtered.Where(w =>
                    (w.Name != null && w.Name.ToLower().Contains(search)) ||
                    (w.Location != null && w.Location.ToLower().Contains(search)));
            }

            WorkshopsList.ItemsSource = filtered.OrderBy(w => w.Name).ToList();
        }

        private void UpdateStats()
        {
            if (_allWorkshops == null) return;

            TotalCount.Text = _allWorkshops.Count.ToString();
            WithManagerCount.Text = _allWorkshops.Count(w => w.ManagerId.HasValue).ToString();
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void BtnAddWorkshop_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new WorkshopManage();
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void WorkshopCard_Click(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border?.Tag != null)
            {
                int id = (int)border.Tag;
                var workshop = db.WORKSHOPS.FirstOrDefault(w => w.id == id);
                if (workshop != null)
                {
                    var dialog = new WorkshopManage(workshop);
                    dialog.Owner = Window.GetWindow(this);
                    if (dialog.ShowDialog() == true)
                    {
                        LoadData();
                    }
                }
            }
        }

        private void EditWorkshop_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag != null)
            {
                int id = (int)button.Tag;
                var workshop = db.WORKSHOPS.FirstOrDefault(w => w.id == id);
                if (workshop != null)
                {
                    var dialog = new WorkshopManage(workshop);
                    dialog.Owner = Window.GetWindow(this);
                    if (dialog.ShowDialog() == true)
                    {
                        LoadData();
                    }
                }
            }
        }

        private void DeleteWorkshop_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag != null)
            {
                int id = (int)button.Tag;
                var workshop = db.WORKSHOPS.FirstOrDefault(w => w.id == id);
                if (workshop != null)
                {
                    var result = MessageBox.Show(
                        $"Вы уверены, что хотите удалить цех '{workshop.name}'?",
                        "Подтверждение удаления",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            // Проверка зависимостей
                            bool hasEquipment = db.EQUIPMENT.Any(e => e.Workshop_id == id);
                            bool hasAssets = db.PRODUCTION_ASSETS.Any(a => a.workshop_id == id);

                            if (hasEquipment || hasAssets)
                            {
                                MessageBox.Show(
                                    "Невозможно удалить цех, так как к нему привязано оборудование или активы.",
                                    "Ошибка удаления",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                                return;
                            }

                            db.WORKSHOPS.Remove(workshop);
                            db.SaveChanges();
                            LoadData();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                                          MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
        }
    }
    public class WorkshopViewModel
    {
        private WORKSHOPS _workshop;
        private DB_AssetManage _db;

        public WorkshopViewModel(WORKSHOPS workshop, DB_AssetManage db)
        {
            _workshop = workshop;
            _db = db;
        }

        public int Id => _workshop.id;
        public string Name => _workshop.name ?? "Без названия";
        public string Location => _workshop.location ?? "Не указано";
        public int? ManagerId => _workshop.manager_id;

        public string ManagerName
        {
            get
            {
                if (_workshop.manager_id.HasValue)
                {
                    var manager = _db.USERS.FirstOrDefault(u => u.id == _workshop.manager_id.Value);
                    return manager?.full_name ?? "Не назначен";
                }
                return "Не назначен";
            }
        }

        public FontWeight ManagerWeight => _workshop.manager_id.HasValue ? FontWeights.Normal : FontWeights.Light;
    }
}