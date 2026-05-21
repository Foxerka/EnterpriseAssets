using System;
using System.Linq;
using System.Windows;
using EnterpriseAssets.Model.DataBase;

namespace EnterpriseAssets.View
{
    public partial class WorkActDialog : Window
    {
        private DB_AssetManage db = new DB_AssetManage();
        private WORK_ACTS _currentAct;
        private bool _isNewAct;
        private int _currentUserId;

        public WorkActDialog(int currentUserId)
        {
            InitializeComponent();
            _currentUserId = currentUserId;
            _isNewAct = true;
            _currentAct = new WORK_ACTS();
            Title = "Новый акт работ";
            LoadComboBoxes();
            DateAct.SelectedDate = DateTime.Now;
        }

        public WorkActDialog(WORK_ACTS act, int currentUserId) : this(currentUserId)
        {
            _currentAct = act;
            _isNewAct = false;
            Title = $"Акт работ №{act.act_number}";
            LoadActData();
        }

        private void LoadComboBoxes()
        {
            // Загрузка оборудования
            CmbEquipment.ItemsSource = db.EQUIPMENT.ToList();

            // Загрузка мастеров
            var masters = db.MASTERS
                .Include("USERS")
                .Select(m => new { m.id, FullName = m.USERS.full_name })
                .ToList();
            CmbMaster.ItemsSource = masters;

            // Загрузка статусов
            CmbStatus.ItemsSource = db.ActStatus.ToList();
        }

        private void LoadActData()
        {
            TxtActNumber.Text = _currentAct.act_number;
            DateAct.SelectedDate = _currentAct.act_date;
            TxtDescription.Text = _currentAct.description;

            if (_currentAct.equipment_id.HasValue)
            {
                CmbEquipment.SelectedValue = _currentAct.equipment_id;
            }

            if (_currentAct.master_id.HasValue)
            {
                CmbMaster.SelectedValue = _currentAct.master_id;
            }

            if (_currentAct.status_id.HasValue)
            {
                CmbStatus.SelectedValue = _currentAct.status_id;
            }

            // Загрузка акта завершения
            if (_currentAct.COMPLETION_ACTS != null)
            {
                ShowCompletionSection();
                TxtCompletionNumber.Text = _currentAct.COMPLETION_ACTS.act_number;
                DateCompletion.SelectedDate = _currentAct.COMPLETION_ACTS.completion_date;
                ChkQualityCheck.IsChecked = _currentAct.COMPLETION_ACTS.quality_check;
                TxtCompletionComments.Text = _currentAct.COMPLETION_ACTS.comments;
            }
        }

        private void CmbEquipment_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Дополнительная логика при выборе оборудования
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация
                if (string.IsNullOrWhiteSpace(TxtActNumber.Text))
                {
                    MessageBox.Show("Введите номер акта", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Заполнение акта работ
                _currentAct.act_number = TxtActNumber.Text;
                _currentAct.act_date = DateAct.SelectedDate ?? DateTime.Now;
                _currentAct.description = TxtDescription.Text;
                _currentAct.created_at = DateTime.Now;

                if (CmbEquipment.SelectedItem is EQUIPMENT eq)
                    _currentAct.equipment_id = eq.ID;

                if (CmbMaster.SelectedItem is dynamic master)
                    _currentAct.master_id = master.id;

                if (CmbStatus.SelectedItem is ActStatus status)
                    _currentAct.status_id = status.ID_status;

                if (_isNewAct)
                {
                    db.WORK_ACTS.Add(_currentAct);
                }

                db.SaveChanges();

                // Сохранение акта завершения
                var selectedStatus = (ActStatus)CmbStatus.SelectedItem;
                if (selectedStatus?.Status == "Завершен" && _currentAct.COMPLETION_ACTS == null)
                {
                    var completionAct = new COMPLETION_ACTS
                    {
                        work_act_id = _currentAct.id,
                        act_number = TxtCompletionNumber.Text,
                        completion_date = DateCompletion.SelectedDate,
                        quality_check = ChkQualityCheck.IsChecked,
                        comments = TxtCompletionComments.Text,
                        created_at = DateTime.Now
                    };
                    db.COMPLETION_ACTS.Add(completionAct);
                    db.SaveChanges();
                }
                else if (selectedStatus?.Status == "Завершен" && _currentAct.COMPLETION_ACTS != null)
                {
                    _currentAct.COMPLETION_ACTS.act_number = TxtCompletionNumber.Text;
                    _currentAct.COMPLETION_ACTS.completion_date = DateCompletion.SelectedDate;
                    _currentAct.COMPLETION_ACTS.quality_check = ChkQualityCheck.IsChecked;
                    _currentAct.COMPLETION_ACTS.comments = TxtCompletionComments.Text;
                    db.SaveChanges();
                }

                MessageBox.Show("Акт успешно сохранен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ShowCompletionSection()
        {
            CompletionSection.Visibility = Visibility.Visible;
        }
    }
}