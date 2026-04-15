using System;
using System.Linq;
using System.Windows;
using EnterpriseAssets.Model.DataBase;

namespace EnterpriseAssets.View
{
    public partial class MaintenanceDialog : Window
    {
        private DB_AssetManage db = new DB_AssetManage();
        private int _equipmentId;
        private EQUIPMENT _equipment;

        public MaintenanceDialog(int equipmentId)
        {
            InitializeComponent();
            _equipmentId = equipmentId;
            Loaded += MaintenanceDialog_Loaded;
        }

        private void MaintenanceDialog_Loaded(object sender, RoutedEventArgs e)
        {
            _equipment = db.EQUIPMENT.FirstOrDefault(eq => eq.ID == _equipmentId);
            if (_equipment != null)
            {
                Title = $"Проведение ТО - {_equipment.asset_id}";
                TxtAssetInfo.Text = $"{_equipment.asset_id} | {_equipment.equipment_type}";
            }

            // Заполняем ComboBox типами ТО
            CmbMaintenanceType.Items.Add("Плановое");
            CmbMaintenanceType.Items.Add("Внеплановое");
            CmbMaintenanceType.Items.Add("Капитальное");
            CmbMaintenanceType.SelectedIndex = 0;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var maintenance = new MAINTENANCE
                {
                    equipment_id = _equipmentId,
                    maintenance_date = DpMaintenanceDate.SelectedDate ?? DateTime.Now,
                    maintenance_type = (CmbMaintenanceType.SelectedItem as string) ?? "Плановое",
                    description = TxtDescription.Text.Trim(),
                    parts_replaced = TxtParts.Text.Trim(),
                    cost = ParseDecimal(TxtCost.Text),
                    downtime_hours = ParseInt(TxtDowntime.Text),
                    next_maintenance_date = DpNextMaintenance.SelectedDate,
                    status = 1, // Статус "Выполнено" (подберите ID из STATUSASSETS)
                    created_at = DateTime.Now
                };

                db.MAINTENANCE.Add(maintenance);

                // Обновляем данные оборудования
                _equipment.last_maintenance_date = maintenance.maintenance_date;
                _equipment.next_maintenance_date = maintenance.next_maintenance_date;
                _equipment.current_work_hours = ParseInt(TxtCurrentHours.Text) ?? _equipment.current_work_hours;

                // Если оборудование было на обслуживании, меняем статус обратно
                if (_equipment.STATUSASSETS?.Status == "На обслуживании")
                {
                    var workingStatus = db.STATUSASSETS.FirstOrDefault(s => s.Status == "В эксплуатации");
                    if (workingStatus != null)
                        _equipment.status = workingStatus.ID_status;
                }

                db.SaveChanges();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TxtNumber_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, 0))
                e.Handled = true;
        }

        private decimal? ParseDecimal(string text)
        {
            if (decimal.TryParse(text, out decimal result))
                return result;
            return null;
        }

        private int? ParseInt(string text)
        {
            if (int.TryParse(text, out int result))
                return result;
            return null;
        }
    }
}