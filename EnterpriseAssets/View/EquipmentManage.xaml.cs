using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using EnterpriseAssets.Model.DataBase;

namespace EnterpriseAssets.View
{
    public partial class EquipmentManage : Window
    {
        private DB_AssetManage db = new DB_AssetManage();
        private int? _equipmentId;
        private EQUIPMENT _currentEquipment;

        public EquipmentManage(int? equipmentId = null)
        {
            InitializeComponent();
            _equipmentId = equipmentId;
            Loaded += EquipmentManage_Loaded;
        }

        private void EquipmentManage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadComboBoxes();

            if (_equipmentId.HasValue)
            {
                Title = "Редактирование оборудования";
                WindowTitle.Text = "Редактирование оборудования";
                BtnDelete.Visibility = Visibility.Visible;
                LoadEquipmentData();
            }
            else
            {
                Title = "Добавление оборудования";
                WindowTitle.Text = "Добавление оборудования";
                BtnDelete.Visibility = Visibility.Collapsed;
            }
        }

        private void LoadComboBoxes()
        {
            // 1. Активы (фильтруемые)
            LoadAssetsComboBox();

            // 2. Цеха
            CmbWorkshop.ItemsSource = db.WORKSHOPS.ToList();
            CmbWorkshop.DisplayMemberPath = "name";
            CmbWorkshop.SelectedValuePath = "id";

            // 3. Мастера
            var masters = db.MASTERS.Include("USERS").ToList();
            CmbMaster.ItemsSource = masters;
            CmbMaster.DisplayMemberPath = "USERS.full_name";
            CmbMaster.SelectedValuePath = "id";

            // 4. Статусы
            CmbStatus.ItemsSource = db.STATUSASSETS.ToList();
            CmbStatus.DisplayMemberPath = "Status";
            CmbStatus.SelectedValuePath = "ID_status";
        }

        private void LoadAssetsComboBox()
        {
            // Все активы типа "Оборудование"
            var allAssets = db.PRODUCTION_ASSETS
                              .Where(a => a.asset_type == 6)
                              .ToList();

            // ID активов, которые уже привязаны к какому-либо оборудованию (строковые значения)
            var usedAssetIds = db.EQUIPMENT
                                 .Where(e => e.asset_id != null)
                                 .Select(e => e.asset_id)
                                 .Distinct()
                                 .ToList();

            // Доступные активы = все активы нужного типа, исключая занятые
            var availableAssets = allAssets
                .Where(a => !usedAssetIds.Contains(a.id.ToString()))
                .ToList();

            CmbAsset.ItemsSource = availableAssets;
            CmbAsset.DisplayMemberPath = "name";
            CmbAsset.SelectedValuePath = "id";
        }


        /// <summary>
        /// Обновление списка активов для режима редактирования (включает текущий актив)
        /// </summary>
        private void RefreshAssetsComboBoxForEdit()
        {
            if (_currentEquipment == null) return;

            // Все активы типа "Оборудование"
            var allAssets = db.PRODUCTION_ASSETS
                              .Where(a => a.asset_type == 6)
                              .ToList();

            // ID активов, занятых другим оборудованием (исключая текущее)
            var usedAssetIds = db.EQUIPMENT
                                 .Where(e => e.asset_id != null && e.ID != _equipmentId.Value)
                                 .Select(e => e.asset_id)
                                 .Distinct()
                                 .ToList();

            // Доступные активы = все активы нужного типа, кроме занятых, плюс текущий актив (чтобы он был в списке)
            var availableAssets = allAssets
                .Where(a => !usedAssetIds.Contains(a.id.ToString()) || a.id.ToString() == _currentEquipment.asset_id)
                .ToList();

            CmbAsset.ItemsSource = availableAssets;
            CmbAsset.DisplayMemberPath = "name";
            CmbAsset.SelectedValuePath = "id";
        }
        private void LoadEquipmentData()
        {
            _currentEquipment = db.EQUIPMENT
                .Include("WORKSHOPS")
                .Include("MASTERS")
                .Include("STATUSASSETS")
                .FirstOrDefault(e => e.ID == _equipmentId.Value);

            if (_currentEquipment == null)
            {
                MessageBox.Show("Оборудование не найдено.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            // Обновляем список активов с учётом текущего
            RefreshAssetsComboBoxForEdit();

            // Заполнение полей
            CmbAsset.SelectedValue = ParseInt(_currentEquipment.asset_id); // если asset_id число, иначе используйте строку
            TxtEquipmentType.Text = _currentEquipment.equipment_type;
            TxtManufacturer.Text = _currentEquipment.manufacturer;
            TxtNotes.Text = _currentEquipment.notes;
            CmbWorkshop.SelectedValue = _currentEquipment.Workshop_id;
            CmbMaster.SelectedValue = _currentEquipment.assigned_to;
            DpInstallationDate.SelectedDate = _currentEquipment.installation_date;
            TxtWarranty.Text = _currentEquipment.warranty_period_months?.ToString();
            DpLastMaintenance.SelectedDate = _currentEquipment.last_maintenance_date;
            DpNextMaintenance.SelectedDate = _currentEquipment.next_maintenance_date;
            TxtCurrentHours.Text = _currentEquipment.current_work_hours?.ToString();
            TxtMaxHours.Text = _currentEquipment.max_work_hours_before_maintenance?.ToString();
            CmbStatus.SelectedValue = _currentEquipment.status;
        }

        private void CmbAsset_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Здесь можно автоматически заполнять поля, если актив содержит нужные данные
            // Например:
            // if (CmbAsset.SelectedItem is PRODUCTION_ASSETS selectedAsset)
            // {
            //     TxtEquipmentType.Text = selectedAsset.type;        // если есть такое поле
            //     TxtManufacturer.Text = selectedAsset.manufacturer; // если есть
            // }
        }

        private bool ValidateFields()
        {
            if (CmbAsset.SelectedValue == null)
            {
                MessageBox.Show("Выберите производственный актив.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (string.IsNullOrWhiteSpace(TxtEquipmentType.Text))
            {
                MessageBox.Show("Введите тип оборудования.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (string.IsNullOrWhiteSpace(TxtManufacturer.Text))
            {
                MessageBox.Show("Введите производителя.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (CmbWorkshop.SelectedValue == null)
            {
                MessageBox.Show("Выберите цех.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (CmbMaster.SelectedValue == null)
            {
                MessageBox.Show("Выберите ответственного мастера.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (CmbStatus.SelectedValue == null)
            {
                MessageBox.Show("Выберите статус оборудования.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateFields())
                return;

            try
            {
                if (_equipmentId.HasValue)
                {
                    // Обновление
                    _currentEquipment.asset_id = CmbAsset.SelectedValue.ToString();
                    _currentEquipment.equipment_type = TxtEquipmentType.Text.Trim();
                    _currentEquipment.manufacturer = TxtManufacturer.Text.Trim();
                    _currentEquipment.notes = TxtNotes.Text.Trim();
                    _currentEquipment.Workshop_id = (int?)CmbWorkshop.SelectedValue;
                    _currentEquipment.assigned_to = (int?)CmbMaster.SelectedValue;
                    _currentEquipment.installation_date = DpInstallationDate.SelectedDate;
                    _currentEquipment.warranty_period_months = ParseInt(TxtWarranty.Text);
                    _currentEquipment.last_maintenance_date = DpLastMaintenance.SelectedDate;
                    _currentEquipment.next_maintenance_date = DpNextMaintenance.SelectedDate;
                    _currentEquipment.current_work_hours = ParseInt(TxtCurrentHours.Text);
                    _currentEquipment.max_work_hours_before_maintenance = ParseInt(TxtMaxHours.Text);
                    _currentEquipment.status = (int?)CmbStatus.SelectedValue;

                    db.SaveChanges();
                }
                else
                {
                    // Добавление
                    var newEquipment = new EQUIPMENT
                    {
                        asset_id = CmbAsset.SelectedValue.ToString(),
                        equipment_type = TxtEquipmentType.Text.Trim(),
                        manufacturer = TxtManufacturer.Text.Trim(),
                        notes = TxtNotes.Text.Trim(),
                        Workshop_id = (int?)CmbWorkshop.SelectedValue,
                        assigned_to = (int?)CmbMaster.SelectedValue,
                        installation_date = DpInstallationDate.SelectedDate,
                        warranty_period_months = ParseInt(TxtWarranty.Text),
                        last_maintenance_date = DpLastMaintenance.SelectedDate,
                        next_maintenance_date = DpNextMaintenance.SelectedDate,
                        current_work_hours = ParseInt(TxtCurrentHours.Text),
                        max_work_hours_before_maintenance = ParseInt(TxtMaxHours.Text),
                        status = (int?)CmbStatus.SelectedValue
                    };

                    db.EQUIPMENT.Add(newEquipment);
                    db.SaveChanges();
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!_equipmentId.HasValue) return;

            var result = MessageBox.Show("Вы уверены, что хотите удалить это оборудование?", "Подтверждение",
                                         MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var equipment = db.EQUIPMENT.Find(_equipmentId.Value);
                    if (equipment != null)
                    {
                        db.EQUIPMENT.Remove(equipment);
                        db.SaveChanges();
                        DialogResult = true;
                        Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TxtNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, 0))
                e.Handled = true;
        }

        private int? ParseInt(string text)
        {
            if (int.TryParse(text, out int result))
                return result;
            return null;
        }
    }
}