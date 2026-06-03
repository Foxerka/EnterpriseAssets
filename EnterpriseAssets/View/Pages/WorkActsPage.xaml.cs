using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using EnterpriseAssets.Model;
using EnterpriseAssets.Model.DataBase;

namespace EnterpriseAssets.View.Pages
{
    public partial class WorkActsPage : Page
    {
        private DB_AssetManage db = new DB_AssetManage();
        private List<WorkActViewModel> _allActsViewModels;
        private List<WORK_ACTS> _allActs;
        private int _currentUserId;
        private int? _currentMasterId;
        private bool _isAdmin;
        private List<ActStatus> _allStatuses;

        public WorkActsPage()
        {
            InitializeComponent();
        }

        public WorkActsPage(int currentUserId)
        {
            InitializeComponent();
            _currentUserId = currentUserId;
        }

        private void LoadCurrentUser()
        {
            try
            {
                _isAdmin = Session.IsAdmin;
                _currentMasterId = Session.CurrentMasterId;

                if (_isAdmin)
                {
                    TxtAccessInfo.Text = $"Вы {Session.CurrentUserRole} ({Session.CurrentUserName}) — видите все акты и можете управлять ими";
                }
                else
                {
                    TxtAccessInfo.Text = $"Вы видите только свои акты ({Session.CurrentUserName}). Для просмотра всех обратитесь к администратору.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки пользователя: {ex.Message}",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadStatuses();
            LoadWorkActs();

            // Настраиваем RadioButton'ы в зависимости от прав
            if (!_isAdmin)
            {
                RbMyActs.IsChecked = true;
                RbAllActs.IsEnabled = false;
                RbAllActs.ToolTip = "Только администраторы могут просматривать все акты";
            }
        }

        private void LoadStatuses()
        {
            try
            {
                _allStatuses = db.ActStatus.OrderBy(s => s.ID_status).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки статусов: {ex.Message}",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadWorkActs()
        {
            try
            {
                var query = db.WORK_ACTS
                    .Include("ActStatus")
                    .Include("EQUIPMENT")
                    .Include("MASTERS")
                    .Include("MASTERS.USERS")
                    .Include("PRODUCTION_ASSETS")
                    .Include("COMPLETION_ACTS")
                    .Include("WORK_ACTS_MATERIALS")
                    .AsQueryable();

                // Фильтрация по правам доступа
                bool showMyOnly = RbMyActs.IsChecked == true || !_isAdmin;

                if (showMyOnly && _currentMasterId.HasValue)
                {
                    query = query.Where(a => a.master_id == _currentMasterId.Value);
                }

                _allActs = query.OrderByDescending(a => a.work_date).ToList();
                _allActsViewModels = _allActs.Select(MapToViewModel).ToList();

                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}\n\n{ex.InnerException?.Message}",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private WorkActViewModel MapToViewModel(WORK_ACTS act)
        {
            // Отображение оборудования
            string equipmentDisplay = "—";
            if (act.EQUIPMENT != null)
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(act.EQUIPMENT.asset_id))
                    parts.Add(act.EQUIPMENT.asset_id);
                if (!string.IsNullOrWhiteSpace(act.EQUIPMENT.equipment_type))
                    parts.Add(act.EQUIPMENT.equipment_type);
                if (!string.IsNullOrWhiteSpace(act.EQUIPMENT.manufacturer))
                    parts.Add(act.EQUIPMENT.manufacturer);
                equipmentDisplay = parts.Count > 0
                    ? string.Join(" • ", parts)
                    : $"Оборудование #{act.EQUIPMENT.ID}";
            }

            // Отображение актива
            string assetDisplay = "—";
            if (act.PRODUCTION_ASSETS != null)
            {
                assetDisplay = !string.IsNullOrWhiteSpace(act.PRODUCTION_ASSETS.name)
                    ? act.PRODUCTION_ASSETS.name
                    : $"Актив #{act.PRODUCTION_ASSETS.id}";
            }

            string statusName = act.ActStatus?.Status ?? "Черновик";
            var completionAct = act.COMPLETION_ACTS?.FirstOrDefault();

            // Определяем права доступа
            bool isMyAct = act.master_id == _currentMasterId;
            bool canEdit = _isAdmin || isMyAct;
            bool canChangeStatus = _isAdmin || isMyAct;

            return new WorkActViewModel
            {
                Id = act.id,
                ActNumber = act.act_number ?? $"Акт #{act.id}",
                ActDate = act.work_date,
                Description = act.work_type ?? "Без описания",
                EquipmentDisplay = equipmentDisplay,
                AssetDisplay = assetDisplay,
                MasterName = act.MASTERS?.USERS?.full_name ?? "Не назначен",
                MasterId = act.master_id,
                StatusId = act.status,
                StatusName = statusName,
                Quantity = act.quantity,
                EquipmentId = act.equipment_id,
                HasCompletionAct = act.COMPLETION_ACTS != null && act.COMPLETION_ACTS.Any(),
                CompletionDate = completionAct?.completion_date,
                MaterialsCount = act.WORK_ACTS_MATERIALS?.Count ?? 0,
                IsMyAct = isMyAct,
                CanEdit = canEdit,
                CanChangeStatus = canChangeStatus
            };
        }

        private void ApplyFilter()
        {
            if (_allActsViewModels == null) return;

            var filtered = _allActsViewModels.AsEnumerable();

            string search = SearchBox.Text?.Trim().ToLower();
            if (!string.IsNullOrEmpty(search))
            {
                filtered = filtered.Where(a =>
                    (a.ActNumber?.ToLower().Contains(search) ?? false) ||
                    (a.Description?.ToLower().Contains(search) ?? false) ||
                    (a.EquipmentDisplay?.ToLower().Contains(search) ?? false) ||
                    (a.AssetDisplay?.ToLower().Contains(search) ?? false) ||
                    (a.MasterName?.ToLower().Contains(search) ?? false) ||
                    (a.StatusName?.ToLower().Contains(search) ?? false));
            }

            var resultList = filtered.ToList();
            WorkActsList.ItemsSource = resultList;
            TxtTotalCount.Text = resultList.Count.ToString();

            // После привязки данных заполняем ComboBox'ы статусов
            Dispatcher.BeginInvoke(new Action(FillStatusComboBoxes));
        }

        private void FillStatusComboBoxes()
        {
            try
            {
                // Находим все ComboBox с именем CmbQuickStatus в визуальном дереве
                foreach (var item in WorkActsList.Items)
                {
                    var container = WorkActsList.ItemContainerGenerator.ContainerFromItem(item) as ContentPresenter;
                    if (container == null) continue;

                    var cmb = FindVisualChild<ComboBox>(container, "CmbQuickStatus");
                    if (cmb == null) continue;

                    var vm = item as WorkActViewModel;
                    if (vm == null) continue;

                    // Заполняем ComboBox статусами
                    cmb.ItemsSource = _allStatuses;
                    cmb.DisplayMemberPath = "Status";
                    cmb.SelectedValuePath = "ID_status";
                    cmb.Tag = vm.Id;

                    // Устанавливаем текущий статус
                    if (vm.StatusId.HasValue)
                    {
                        cmb.SelectedValue = vm.StatusId.Value;
                    }

                    // Если нет прав — блокируем
                    if (!vm.CanChangeStatus)
                    {
                        cmb.IsEnabled = false;
                        cmb.ToolTip = "Нет прав на изменение статуса";
                    }
                    else
                    {
                        cmb.IsEnabled = true;
                        cmb.ToolTip = "Изменить статус акта";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FillStatusComboBoxes error: {ex.Message}");
            }
        }

        private T FindVisualChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T tChild)
                {
                    if (childName == null || (child is FrameworkElement fe && fe.Name == childName))
                        return tChild;
                }
                var result = FindVisualChild<T>(child, childName);
                if (result != null) return result;
            }
            return null;
        }

        private void QuickStatus_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cmb && cmb.SelectedValue is int newStatusId)
            {
                int actId;
                if (cmb.Tag is int tagInt)
                    actId = tagInt;
                else if (int.TryParse(cmb.Tag?.ToString(), out int parsed))
                    actId = parsed;
                else
                    return;

                try
                {
                    var act = db.WORK_ACTS.Find(actId);
                    if (act == null) return;

                    // Проверка прав
                    bool isMyAct = act.master_id == _currentMasterId;
                    if (!_isAdmin && !isMyAct)
                    {
                        MessageBox.Show("У вас нет прав на изменение статуса этого акта",
                                      "Доступ запрещён", MessageBoxButton.OK, MessageBoxImage.Warning);
                        LoadWorkActs();
                        return;
                    }

                    var oldStatus = act.status;
                    act.status = newStatusId;
                    db.SaveChanges();

                    var newStatus = _allStatuses.FirstOrDefault(s => s.ID_status == newStatusId);
                    MessageBox.Show($"Статус изменён: {newStatus?.Status ?? newStatusId.ToString()}",
                                  "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Если статус "Завершён" — создаём запись в COMPLETION_ACTS
                    if (newStatus?.Status == "Завершён" || newStatus?.Status == "Завершен")
                    {
                        CreateCompletionAct(act);
                    }

                    // Если это акт типа ТО — обновляем оборудование
                    UpdateEquipmentAfterAct(act, newStatus?.Status);

                    LoadWorkActs();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка изменения статуса: {ex.Message}",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CreateCompletionAct(WORK_ACTS act)
        {
            try
            {
                // Проверяем, нет ли уже акта завершения
                if (act.COMPLETION_ACTS != null && act.COMPLETION_ACTS.Any())
                    return;

                var completion = new COMPLETION_ACTS
                {
                    work_act_id = act.id,
                    completion_date = DateTime.Now,
                    quality_check = true,
                    comments = "Акт завершён автоматически при смене статуса"
                };
                db.COMPLETION_ACTS.Add(completion);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateCompletionAct error: {ex.Message}");
            }
        }

        private void UpdateEquipmentAfterAct(WORK_ACTS act, string statusName)
        {
            try
            {
                if (!act.equipment_id.HasValue) return;

                var equipment = db.EQUIPMENT.Find(act.equipment_id.Value);
                if (equipment == null) return;

                // Если акт завершён и это ТО — обновляем дату следующего ТО
                if ((statusName == "Завершён" || statusName == "Завершен") &&
                    (act.work_type?.ToLower().Contains("то") == true ||
                     act.work_type?.ToLower().Contains("обслуж") == true))
                {
                    equipment.last_maintenance_date = DateTime.Now;
                    equipment.next_maintenance_date = DateTime.Now.AddMonths(3); // +3 месяца
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateEquipmentAfterAct error: {ex.Message}");
            }
        }

        private void ViewMode_Changed(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                if (!_isAdmin)
                {
                    RbMyActs.IsChecked = true;
                }

                LoadWorkActs();
            }
        }

        private void NewAct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new WorkActDialog(_currentUserId);
                dialog.Owner = Window.GetWindow(this);
                if (dialog.ShowDialog() == true)
                {
                    LoadWorkActs();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ActCard_Click(object sender, MouseButtonEventArgs e)
        {
            // Если клик был по ComboBox — не открываем диалог
            if (e.OriginalSource is ComboBox || e.OriginalSource is ComboBoxItem)
                return;

            if (sender is Border border && border.Tag != null && int.TryParse(border.Tag.ToString(), out int actId))
            {
                var act = _allActs.FirstOrDefault(a => a.id == actId);
                if (act != null)
                {
                    try
                    {
                        var dialog = new WorkActDialog(act, _currentUserId, _isAdmin);
                        dialog.Owner = Window.GetWindow(this);
                        if (dialog.ShowDialog() == true)
                        {
                            LoadWorkActs();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadWorkActs();
        }
    }
}