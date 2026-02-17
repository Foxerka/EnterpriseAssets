using System;
using System.Linq;
using System.Windows;
using EnterpriseAssets.Model.DataBase;

namespace EnterpriseAssets.View
{
    public partial class RoleManage : Window
    {
        private DB_AssetManage db = new DB_AssetManage();
        private ROLES _currentRole;
        private bool _isNewRole;

        // Конструктор для новой роли
        public RoleManage()
        {
            InitializeComponent();
            _currentRole = new ROLES();
            _isNewRole = true;
            Title = "Добавление новой роли";
            BtnDelete.Visibility = Visibility.Collapsed;
            LoadUsersList();
        }

        public RoleManage(ROLES role)
        {
            InitializeComponent();
            _currentRole = db.ROLES.Include("USERS").FirstOrDefault(r => r.id == role.id);

            if (_currentRole == null)
            {
                MessageBox.Show("Роль не найдена", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                DialogResult = false;
                Close();
                return;
            }

            _isNewRole = false;
            Title = $"Редактирование роли: {_currentRole.name}";
            LoadRoleData();
            LoadUsersList();
        }

        private void LoadRoleData()
        {
            TxtRoleName.Text = _currentRole.name;
            TxtDescription.Text = _currentRole.description;
        }

        private void LoadUsersList()
        {
            if (_isNewRole)
            {
                UsersInRoleList.ItemsSource = null;
                TxtUsersCount.Text = "0 пользователей (роль еще не создана)";
                return;
            }

            // Загружаем пользователей, у которых эта роль
            var users = db.USERS.Where(u => u.role_id == _currentRole.id).ToList();
            UsersInRoleList.ItemsSource = users;
            TxtUsersCount.Text = $"{users.Count} пользователей";
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TxtRoleName.Text))
                {
                    MessageBox.Show("Введите название роли", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверка на уникальность имени (кроме текущей роли)
                var existingRole = db.ROLES.FirstOrDefault(r => r.name == TxtRoleName.Text && r.id != _currentRole.id);
                if (existingRole != null)
                {
                    MessageBox.Show("Роль с таким названием уже существует", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_isNewRole)
                {
                    _currentRole.name = TxtRoleName.Text;
                    _currentRole.description = TxtDescription.Text;
                    db.ROLES.Add(_currentRole);
                }
                else
                {
                    // Для существующей роли загружаем актуальную запись
                    var roleInDb = db.ROLES.Find(_currentRole.id);
                    if (roleInDb != null)
                    {
                        roleInDb.name = TxtRoleName.Text;
                        roleInDb.description = TxtDescription.Text;
                    }
                }

                db.SaveChanges();

                MessageBox.Show("Роль успешно сохранена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_isNewRole)
            {
                DialogResult = false;
                Close();
                return;
            }

            var usersCount = db.USERS.Count(u => u.role_id == _currentRole.id);

            if (usersCount > 0)
            {
                MessageBox.Show(
                    $"Невозможно удалить роль, так как в ней находится {usersCount} пользователей.\n" +
                    "Сначала измените роль у всех пользователей.",
                    "Ошибка удаления",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить роль \"{_currentRole.name}\"?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var roleToDelete = db.ROLES.Find(_currentRole.id);
                    if (roleToDelete != null)
                    {
                        db.ROLES.Remove(roleToDelete);
                        db.SaveChanges();
                        MessageBox.Show("Роль успешно удалена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        DialogResult = true;
                        Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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