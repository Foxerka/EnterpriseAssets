using System;
using System.Linq;
using System.Windows;
using EnterpriseAssets.Model.DataBase;

namespace EnterpriseAssets.View
{
    public partial class SupplierManage : Window
    {
        private DB_AssetManage db = new DB_AssetManage();
        private SUPPLIERS _currentSupplier;
        private bool _isNewSupplier;

        // Конструктор для существующего поставщика
        public SupplierManage(SUPPLIERS supplier)
        {
            InitializeComponent();
            _currentSupplier = supplier;
            _isNewSupplier = false;
            Title = "Редактирование поставщика";
            LoadSupplierData();
            BtnDelete.Visibility = Visibility.Visible;
            CreatedAtSection.Visibility = Visibility.Visible;
        }

        // Конструктор для нового поставщика
        public SupplierManage()
        {
            InitializeComponent();
            _currentSupplier = new SUPPLIERS();
            _isNewSupplier = true;
            Title = "Добавление нового поставщика";
            SupplierName.Text = "Новый поставщик";
            SupplierStatus.Text = "Заполните информацию";
            ChkIsActive.IsChecked = true;
            BtnDelete.Visibility = Visibility.Collapsed;
            CreatedAtSection.Visibility = Visibility.Collapsed;
        }

        private void LoadSupplierData()
        {
            if (_currentSupplier == null) return;

            TxtName.Text = _currentSupplier.name;
            TxtContactPerson.Text = _currentSupplier.contact_person;
            TxtPhone.Text = _currentSupplier.phone;
            TxtEmail.Text = _currentSupplier.email;
            TxtAddress.Text = _currentSupplier.address;
            TxtTaxNumber.Text = _currentSupplier.tax_number;
            ChkIsActive.IsChecked = _currentSupplier.is_active ?? true;

            SupplierName.Text = _currentSupplier.name ?? "Без названия";
            SupplierStatus.Text = (_currentSupplier.is_active == true) ? "Активен" : "Неактивен";

            if (_currentSupplier.created_at.HasValue)
            {
                TxtCreatedAt.Text = $"Добавлен: {_currentSupplier.created_at:dd.MM.yyyy}";
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация
                if (string.IsNullOrWhiteSpace(TxtName.Text))
                {
                    MessageBox.Show("Введите название компании", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Сохраняем данные
                _currentSupplier.name = TxtName.Text;
                _currentSupplier.contact_person = TxtContactPerson.Text;
                _currentSupplier.phone = TxtPhone.Text;
                _currentSupplier.email = TxtEmail.Text;
                _currentSupplier.address = TxtAddress.Text;
                _currentSupplier.tax_number = TxtTaxNumber.Text;
                _currentSupplier.is_active = ChkIsActive.IsChecked;

                if (_isNewSupplier)
                {
                    _currentSupplier.created_at = DateTime.Now;
                    db.SUPPLIERS.Add(_currentSupplier);
                }

                db.SaveChanges();

                MessageBox.Show("Данные успешно сохранены", "Успех",
                              MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить поставщика '{_currentSupplier.name}'?\n" +
                "Это действие нельзя отменить.",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Проверяем, есть ли связанные записи
                    var hasPurchases = db.EQUIPMENT_PURCHASES.Any(p => p.supplier_id == _currentSupplier.id);
                    var hasAssets = db.PRODUCTION_ASSETS.Any(a => a.supplier_id == _currentSupplier.id);

                    if (hasPurchases || hasAssets)
                    {
                        MessageBox.Show(
                            "Невозможно удалить поставщика, так как с ним связаны закупки или активы.\n" +
                            "Сначала удалите или переназначьте связанные записи.",
                            "Ошибка удаления",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return;
                    }

                    db.SUPPLIERS.Remove(_currentSupplier);
                    db.SaveChanges();

                    MessageBox.Show("Поставщик успешно удален", "Успех",
                                  MessageBoxButton.OK, MessageBoxImage.Information);

                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}