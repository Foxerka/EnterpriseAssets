using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Data.Entity;
using EnterpriseAssets.Model.DataBase;

namespace EnterpriseAssets.View
{
    public partial class EquipmentManage : Window
    {
        private DB_AssetManage db = new DB_AssetManage();
        private EQUIPMENT _currentEquipment;
        private bool _isNewEquipment;

        public EquipmentManage()
        {
            InitializeComponent();
            _currentEquipment = new EQUIPMENT();
            _isNewEquipment = true;
            WindowTitle.Text = "Добавление оборудования";
            BtnDelete.Visibility = Visibility.Collapsed;
            InitializeData();
        }

        public EquipmentManage(EQUIPMENT equipment)
        {
            InitializeComponent();
            _currentEquipment = equipment;
            _isNewEquipment = false;
            WindowTitle.Text = "Редактирование оборудования";
            BtnDelete.Visibility = Visibility.Visible;
            InitializeData();
            LoadEquipmentData();
        }

        private void InitializeData()
        {
            LoadEquipmentAssets();
            LoadWorkshops();
            LoadMasters();  // ✅ Исправленный метод
            LoadStatuses();
        }

        private void LoadEquipmentAssets()
        {
            try
            {
                // 🔹 Загружаем ТОЛЬКО активы типа "Оборудование"
                var equipmentAssets = db.PRODUCTION_ASSETS
                    .Include("ASSETTYPE")
                    .Where(a => a.ASSETTYPE != null &&
                               a.ASSETTYPE.AssetType1 != null &&  // ✅ Проверка на null
                               a.ASSETTYPE.AssetType1.Trim().ToLower() == "оборудование")
                    .OrderBy(a => a.name)
                    .ToList();

                CmbAsset.ItemsSource = equipmentAssets;
                CmbAsset.DisplayMemberPath = "name";
                CmbAsset.SelectedValuePath = "name";

                if (!_isNewEquipment && !string.IsNullOrEmpty(_currentEquipment.asset_id))
                {
                    CmbAsset.SelectedValue = _currentEquipment.asset_id;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки активов: {ex.Message}", "Ошибка");
            }
        }

        private void LoadWorkshops()
        {
            var workshops = db.WORKSHOPS.OrderBy(w => w.name).ToList();
            CmbWorkshop.ItemsSource = workshops;
            CmbWorkshop.DisplayMemberPath = "name";
            CmbWorkshop.SelectedValuePath = "id";
        }

        // ✅ ИСПРАВЛЕННЫЙ МЕТОД: Загрузка мастеров из MASTERS с навигацией
        private void LoadMasters()
        {
            try
            {
                // 🔹 Находим роль "Мастер"
                var masterRole = db.ROLES
                    .FirstOrDefault(r => r.name.Trim().ToLower() == "мастер");

                if (masterRole == null)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Роль 'Мастер' не найдена!");
                    return;
                }

                // 🔹 Загружаем мастеров через таблицу MASTERS с навигационным свойством USERS
                var masters = db.MASTERS
                    .Include(m => m.USERS)  // ✅ Загружаем связанного пользователя
                    .Where(m => m.user_id != null &&
                               m.USERS != null &&
                               m.USERS.role_id == masterRole.id)
                    .Select(m => new
                    {
                        MasterId = m.id,
                        FullName = m.USERS.full_name != null ? m.USERS.full_name : m.USERS.username
                    })
                    .OrderBy(m => m.FullName)
                    .ToList();

                // 🔹 Формируем список для ComboBox
                var items = new List<dynamic>();
                items.Add(new { MasterId = (int?)null, FullName = "— Не назначен —" });
                items.AddRange(masters);

                CmbMaster.ItemsSource = items;
                CmbMaster.DisplayMemberPath = "FullName";
                CmbMaster.SelectedValuePath = "MasterId";

                System.Diagnostics.Debug.WriteLine($"✅ Загружено мастеров: {masters.Count}");

                // 🔹 Если редактируем — устанавливаем выбранного мастера
                if (!_isNewEquipment && _currentEquipment.assigned_to.HasValue)
                {
                    CmbMaster.SelectedValue = _currentEquipment.assigned_to.Value;
                }
                else
                {
                    CmbMaster.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ LoadMasters error: {ex.Message}");
                MessageBox.Show($"Ошибка загрузки мастеров: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadStatuses()
        {
            var statuses = db.STATUSASSETS.OrderBy(s => s.Status).ToList();
            CmbStatus.ItemsSource = statuses;
            CmbStatus.DisplayMemberPath = "Status";
            CmbStatus.SelectedValuePath = "ID_status";
        }

        private void LoadEquipmentData()
        {
            if (_currentEquipment == null) return;

            TxtEquipmentType.Text = _currentEquipment.equipment_type;
            TxtManufacturer.Text = _currentEquipment.manufacturer;
            TxtNotes.Text = _currentEquipment.notes;
            TxtWarranty.Text = _currentEquipment.warranty_period_months?.ToString();
            TxtCurrentHours.Text = _currentEquipment.current_work_hours?.ToString();
            TxtMaxHours.Text = _currentEquipment.max_work_hours_before_maintenance?.ToString();

            if (_currentEquipment.installation_date.HasValue)
                DpInstallationDate.SelectedDate = _currentEquipment.installation_date.Value;
            if (_currentEquipment.last_maintenance_date.HasValue)
                DpLastMaintenance.SelectedDate = _currentEquipment.last_maintenance_date.Value;
            if (_currentEquipment.next_maintenance_date.HasValue)
                DpNextMaintenance.SelectedDate = _currentEquipment.next_maintenance_date.Value;

            CmbWorkshop.SelectedValue = _currentEquipment.Workshop_id;
            CmbStatus.SelectedValue = _currentEquipment.status;
        }

        private void CmbAsset_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbAsset.SelectedItem is PRODUCTION_ASSETS asset)
            {
                if (!_currentEquipment.Workshop_id.HasValue && asset.workshop_id.HasValue)
                {
                    CmbWorkshop.SelectedValue = asset.workshop_id.Value;
                }
            }
        }

        private void TxtNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[0-9]+$");
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TxtEquipmentType.Text))
                {
                    MessageBox.Show("Введите тип оборудования", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _currentEquipment.asset_id = CmbAsset.SelectedValue?.ToString();
                if (string.IsNullOrEmpty(_currentEquipment.asset_id))
                {
                    MessageBox.Show("Выберите производственный актив", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _currentEquipment.equipment_type = TxtEquipmentType.Text.Trim();
                _currentEquipment.manufacturer = TxtManufacturer.Text?.Trim();
                _currentEquipment.notes = string.IsNullOrWhiteSpace(TxtNotes.Text) ? null : TxtNotes.Text.Trim();

                _currentEquipment.installation_date = DpInstallationDate.SelectedDate;
                _currentEquipment.warranty_period_months = string.IsNullOrWhiteSpace(TxtWarranty.Text)
                    ? (int?)null : int.Parse(TxtWarranty.Text);

                _currentEquipment.last_maintenance_date = DpLastMaintenance.SelectedDate;
                _currentEquipment.next_maintenance_date = DpNextMaintenance.SelectedDate;

                _currentEquipment.current_work_hours = string.IsNullOrWhiteSpace(TxtCurrentHours.Text)
                    ? (int?)null : int.Parse(TxtCurrentHours.Text);
                _currentEquipment.max_work_hours_before_maintenance = string.IsNullOrWhiteSpace(TxtMaxHours.Text)
                    ? (int?)null : int.Parse(TxtMaxHours.Text);

                _currentEquipment.Workshop_id = CmbWorkshop.SelectedValue as int?;
                _currentEquipment.assigned_to = CmbMaster.SelectedValue as int?;
                _currentEquipment.status = CmbStatus.SelectedValue as int?;

                if (_isNewEquipment)
                {
                    db.EQUIPMENT.Add(_currentEquipment);
                }

                db.SaveChanges();

                MessageBox.Show("Оборудование успешно сохранено", "Успех",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.GetBaseException().Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_isNewEquipment || _currentEquipment.ID <= 0) return;

            var result = MessageBox.Show($"Удалить оборудование \"{_currentEquipment.asset_id}\"?",
                                       "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                var item = db.EQUIPMENT.Find(_currentEquipment.ID);
                if (item != null)
                {
                    db.EQUIPMENT.Remove(item);
                    db.SaveChanges();
                    MessageBox.Show("Оборудование удалено", "Успех");
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            db?.Dispose();
            base.OnClosed(e);
        }
    }
}