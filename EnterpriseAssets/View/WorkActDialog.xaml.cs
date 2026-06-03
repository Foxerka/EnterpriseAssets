using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using EnterpriseAssets.Model.DataBase;

namespace EnterpriseAssets.View.Pages
{
    public partial class WorkActDialog : Window
    {
        private DB_AssetManage db = new DB_AssetManage();
        private int _currentUserId;
        private int? _currentMasterId;
        private bool _isAdmin;
        private WORK_ACTS _editingAct;
        private bool _isNew;
        private bool _canEdit;

        public WorkActDialog(int currentUserId, int? preselectedEquipmentId = null)
        {
            InitializeComponent();
            _currentUserId = currentUserId;
            _isNew = true;
            _canEdit = true;
            LoadCurrentUser();
            LoadData(preselectedEquipmentId);
            LoadStatuses();

            DpWorkDate.SelectedDate = DateTime.Now;
            TxtActNumber.Text = $"АКТ-{DateTime.Now:yyyyMMdd}-{GetNextActNumber()}";
        }

        public WorkActDialog(WORK_ACTS act, int currentUserId, bool isAdmin)
        {
            InitializeComponent();
            _currentUserId = currentUserId;
            _isAdmin = isAdmin;
            _editingAct = act;
            _isNew = false;
            LoadCurrentUser();

            // Проверка прав на редактирование
            _canEdit = _isAdmin || (_editingAct.master_id == _currentMasterId);

            TxtTitle.Text = _canEdit
                ? $"Редактирование: {act.act_number}"
                : $"Просмотр: {act.act_number} (только чтение)";

            LoadData();
            LoadStatuses();
            FillForm();

            // Если нет прав — блокируем форму
            if (!_canEdit)
            {
                TxtActNumber.IsEnabled = false;
                DpWorkDate.IsEnabled = false;
                CmbEquipment.IsEnabled = false;
                CmbAsset.IsEnabled = false;
                TxtQuantity.IsEnabled = false;
                CmbMaster.IsEnabled = false;
                TxtDescription.IsEnabled = false;
                CmbStatus.IsEnabled = false;
            }
        }

        private void LoadCurrentUser()
        {
            try
            {
                var user = db.USERS.Include("ROLES").FirstOrDefault(u => u.id == _currentUserId);
                _isAdmin = user?.ROLES?.name == "Администратор" ||
                           user?.ROLES?.name == "Руководитель";

                var master = db.MASTERS.FirstOrDefault(m => m.user_id == _currentUserId);
                _currentMasterId = master?.id;
            }
            catch { }
        }

        private void LoadData(int? preselectedEquipmentId = null)
        {
            try
            {
                var equipment = db.EQUIPMENT
                    .OrderBy(e => e.asset_id)
                    .ToList()
                    .Select(e => new EquipmentDisplay
                    {
                        Id = e.ID,
                        DisplayName = FormatEquipmentName(e)
                    })
                    .ToList();
                CmbEquipment.ItemsSource = equipment;

                if (preselectedEquipmentId.HasValue)
                {
                    CmbEquipment.SelectedItem = equipment.FirstOrDefault(e => e.Id == preselectedEquipmentId.Value);
                }

                var assets = db.PRODUCTION_ASSETS
                    .Include("CATEGORY")
                    .OrderBy(a => a.name)
                    .ToList()
                    .Select(a => new AssetDisplay
                    {
                        Id = a.id,
                        DisplayName = !string.IsNullOrWhiteSpace(a.name) ? a.name : $"Актив #{a.id}"
                    })
                    .ToList();
                CmbAsset.ItemsSource = assets;

                var masters = db.MASTERS
                    .Include("USERS")
                    .Where(m => m.user_id != null)
                    .ToList()
                    .Select(m => new MasterDisplay
                    {
                        Id = m.id,
                        DisplayName = m.USERS?.full_name ?? $"Мастер #{m.id}"
                    })
                    .OrderBy(m => m.DisplayName)
                    .ToList();
                CmbMaster.ItemsSource = masters;

                if (_currentMasterId.HasValue && _isNew)
                {
                    var currentMaster = masters.FirstOrDefault(m => m.Id == _currentMasterId.Value);
                    if (currentMaster != null)
                    {
                        CmbMaster.SelectedItem = currentMaster;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadStatuses()
        {
            try
            {
                var statuses = db.ActStatus.OrderBy(s => s.ID_status).ToList();
                CmbStatus.ItemsSource = statuses;
                CmbStatus.DisplayMemberPath = "Status";
                CmbStatus.SelectedValuePath = "ID_status";
                if (statuses.Any()) CmbStatus.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки статусов: {ex.Message}",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string FormatEquipmentName(EQUIPMENT e)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(e.asset_id)) parts.Add(e.asset_id);
            if (!string.IsNullOrWhiteSpace(e.equipment_type)) parts.Add(e.equipment_type);
            if (!string.IsNullOrWhiteSpace(e.manufacturer)) parts.Add(e.manufacturer);
            return parts.Count > 0 ? string.Join(" • ", parts) : $"Оборудование #{e.ID}";
        }

        private void FillForm()
        {
            if (_editingAct == null) return;

            TxtActNumber.Text = _editingAct.act_number;
            DpWorkDate.SelectedDate = _editingAct.work_date;
            TxtDescription.Text = _editingAct.work_type;
            TxtQuantity.Text = _editingAct.quantity?.ToString() ?? "";

            if (_editingAct.equipment_id.HasValue)
            {
                CmbEquipment.SelectedItem = CmbEquipment.ItemsSource
                    .Cast<EquipmentDisplay>()
                    .FirstOrDefault(e => e.Id == _editingAct.equipment_id.Value);
            }

            if (_editingAct.asset_id.HasValue)
            {
                CmbAsset.SelectedItem = CmbAsset.ItemsSource
                    .Cast<AssetDisplay>()
                    .FirstOrDefault(a => a.Id == _editingAct.asset_id.Value);
            }

            if (_editingAct.master_id.HasValue)
            {
                CmbMaster.SelectedItem = CmbMaster.ItemsSource
                    .Cast<MasterDisplay>()
                    .FirstOrDefault(m => m.Id == _editingAct.master_id.Value);
            }

            if (_editingAct.status.HasValue)
            {
                CmbStatus.SelectedValue = _editingAct.status.Value;
            }
        }

        private int GetNextActNumber()
        {
            try
            {
                var lastAct = db.WORK_ACTS.OrderByDescending(a => a.id).FirstOrDefault();
                return (lastAct?.id ?? 0) + 1;
            }
            catch { return 1; }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(TxtActNumber.Text))
            {
                MessageBox.Show("Введите номер акта", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (!DpWorkDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Укажите дату работ", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (CmbEquipment.SelectedItem == null)
            {
                MessageBox.Show("Выберите оборудование", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (CmbMaster.SelectedItem == null)
            {
                MessageBox.Show("Выберите исполнителя", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (string.IsNullOrWhiteSpace(TxtDescription.Text))
            {
                MessageBox.Show("Введите описание работ", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (!string.IsNullOrWhiteSpace(TxtQuantity.Text))
            {
                if (!decimal.TryParse(TxtQuantity.Text, out decimal q) || q < 0)
                {
                    MessageBox.Show("Количество должно быть неотрицательным числом", "Внимание",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }
            return true;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!_canEdit)
            {
                MessageBox.Show("У вас нет прав на редактирование этого акта",
                              "Доступ запрещён", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ValidateInput()) return;

            try
            {
                var selectedEquipment = CmbEquipment.SelectedItem as EquipmentDisplay;
                var selectedAsset = CmbAsset.SelectedItem as AssetDisplay;
                var selectedMaster = CmbMaster.SelectedItem as MasterDisplay;
                var selectedStatus = CmbStatus.SelectedItem as ActStatus;

                decimal? quantity = null;
                if (!string.IsNullOrWhiteSpace(TxtQuantity.Text) && decimal.TryParse(TxtQuantity.Text, out decimal q))
                    quantity = q;

                if (_isNew)
                {
                    var newAct = new WORK_ACTS
                    {
                        act_number = TxtActNumber.Text.Trim(),
                        work_date = DpWorkDate.SelectedDate.Value,
                        equipment_id = selectedEquipment?.Id,
                        asset_id = selectedAsset?.Id,
                        quantity = quantity,
                        master_id = selectedMaster?.Id,
                        work_type = TxtDescription.Text.Trim(),
                        status = selectedStatus?.ID_status,
                        created_at = DateTime.Now
                    };
                    db.WORK_ACTS.Add(newAct);

                    // Если это ТО — создаём запись в MAINTENANCE
                    if (IsMaintenanceAct(TxtDescription.Text))
                    {
                        CreateMaintenanceRecord(newAct, selectedEquipment?.Id, selectedMaster?.Id);
                    }
                }
                else
                {
                    _editingAct.act_number = TxtActNumber.Text.Trim();
                    _editingAct.work_date = DpWorkDate.SelectedDate.Value;
                    _editingAct.equipment_id = selectedEquipment?.Id;
                    _editingAct.asset_id = selectedAsset?.Id;
                    _editingAct.quantity = quantity;
                    _editingAct.master_id = selectedMaster?.Id;
                    _editingAct.work_type = TxtDescription.Text.Trim();
                    _editingAct.status = selectedStatus?.ID_status;
                }

                db.SaveChanges();

                MessageBox.Show(_isNew ? "Акт успешно создан!" : "Акт успешно обновлён!",
                              "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}\n\n{ex.InnerException?.Message}",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool IsMaintenanceAct(string description)
        {
            if (string.IsNullOrWhiteSpace(description)) return false;
            var lower = description.ToLower();
            return lower.Contains("то") || lower.Contains("обслуживан") ||
                   lower.Contains("ремонт") || lower.Contains("проверк");
        }

        private void CreateMaintenanceRecord(WORK_ACTS act, int? equipmentId, int? masterId)
        {
            try
            {
                var maintenance = new MAINTENANCE
                {
                    equipment_id = equipmentId,
                    maintenance_date = act.work_date,
                    maintenance_type = "Плановое ТО",
                    technician_id = masterId,
                    description = act.work_type,
                    status = act.status,
                    created_at = DateTime.Now,
                    next_maintenance_date = DateTime.Now.AddMonths(3)
                };
                db.MAINTENANCE.Add(maintenance);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateMaintenanceRecord error: {ex.Message}");
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class EquipmentDisplay
    {
        public int Id { get; set; }
        public string DisplayName { get; set; }
        public override string ToString() => DisplayName;
    }

    public class AssetDisplay
    {
        public int Id { get; set; }
        public string DisplayName { get; set; }
        public override string ToString() => DisplayName;
    }

    public class MasterDisplay
    {
        public int Id { get; set; }
        public string DisplayName { get; set; }
        public override string ToString() => DisplayName;
    }
}