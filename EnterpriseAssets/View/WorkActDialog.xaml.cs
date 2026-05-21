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

        // Класс для мастера
        public class MasterItem
        {
            public int id { get; set; }
            public string FullName { get; set; }
        }

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

            // Загрузка мастеров - используем конкретный тип
            var masters = db.MASTERS
                .Include("USERS")
                .Select(m => new MasterItem { id = m.id, FullName = m.USERS.full_name })
                .ToList();
            CmbMaster.ItemsSource = masters;

            // Загрузка статусов
            CmbStatus.ItemsSource = db.ActStatus.ToList();
        }

        private void LoadActData()
        {
            TxtActNumber.Text = _currentAct.act_number;
            DateAct.SelectedDate = _currentAct.work_date;
            TxtDescription.Text = _currentAct.work_type;

            if (_currentAct.equipment_id.HasValue)
            {
                CmbEquipment.SelectedValue = _currentAct.equipment_id;
            }

            if (_currentAct.master_id.HasValue)
            {
                // Ищем мастера по id
                var master = CmbMaster.ItemsSource.Cast<MasterItem>().FirstOrDefault(m => m.id == _currentAct.master_id);
                if (master != null)
                {
                    CmbMaster.SelectedItem = master;
                }
            }

            if (_currentAct.status.HasValue)
            {
                CmbStatus.SelectedValue = _currentAct.status;
            }

            // COMPLETION_ACTS - это коллекция, берем первый элемент
            var completionAct = _currentAct.COMPLETION_ACTS?.FirstOrDefault();
            if (completionAct != null)
            {
                ShowCompletionSection();
                TxtCompletionNumber.Text = completionAct.act_number;
                DateCompletion.SelectedDate = completionAct.completion_date;
                ChkQualityCheck.IsChecked = completionAct.quality_check;
                TxtCompletionComments.Text = completionAct.comments;
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
                _currentAct.work_date = DateAct.SelectedDate ?? DateTime.Now;
                _currentAct.work_type = TxtDescription.Text;
                _currentAct.created_at = DateTime.Now;

                if (CmbEquipment.SelectedItem is EQUIPMENT eq)
                    _currentAct.equipment_id = eq.ID;

                // Используем конкретный тип вместо dynamic
                if (CmbMaster.SelectedItem is MasterItem master)
                    _currentAct.master_id = master.id;

                if (CmbStatus.SelectedItem is ActStatus status)
                    _currentAct.status = status.ID_status;

                if (_isNewAct)
                {
                    db.WORK_ACTS.Add(_currentAct);
                }

                db.SaveChanges();

                // Сохранение акта завершения
                var selectedStatus = (ActStatus)CmbStatus.SelectedItem;
                var existingCompletion = _currentAct.COMPLETION_ACTS?.FirstOrDefault();

                if (selectedStatus?.Status == "Завершен" && existingCompletion == null)
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
                else if (selectedStatus?.Status == "Завершен" && existingCompletion != null)
                {
                    existingCompletion.act_number = TxtCompletionNumber.Text;
                    existingCompletion.completion_date = DateCompletion.SelectedDate;
                    existingCompletion.quality_check = ChkQualityCheck.IsChecked;
                    existingCompletion.comments = TxtCompletionComments.Text;
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