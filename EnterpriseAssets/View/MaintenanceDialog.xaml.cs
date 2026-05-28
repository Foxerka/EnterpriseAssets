using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
            SetupInputMasks();
            SetDefaultDates();
        }

        private void SetupInputMasks()
        {
            // Маска для описания работ
            TxtDescription.PreviewTextInput += (s, e) =>
            {
                e.Handled = !Regex.IsMatch(e.Text, @"^[a-zA-Zа-яА-Я0-9\s\-\.\,\!\(\)]+$");
            };

            // Маска для деталей
            TxtParts.PreviewTextInput += (s, e) =>
            {
                e.Handled = !Regex.IsMatch(e.Text, @"^[a-zA-Zа-яА-Я0-9\s\-\.\,\!\(\)]+$");
            };
        }

        private void SetDefaultDates()
        {
            DpMaintenanceDate.SelectedDate = DateTime.Now;
        }

        private void MaintenanceDialog_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _equipment = db.EQUIPMENT.FirstOrDefault(eq => eq.ID == _equipmentId);
                if (_equipment != null)
                {
                    Title = $"Проведение ТО - {_equipment.asset_id}";
                    TxtAssetInfo.Text = $"{_equipment.asset_id} | {_equipment.equipment_type}";

                    // Заполняем текущие данные для удобства
                    if (_equipment.current_work_hours.HasValue)
                        TxtCurrentHours.Text = _equipment.current_work_hours.ToString();

                    if (_equipment.next_maintenance_date.HasValue)
                        DpNextMaintenance.SelectedDate = _equipment.next_maintenance_date;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки: {ex.Message}");
            }
        }

        private bool ValidateFields()
        {
            if (!DpMaintenanceDate.SelectedDate.HasValue)
            {
                ShowWarning("Выберите дату проведения ТО.");
                DpMaintenanceDate.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(TxtDescription.Text))
            {
                ShowWarning("Введите описание выполненных работ.");
                TxtDescription.Focus();
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
                var maintenance = new MAINTENANCE
                {
                    equipment_id = _equipmentId,
                    maintenance_date = DpMaintenanceDate.SelectedDate ?? DateTime.Now,
                    maintenance_type = (CmbMaintenanceType.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Плановое",
                    description = TxtDescription.Text.Trim(),
                    parts_replaced = string.IsNullOrWhiteSpace(TxtParts.Text) ? null : TxtParts.Text.Trim(),
                    cost = ParseDecimal(TxtCost.Text),
                    downtime_hours = ParseInt(TxtDowntime.Text),
                    next_maintenance_date = DpNextMaintenance.SelectedDate,
                    status = GetStatusId(), // Статус "Выполнено"
                    created_at = DateTime.Now
                };

                db.MAINTENANCE.Add(maintenance);

                // Обновляем данные оборудования
                if (_equipment != null)
                {
                    _equipment.last_maintenance_date = maintenance.maintenance_date;
                    _equipment.next_maintenance_date = maintenance.next_maintenance_date;

                    if (ParseInt(TxtCurrentHours.Text).HasValue)
                        _equipment.current_work_hours = ParseInt(TxtCurrentHours.Text);

                    // Обновляем статус оборудования по выбору пользователя
                    var selectedStatus = (CmbNewStatus.SelectedItem as ComboBoxItem)?.Content.ToString();
                    var newStatus = db.STATUSASSETS.FirstOrDefault(s => s.Status == selectedStatus);
                    if (newStatus != null)
                        _equipment.status = newStatus.ID_status;
                }

                db.SaveChanges();
                ShowSuccess("Запись о техническом обслуживании успешно сохранена.");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при сохранении: {ex.Message}");
            }
        }

        private int? GetStatusId()
        {
            // Находим статус "Выполнено" или аналогичный
            var status = db.STATUSASSETS.FirstOrDefault(s => s.Status == "Выполнено");
            if (status != null) return status.ID_status;

            // Или используем статус "В эксплуатации" как альтернативу
            var alternativeStatus = db.STATUSASSETS.FirstOrDefault(s => s.Status == "В эксплуатации");
            return alternativeStatus?.ID_status ?? 1;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TxtNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем только цифры
            e.Handled = !Regex.IsMatch(e.Text, @"^[0-9]+$");
        }

        private void TxtDecimal_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;
            string currentText = textBox?.Text ?? "";
            string newText = currentText.Insert(textBox.SelectionStart, e.Text);

            // Разрешаем только цифры и точку
            if (!Regex.IsMatch(e.Text, @"^[0-9\.]$"))
            {
                e.Handled = true;
                return;
            }

            // Проверяем, что точка только одна
            if (e.Text == "." && currentText.Contains("."))
            {
                e.Handled = true;
                return;
            }

            // Проверяем, что после точки не более 2 цифр
            if (currentText.Contains("."))
            {
                int dotIndex = currentText.IndexOf('.');
                int decimalDigits = currentText.Length - dotIndex - 1;
                if (decimalDigits >= 2 && textBox.SelectionStart > dotIndex)
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        private decimal? ParseDecimal(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            if (decimal.TryParse(text, out decimal result))
                return result;
            return null;
        }

        private int? ParseInt(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
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