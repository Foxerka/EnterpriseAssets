using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Text.RegularExpressions;
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
            SetupInputMasks();
        }

        private void SetupInputMasks()
        {
            // Маска для типа оборудования (буквы, цифры, пробелы, дефисы)
            TxtEquipmentType.PreviewTextInput += (s, e) =>
            {
                e.Handled = !Regex.IsMatch(e.Text, @"^[a-zA-Zа-яА-Я0-9\s\-\.\(\)]+$");
            };
            TxtEquipmentType.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Space && string.IsNullOrEmpty(TxtEquipmentType.Text))
                    e.Handled = true;
            };

            // Маска для производителя
            TxtManufacturer.PreviewTextInput += (s, e) =>
            {
                e.Handled = !Regex.IsMatch(e.Text, @"^[a-zA-Zа-яА-Я0-9\s\-\.\(\)]+$");
            };

            // Маска для примечаний - без ограничений
            // Маски для числовых полей уже есть в TxtNumber_PreviewTextInput
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
            LoadAssetsComboBox();
            LoadWorkshops();
            LoadMasters();
            LoadStatuses();
        }

        private void LoadAssetsComboBox()
        {
            try
            {
                var allAssets = db.PRODUCTION_ASSETS
                                  .Where(a => a.asset_type == 6)
                                  .ToList();

                var usedAssetIds = db.EQUIPMENT
                                     .Where(e => e.asset_id != null)
                                     .Select(e => e.asset_id)
                                     .Distinct()
                                     .ToList();

                var availableAssets = allAssets
                    .Where(a => !usedAssetIds.Contains(a.id.ToString()))
                    .ToList();

                CmbAsset.ItemsSource = availableAssets;
                CmbAsset.DisplayMemberPath = "name";
                CmbAsset.SelectedValuePath = "id";

                System.Diagnostics.Debug.WriteLine($"✅ Загружено активов: {availableAssets.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка загрузки активов: {ex.Message}");
            }
        }

        private void LoadWorkshops()
        {
            try
            {
                CmbWorkshop.ItemsSource = db.WORKSHOPS.ToList();
                CmbWorkshop.DisplayMemberPath = "name";
                CmbWorkshop.SelectedValuePath = "id";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка загрузки цехов: {ex.Message}");
            }
        }

        private void LoadMasters()
        {
            try
            {
                var masters = db.MASTERS.Include("USERS").ToList();
                CmbMaster.ItemsSource = masters;
                CmbMaster.DisplayMemberPath = "USERS.full_name";
                CmbMaster.SelectedValuePath = "id";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка загрузки мастеров: {ex.Message}");
            }
        }

        private void LoadStatuses()
        {
            try
            {
                CmbStatus.ItemsSource = db.STATUSASSETS.ToList();
                CmbStatus.DisplayMemberPath = "Status";
                CmbStatus.SelectedValuePath = "ID_status";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка загрузки статусов: {ex.Message}");
            }
        }

        private void RefreshAssetsComboBoxForEdit()
        {
            if (_currentEquipment == null) return;

            try
            {
                var allAssets = db.PRODUCTION_ASSETS
                                  .Where(a => a.asset_type == 6)
                                  .ToList();

                var usedAssetIds = db.EQUIPMENT
                                     .Where(e => e.asset_id != null && e.ID != _equipmentId.Value)
                                     .Select(e => e.asset_id)
                                     .Distinct()
                                     .ToList();

                var availableAssets = allAssets
                    .Where(a => !usedAssetIds.Contains(a.id.ToString()) || a.id.ToString() == _currentEquipment.asset_id)
                    .ToList();

                CmbAsset.ItemsSource = availableAssets;
                CmbAsset.DisplayMemberPath = "name";
                CmbAsset.SelectedValuePath = "id";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка обновления списка активов: {ex.Message}");
            }
        }

        private void LoadEquipmentData()
        {
            try
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

                RefreshAssetsComboBoxForEdit();

                // Заполнение полей
                if (int.TryParse(_currentEquipment.asset_id, out int assetId))
                {
                    CmbAsset.SelectedValue = assetId;
                }

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
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CmbAsset_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Можно автоматически заполнять поля из выбранного актива
            if (CmbAsset.SelectedItem is PRODUCTION_ASSETS selectedAsset)
            {
                // Например: TxtEquipmentType.Text = selectedAsset.name; - по желанию
            }
        }

        private bool ValidateFields()
        {
            if (CmbAsset.SelectedValue == null)
            {
                ShowWarning("Выберите производственный актив.");
                CmbAsset.Focus();
                return false;
            }
            if (string.IsNullOrWhiteSpace(TxtEquipmentType.Text))
            {
                ShowWarning("Введите тип оборудования.");
                TxtEquipmentType.Focus();
                return false;
            }
            if (string.IsNullOrWhiteSpace(TxtManufacturer.Text))
            {
                ShowWarning("Введите производителя.");
                TxtManufacturer.Focus();
                return false;
            }
            if (CmbWorkshop.SelectedValue == null)
            {
                ShowWarning("Выберите цех.");
                CmbWorkshop.Focus();
                return false;
            }
            if (CmbMaster.SelectedValue == null)
            {
                ShowWarning("Выберите ответственного мастера.");
                CmbMaster.Focus();
                return false;
            }
            if (CmbStatus.SelectedValue == null)
            {
                ShowWarning("Выберите статус оборудования.");
                CmbStatus.Focus();
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
                    ShowSuccess("Оборудование успешно обновлено.");
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
                    ShowSuccess("Оборудование успешно добавлено.");
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при сохранении: {ex.Message}");
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!_equipmentId.HasValue) return;

            var result = MessageBox.Show("Вы уверены, что хотите удалить это оборудование?\n\nЭто действие нельзя отменить.",
                                         "Подтверждение удаления",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var equipment = db.EQUIPMENT.Find(_equipmentId.Value);
                    if (equipment != null)
                    {
                        db.EQUIPMENT.Remove(equipment);
                        db.SaveChanges();
                        ShowSuccess("Оборудование успешно удалено.");
                        DialogResult = true;
                        Close();
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"Ошибка при удалении: {ex.Message}");
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
            // Разрешаем только цифры
            e.Handled = !char.IsDigit(e.Text, 0);
        }

        private int? ParseInt(string text)
        {
            if (int.TryParse(text, out int result))
                return result;
            return null;
        }

        private void ShowWarning(string message)
        {
            MessageBox.Show(message, "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ShowSuccess(string message)
        {
            MessageBox.Show(message, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        protected override void OnClosed(EventArgs e)
        {
            db?.Dispose();
            base.OnClosed(e);
        }
    }
}