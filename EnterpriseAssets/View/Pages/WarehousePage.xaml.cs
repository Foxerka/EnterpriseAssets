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
        private List<LocationViewModel> _allLocations; // Переименовано

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
                var locations = db.WORKSHOPS.ToList(); // Переименовано
                _allLocations = locations.Select(w => new LocationViewModel(w, db)).ToList(); // Переименовано
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
            if (_allLocations == null) return;

            var filtered = _allLocations.AsEnumerable();

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
            if (_allLocations == null) return;

            TotalCount.Text = _allLocations.Count.ToString();
            WithManagerCount.Text = _allLocations.Count(w => w.ManagerId.HasValue).ToString();
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
                var location = db.WORKSHOPS.FirstOrDefault(w => w.id == id); // Переименовано
                if (location != null)
                {
                    var dialog = new WorkshopManage(location);
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
                var location = db.WORKSHOPS.FirstOrDefault(w => w.id == id); // Переименовано
                if (location != null)
                {
                    var dialog = new WorkshopManage(location);
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
                var location = db.WORKSHOPS.FirstOrDefault(w => w.id == id); // Переименовано
                if (location != null)
                {
                    var result = MessageBox.Show(
                        $"Вы уверены, что хотите удалить место хранения '{location.name}'?", // Изменен текст
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
                                    "Невозможно удалить место хранения, так как к нему привязано оборудование или активы.", // Изменен текст
                                    "Ошибка удаления",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                                return;
                            }

                            db.WORKSHOPS.Remove(location);
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

    // Переименовано с WorkshopViewModel на LocationViewModel
    public class LocationViewModel
    {
        private WORKSHOPS _location; // Переименовано
        private DB_AssetManage _db;

        public LocationViewModel(WORKSHOPS location, DB_AssetManage db) // Переименовано
        {
            _location = location;
            _db = db;
        }

        public int Id => _location.id;
        public string Name => _location.name ?? "Без названия";
        public string Location => _location.location ?? "Не указано";
        public int? ManagerId => _location.manager_id;

        public string ManagerName
        {
            get
            {
                if (_location.manager_id.HasValue)
                {
                    var responsiblePerson = _db.USERS.FirstOrDefault(u => u.id == _location.manager_id.Value); // Переименовано
                    return responsiblePerson?.full_name ?? "Не назначен";
                }
                return "Не назначен";
            }
        }

        public FontWeight ManagerWeight => _location.manager_id.HasValue ? FontWeights.Normal : FontWeights.Light;
    }
}