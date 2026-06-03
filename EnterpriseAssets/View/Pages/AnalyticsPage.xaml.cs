using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using EnterpriseAssets.Model.DataBase;

namespace EnterpriseAssets.View
{
    public partial class AnalyticsPage : Page
    {
        private DB_AssetManage db = new DB_AssetManage();
        private string currentSection = "Overview";
        private DateTime? periodStart;
        private DateTime? periodEnd;

        private Dictionary<Button, string> _buttonSections = new Dictionary<Button, string>();
        private bool _buttonsInitialized = false;

        public AnalyticsPage()
        {
            InitializeComponent();

            // Период по умолчанию — текущий месяц
            DpPeriodStart.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            DpPeriodEnd.SelectedDate = DateTime.Now;
            periodStart = DpPeriodStart.SelectedDate;
            periodEnd = DpPeriodEnd.SelectedDate;

            LoadKpiData();
            ShowSection("Overview");
        }

        #region KPI
        private void LoadKpiData()
        {
            try
            {
                // Всего активов
                var totalAssets = db.PRODUCTION_ASSETS.Count();
                TxtTotalAssets.Text = totalAssets.ToString();

                // Активы, добавленные за текущий месяц
                var thisMonth = DateTime.Now.Month;
                var thisYear = DateTime.Now.Year;
                var assetsThisMonth = db.PRODUCTION_ASSETS.Count(a =>
                    a.created_at.HasValue &&
                    a.created_at.Value.Month == thisMonth &&
                    a.created_at.Value.Year == thisYear);
                TxtAssetsChange.Text = $"+{assetsThisMonth} за месяц";

                // Общая стоимость (current_value)
                var totalValue = db.PRODUCTION_ASSETS
                    .Where(a => a.current_value.HasValue)
                    .Sum(a => a.current_value) ?? 0;
                TxtTotalValue.Text = FormatMoney(totalValue);

                // В ремонте (status = id статуса "В ремонте")
                // Получаем ID статуса "В ремонте" из таблицы STATUSASSETS
                var repairStatus = db.STATUSASSETS.FirstOrDefault(s =>
                    s.Status != null &&
                    (s.Status.ToLower().Contains("ремонт") || s.Status.ToLower().Contains("ремонте")));
                int repairStatusId = repairStatus?.ID_status ?? 0;

                var inRepair = repairStatusId > 0
                    ? db.PRODUCTION_ASSETS.Count(a => a.status == repairStatusId)
                    : 0;
                TxtInRepair.Text = inRepair.ToString();
                TxtRepairPercent.Text = totalAssets > 0
                    ? $"{(inRepair * 100 / totalAssets)}% от всех"
                    : "0%";

                // На складе
                var warehouseStatus = db.STATUSASSETS.FirstOrDefault(s =>
                    s.Status != null &&
                    (s.Status.ToLower().Contains("склад") || s.Status.ToLower().Contains("складе")));
                int warehouseStatusId = warehouseStatus?.ID_status ?? 0;

                var inWarehouse = warehouseStatusId > 0
                    ? db.PRODUCTION_ASSETS.Count(a => a.status == warehouseStatusId)
                    : 0;
                TxtInWarehouse.Text = inWarehouse.ToString();
                TxtWarehousePercent.Text = totalAssets > 0
                    ? $"{(inWarehouse * 100 / totalAssets)}% от всех"
                    : "0%";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки KPI: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Навигация
        private void SectionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                // Инициализируем словарь при первом клике
                if (!_buttonsInitialized)
                {
                    InitializeButtonSections();
                }

                // Получаем имя раздела из словаря
                if (_buttonSections.TryGetValue(btn, out string section))
                {
                    currentSection = section;
                    UpdateNavButtons(btn);
                    ShowSection(section);
                }
            }
        }

        private void InitializeButtonSections()
        {
            // Находим все кнопки навигации и запоминаем их разделы из Tag
            if (BtnNavOverview != null)
            {
                _buttonSections[BtnNavOverview] = "Overview";
                _buttonSections[BtnNavCategories] = "Categories";
                _buttonSections[BtnNavStatuses] = "Statuses";
                _buttonSections[BtnNavWorkshops] = "Workshops";
                _buttonSections[BtnNavFinance] = "Finance";
                _buttonSections[BtnNavMaintenance] = "Maintenance";
                _buttonsInitialized = true;
            }
        }



        private void UpdateNavButtons(Button activeButton)
        {
            // Сбрасываем стиль всех кнопок
            foreach (var btn in _buttonSections.Keys)
            {
                btn.Background = Brushes.Transparent;
                btn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555555"));
                btn.FontWeight = FontWeights.Normal;
            }

            // Выделяем активную
            activeButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3"));
            activeButton.Foreground = Brushes.White;
            activeButton.FontWeight = FontWeights.Bold;
        }

        private void ShowSection(string section)
        {
            switch (section)
            {
                case "Overview": ShowOverviewSection(); break;
                case "Categories": ShowCategoriesSection(); break;
                case "Statuses": ShowStatusesSection(); break;
                case "Workshops": ShowWorkshopsSection(); break;
                case "Finance": ShowFinanceSection(); break;
                case "Maintenance": ShowMaintenanceSection(); break;
            }
        }
        #endregion

        #region Раздел: Общий обзор
        private void ShowOverviewSection()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var categoriesBlock = CreateSectionBlock("📁 Топ категорий активов", BuildCategoriesContent());
            Grid.SetRow(categoriesBlock, 0);
            grid.Children.Add(categoriesBlock);

            var shortageBlock = CreateSectionBlock("⚠️ Требуют пополнения (ниже минимума)", BuildShortageContent());
            Grid.SetRow(shortageBlock, 1);
            grid.Children.Add(shortageBlock);

            var recentBlock = CreateSectionBlock("🕒 Последние поступления", BuildRecentActivityContent());
            Grid.SetRow(recentBlock, 2);
            grid.Children.Add(recentBlock);

            SectionContent.Content = grid;
        }

        private StackPanel BuildCategoriesContent()
        {
            var stack = new StackPanel();
            try
            {
                var categories = db.CATEGORY
                    .Select(c => new
                    {
                        Name = c.Category1,
                        Count = db.PRODUCTION_ASSETS.Count(a => a.id_category == c.ID_category)
                    })
                    .Where(x => x.Count > 0)
                    .OrderByDescending(x => x.Count)
                    .Take(5)
                    .ToList();

                var total = categories.Sum(x => x.Count);
                if (total == 0) total = 1;

                var colors = new[] { "#2196F3", "#4CAF50", "#FF9800", "#9C27B0", "#F44336" };

                for (int i = 0; i < categories.Count; i++)
                {
                    var item = categories[i];
                    var percent = (item.Count * 100.0 / total);

                    var row = new Grid();
                    row.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    row.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
                    headerPanel.Children.Add(new TextBlock
                    {
                        Text = item.Name ?? "Без категории",
                        FontWeight = FontWeights.SemiBold,
                        Width = 220
                    });
                    headerPanel.Children.Add(new TextBlock
                    {
                        Text = $"{item.Count} ед.",
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")),
                        Margin = new Thickness(10, 0, 0, 0)
                    });
                    Grid.SetRow(headerPanel, 0);
                    row.Children.Add(headerPanel);

                    var progressBar = new ProgressBar
                    {
                        Value = percent,
                        Maximum = 100,
                        Height = 20,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors[i % colors.Length])),
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0")),
                        Margin = new Thickness(0, 5, 0, 10)
                    };
                    Grid.SetRow(progressBar, 1);
                    row.Children.Add(progressBar);

                    stack.Children.Add(row);
                }

                if (categories.Count == 0)
                    stack.Children.Add(new TextBlock { Text = "Нет данных", Foreground = Brushes.Gray });
            }
            catch (Exception ex)
            {
                stack.Children.Add(new TextBlock { Text = $"Ошибка: {ex.Message}", Foreground = Brushes.Red });
            }
            return stack;
        }

        private StackPanel BuildShortageContent()
        {
            var stack = new StackPanel();
            try
            {
                // Активы, у которых quantity < min_quantity
                var shortage = db.PRODUCTION_ASSETS
                    .Include("CATEGORY")
                    .Include("WORKSHOPS")
                    .Where(a => a.quantity.HasValue && a.min_quantity.HasValue && a.quantity < a.min_quantity)
                    .OrderBy(a => (a.quantity - a.min_quantity))
                    .Take(5)
                    .ToList();

                foreach (var item in shortage)
                {
                    var row = new Border
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF3E0")),
                        CornerRadius = new CornerRadius(5),
                        Padding = new Thickness(10),
                        Margin = new Thickness(0, 0, 0, 5),
                        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800")),
                        BorderThickness = new Thickness(1)
                    };

                    var grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });

                    var nameText = new TextBlock
                    {
                        Text = item.name ?? $"Актив #{item.id}",
                        FontWeight = FontWeights.SemiBold,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(nameText, 0);
                    grid.Children.Add(nameText);

                    var quantityText = new TextBlock
                    {
                        Text = $"Есть: {item.quantity} / Мин: {item.min_quantity}",
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E65100")),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(quantityText, 1);
                    grid.Children.Add(quantityText);

                    var wsText = new TextBlock
                    {
                        Text = item.WORKSHOPS?.name ?? "—",
                        Foreground = Brushes.Gray,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(wsText, 2);
                    grid.Children.Add(wsText);

                    row.Child = grid;
                    stack.Children.Add(row);
                }

                if (shortage.Count == 0)
                {
                    stack.Children.Add(new TextBlock
                    {
                        Text = "✓ Все активы в норме",
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")),
                        FontWeight = FontWeights.SemiBold
                    });
                }
            }
            catch (Exception ex)
            {
                stack.Children.Add(new TextBlock { Text = $"Ошибка: {ex.Message}", Foreground = Brushes.Red });
            }
            return stack;
        }

        private StackPanel BuildRecentActivityContent()
        {
            var stack = new StackPanel();
            try
            {
                var recent = db.PRODUCTION_ASSETS
                    .Include("CATEGORY")
                    .Include("STATUSASSETS")
                    .OrderByDescending(a => a.created_at)
                    .Take(5)
                    .ToList();

                foreach (var item in recent)
                {
                    var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };

                    var dateStr = item.created_at.HasValue ? item.created_at.Value.ToString("dd.MM.yyyy") : "—";
                    row.Children.Add(new TextBlock
                    {
                        Text = $"• {dateStr} — ",
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#888888")),
                        Width = 120
                    });
                    row.Children.Add(new TextBlock { Text = item.name ?? "—" });
                    row.Children.Add(new TextBlock
                    {
                        Text = $"  [{item.STATUSASSETS?.Status ?? "—"}]",
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3")),
                        Margin = new Thickness(10, 0, 0, 0)
                    });

                    stack.Children.Add(row);
                }

                if (recent.Count == 0)
                    stack.Children.Add(new TextBlock { Text = "Нет данных", Foreground = Brushes.Gray });
            }
            catch (Exception ex)
            {
                stack.Children.Add(new TextBlock { Text = $"Ошибка: {ex.Message}", Foreground = Brushes.Red });
            }
            return stack;
        }
        #endregion

        #region Раздел: По категориям
        private void ShowCategoriesSection()
        {
            var content = new StackPanel();
            try
            {
                var categories = db.CATEGORY
                    .Select(c => new
                    {
                        Id = c.ID_category,
                        Name = c.Category1,
                        Count = db.PRODUCTION_ASSETS.Count(a => a.id_category == c.ID_category),
                        TotalValue = db.PRODUCTION_ASSETS.Where(a => a.id_category == c.ID_category).Sum(a => a.current_value) ?? 0,
                        TotalCost = db.PRODUCTION_ASSETS.Where(a => a.id_category == c.ID_category).Sum(a => a.purchase_cost) ?? 0
                    })
                    .Where(x => x.Count > 0)
                    .OrderByDescending(x => x.Count)
                    .ToList();

                foreach (var cat in categories)
                {
                    var card = new Border
                    {
                        Background = Brushes.White,
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(15),
                        Margin = new Thickness(0, 0, 0, 10),
                        Effect = new DropShadowEffect { BlurRadius = 4, ShadowDepth = 1, Opacity = 0.1 }
                    };

                    var grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var nameBlock = new TextBlock
                    {
                        Text = cat.Name ?? "Без категории",
                        FontSize = 16,
                        FontWeight = FontWeights.Bold,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(nameBlock, 0);
                    grid.Children.Add(nameBlock);

                    var total = categories.Sum(x => x.Count);
                    var percent = total > 0 ? (cat.Count * 100.0 / total) : 0;
                    var progressBar = new ProgressBar
                    {
                        Value = percent,
                        Maximum = 100,
                        Height = 25,
                        Margin = new Thickness(15, 0, 0 , 0),
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3")),
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"))
                    };
                    Grid.SetColumn(progressBar, 1);
                    grid.Children.Add(progressBar);

                    var statsPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                    statsPanel.Children.Add(new TextBlock { Text = $"{cat.Count} ед.", FontWeight = FontWeights.Bold, FontSize = 14 });
                    statsPanel.Children.Add(new TextBlock { Text = FormatMoney(cat.TotalValue), FontSize = 11, Foreground = Brushes.Gray });
                    Grid.SetColumn(statsPanel, 2);
                    grid.Children.Add(statsPanel);

                    card.Child = grid;
                    content.Children.Add(card);
                }

                if (categories.Count == 0)
                    content.Children.Add(new TextBlock { Text = "Нет данных по категориям", FontSize = 14, Foreground = Brushes.Gray });
            }
            catch (Exception ex)
            {
                content.Children.Add(new TextBlock { Text = $"Ошибка: {ex.Message}", Foreground = Brushes.Red });
            }
            SectionContent.Content = content;
        }
        #endregion

        #region Раздел: По статусам
        private void ShowStatusesSection()
        {
            var wrapPanel = new WrapPanel();
            try
            {
                var statuses = db.STATUSASSETS.ToList();
                var total = db.PRODUCTION_ASSETS.Count();

                var colors = new[] { "#4CAF50", "#2196F3", "#FF9800", "#F44336", "#9C27B0", "#607D8B", "#795548", "#00BCD4" };

                int i = 0;
                foreach (var status in statuses)
                {
                    var count = db.PRODUCTION_ASSETS.Count(a => a.status == status.ID_status);
                    var percent = total > 0 ? (count * 100.0 / total) : 0;
                    var color = colors[i % colors.Length];

                    var card = CreateStatusCard(status.Status ?? $"Статус #{status.ID_status}", count, percent, color);
                    wrapPanel.Children.Add(card);
                    i++;
                }

                if (statuses.Count == 0)
                    wrapPanel.Children.Add(new TextBlock { Text = "Нет статусов в системе", Foreground = Brushes.Gray });
            }
            catch (Exception ex)
            {
                wrapPanel.Children.Add(new TextBlock { Text = $"Ошибка: {ex.Message}", Foreground = Brushes.Red });
            }
            SectionContent.Content = wrapPanel;
        }

        private Border CreateStatusCard(string statusName, int count, double percent, string colorHex)
        {
            var card = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(20),
                Margin = new Thickness(5),
                Width = 250,
                Effect = new DropShadowEffect { BlurRadius = 5, ShadowDepth = 1, Opacity = 0.1 }
            };

            var stack = new StackPanel();

            var topBar = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex)),
                Height = 5,
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(0, 0, 0, 15)
            };
            stack.Children.Add(topBar);

            stack.Children.Add(new TextBlock
            {
                Text = statusName,
                FontSize = 14,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666"))
            });

            stack.Children.Add(new TextBlock
            {
                Text = count.ToString(),
                FontSize = 40,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex)),
                Margin = new Thickness(0, 10, 0, 0)
            });

            var progressBar = new ProgressBar
            {
                Value = percent,
                Maximum = 100,
                Height = 8,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex)),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0")),
                Margin = new Thickness(0, 10, 0, 0)
            };
            stack.Children.Add(progressBar);

            stack.Children.Add(new TextBlock
            {
                Text = $"{percent:F1}% от всех активов",
                FontSize = 12,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 5, 0, 0)
            });

            card.Child = stack;
            return card;
        }
        #endregion

        #region Раздел: По цехам
        private void ShowWorkshopsSection()
        {
            var content = new StackPanel();
            try
            {
                var workshops = db.WORKSHOPS
                    .Select(w => new
                    {
                        Id = w.id,
                        Name = w.name,
                        AssetCount = db.PRODUCTION_ASSETS.Count(a => a.workshop_id == w.id),
                        TotalValue = db.PRODUCTION_ASSETS.Where(a => a.workshop_id == w.id).Sum(a => a.current_value) ?? 0
                    })
                    .Where(x => x.AssetCount > 0)
                    .OrderByDescending(x => x.AssetCount)
                    .ToList();

                var total = workshops.Sum(x => x.AssetCount);
                if (total == 0) total = 1;

                var colors = new[] { "#2196F3", "#4CAF50", "#FF9800", "#9C27B0", "#F44336", "#607D8B", "#795548" };

                foreach (var ws in workshops.Select((w, i) => new { w, i }))
                {
                    var percent = (ws.w.AssetCount * 100.0 / total);
                    var color = colors[ws.i % colors.Length];

                    var row = new Border
                    {
                        Background = Brushes.White,
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(15),
                        Margin = new Thickness(0, 0, 0, 8),
                        Effect = new DropShadowEffect { BlurRadius = 3, ShadowDepth = 1, Opacity = 0.08 }
                    };

                    var grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });

                    var nameText = new TextBlock
                    {
                        Text = ws.w.Name ?? $"Цех #{ws.w.Id}",
                        FontWeight = FontWeights.SemiBold,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(nameText, 0);
                    grid.Children.Add(nameText);

                    var progressBar = new ProgressBar
                    {
                        Value = percent,
                        Maximum = 100,
                        Height = 20,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0")),
                        Margin = new Thickness(10, 0, 0, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(progressBar, 1);
                    grid.Children.Add(progressBar);

                    var statsPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
                    statsPanel.Children.Add(new TextBlock { Text = $"{ws.w.AssetCount} ед.", FontWeight = FontWeights.Bold });
                    statsPanel.Children.Add(new TextBlock { Text = FormatMoney(ws.w.TotalValue), FontSize = 11, Foreground = Brushes.Gray });
                    Grid.SetColumn(statsPanel, 2);
                    grid.Children.Add(statsPanel);

                    row.Child = grid;
                    content.Children.Add(row);
                }

                if (workshops.Count == 0)
                    content.Children.Add(new TextBlock { Text = "Нет данных по цехам", Foreground = Brushes.Gray });
            }
            catch (Exception ex)
            {
                content.Children.Add(new TextBlock { Text = $"Ошибка: {ex.Message}", Foreground = Brushes.Red });
            }
            SectionContent.Content = content;
        }
        #endregion

        #region Раздел: Финансы
        private void ShowFinanceSection()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var summaryBlock = CreateSectionBlock("💰 Финансовая сводка", BuildFinanceSummaryContent());
            Grid.SetRow(summaryBlock, 0);
            grid.Children.Add(summaryBlock);

            var suppliersBlock = CreateSectionBlock("🏢 Топ поставщиков", BuildSuppliersContent());
            Grid.SetRow(suppliersBlock, 1);
            grid.Children.Add(suppliersBlock);

            var typesBlock = CreateSectionBlock("📦 Затраты по типам активов", BuildAssetTypesContent());
            Grid.SetRow(typesBlock, 2);
            grid.Children.Add(typesBlock);

            SectionContent.Content = grid;
        }

        private StackPanel BuildFinanceSummaryContent()
        {
            var stack = new StackPanel();
            try
            {
                var query = db.PRODUCTION_ASSETS.AsQueryable();

                if (periodStart.HasValue)
                    query = query.Where(p => p.purchase_date >= periodStart.Value);
                if (periodEnd.HasValue)
                    query = query.Where(p => p.purchase_date <= periodEnd.Value);

                var totalItems = query.Count();
                var totalPurchaseCost = query.Sum(p => p.purchase_cost) ?? 0;
                var totalCurrentValue = query.Sum(p => p.current_value) ?? 0;
                var avgCost = totalItems > 0 ? totalPurchaseCost / totalItems : 0;

                var summary = new Grid();
                summary.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                summary.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                summary.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                summary.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var item1 = CreateSummaryItem("Всего позиций", totalItems.ToString(), "#2196F3");
                Grid.SetColumn(item1, 0);
                summary.Children.Add(item1);

                var item2 = CreateSummaryItem("Сумма закупок", FormatMoney(totalPurchaseCost), "#4CAF50");
                Grid.SetColumn(item2, 1);
                summary.Children.Add(item2);

                var item3 = CreateSummaryItem("Текущая стоимость", FormatMoney(totalCurrentValue), "#FF9800");
                Grid.SetColumn(item3, 2);
                summary.Children.Add(item3);

                var item4 = CreateSummaryItem("Средняя цена", FormatMoney(avgCost), "#9C27B0");
                Grid.SetColumn(item4, 3);
                summary.Children.Add(item4);

                stack.Children.Add(summary);

                // Изменение стоимости
                if (totalPurchaseCost > 0)
                {
                    var diff = totalCurrentValue - totalPurchaseCost;
                    var diffText = new TextBlock
                    {
                        Text = diff >= 0
                            ? $"📈 Прирост стоимости: +{FormatMoney(diff)}"
                            : $"📉 Снижение стоимости: {FormatMoney(diff)}",
                        FontSize = 14,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(diff >= 0 ? "#4CAF50" : "#F44336")),
                        Margin = new Thickness(0, 15, 0, 0)
                    };
                    stack.Children.Add(diffText);
                }
            }
            catch (Exception ex)
            {
                stack.Children.Add(new TextBlock { Text = $"Ошибка: {ex.Message}", Foreground = Brushes.Red });
            }
            return stack;
        }

        private StackPanel BuildSuppliersContent()
        {
            var stack = new StackPanel();
            try
            {
                var suppliers = db.SUPPLIERS
                    .Select(s => new
                    {
                        Name = s.name,
                        PurchasesCount = db.PRODUCTION_ASSETS.Count(p => p.supplier_id == s.id),
                        TotalAmount = db.PRODUCTION_ASSETS.Where(p => p.supplier_id == s.id).Sum(p => p.purchase_cost) ?? 0
                    })
                    .Where(x => x.PurchasesCount > 0)
                    .OrderByDescending(x => x.TotalAmount)
                    .Take(5)
                    .ToList();

                foreach (var sup in suppliers)
                {
                    var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
                    row.Children.Add(new TextBlock { Text = $"• {sup.Name ?? "—"}", Width = 220, FontWeight = FontWeights.SemiBold });
                    row.Children.Add(new TextBlock { Text = $"{sup.PurchasesCount} позиций", Width = 120, Foreground = Brushes.Gray });
                    row.Children.Add(new TextBlock
                    {
                        Text = FormatMoney(sup.TotalAmount),
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")),
                        FontWeight = FontWeights.Bold
                    });
                    stack.Children.Add(row);
                }

                if (suppliers.Count == 0)
                    stack.Children.Add(new TextBlock { Text = "Нет данных", Foreground = Brushes.Gray });
            }
            catch (Exception ex)
            {
                stack.Children.Add(new TextBlock { Text = $"Ошибка: {ex.Message}", Foreground = Brushes.Red });
            }
            return stack;
        }

        private StackPanel BuildAssetTypesContent()
        {
            var stack = new StackPanel();
            try
            {
                var types = db.ASSETTYPE
                    .Select(t => new
                    {
                        Name = t.AssetType1,
                        Count = db.PRODUCTION_ASSETS.Count(a => a.asset_type == t.ID_ASSETTYPE),
                        TotalCost = db.PRODUCTION_ASSETS.Where(a => a.asset_type == t.ID_ASSETTYPE).Sum(a => a.purchase_cost) ?? 0
                    })
                    .Where(x => x.Count > 0)
                    .OrderByDescending(x => x.TotalCost)
                    .ToList();

                var total = types.Sum(x => x.TotalCost);
                if (total == 0) total = 1;

                var colors = new[] { "#2196F3", "#4CAF50", "#FF9800", "#9C27B0", "#F44336" };

                for (int i = 0; i < types.Count; i++)
                {
                    var t = types[i];
                    var percent = (double)(t.TotalCost * 100 / total);

                    var row = new Grid();
                    row.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    row.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
                    headerPanel.Children.Add(new TextBlock { Text = t.Name ?? "Без типа", FontWeight = FontWeights.SemiBold, Width = 200 });
                    headerPanel.Children.Add(new TextBlock { Text = FormatMoney(t.TotalCost), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")), Margin = new Thickness(10, 0, 0, 0) });
                    headerPanel.Children.Add(new TextBlock { Text = $" ({t.Count} ед.)", Foreground = Brushes.Gray, Margin = new Thickness(5, 0, 0, 0) });
                    Grid.SetRow(headerPanel, 0);
                    row.Children.Add(headerPanel);

                    var progressBar = new ProgressBar
                    {
                        Value = percent,
                        Maximum = 100,
                        Height = 15,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors[i % colors.Length])),
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0")),
                        Margin = new Thickness(0, 5, 0, 10)
                    };
                    Grid.SetRow(progressBar, 1);
                    row.Children.Add(progressBar);

                    stack.Children.Add(row);
                }

                if (types.Count == 0)
                    stack.Children.Add(new TextBlock { Text = "Нет данных", Foreground = Brushes.Gray });
            }
            catch (Exception ex)
            {
                stack.Children.Add(new TextBlock { Text = $"Ошибка: {ex.Message}", Foreground = Brushes.Red });
            }
            return stack;
        }
        #endregion

        #region Раздел: Обслуживание
        private void ShowMaintenanceSection()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var statsBlock = CreateSectionBlock("🔧 Статистика обслуживания", BuildMaintenanceStatsContent());
            Grid.SetRow(statsBlock, 0);
            grid.Children.Add(statsBlock);

            var byTypeBlock = CreateSectionBlock("📋 По типам обслуживания", BuildMaintenanceByTypeContent());
            Grid.SetRow(byTypeBlock, 1);
            grid.Children.Add(byTypeBlock);

            var recentBlock = CreateSectionBlock("🕒 Последние работы", BuildRecentMaintenanceContent());
            Grid.SetRow(recentBlock, 2);
            grid.Children.Add(recentBlock);

            SectionContent.Content = grid;
        }

        private StackPanel BuildMaintenanceStatsContent()
        {
            var stack = new StackPanel();
            try
            {
                var totalMaintenance = db.MAINTENANCE.Count();
                var totalCost = db.MAINTENANCE.Sum(m => m.cost) ?? 0;
                var totalDowntime = db.MAINTENANCE.Sum(m => m.downtime_hours) ?? 0;
                var avgCost = totalMaintenance > 0 ? totalCost / totalMaintenance : 0;

                var summary = new Grid();
                summary.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                summary.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                summary.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                summary.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var item1 = CreateSummaryItem("Всего работ", totalMaintenance.ToString(), "#2196F3");
                Grid.SetColumn(item1, 0);
                summary.Children.Add(item1);

                var item2 = CreateSummaryItem("Общие затраты", FormatMoney(totalCost), "#F44336");
                Grid.SetColumn(item2, 1);
                summary.Children.Add(item2);

                var item3 = CreateSummaryItem("Время простоя", $"{totalDowntime} ч.", "#FF9800");
                Grid.SetColumn(item3, 2);
                summary.Children.Add(item3);

                var item4 = CreateSummaryItem("Средняя стоимость", FormatMoney(avgCost), "#9C27B0");
                Grid.SetColumn(item4, 3);
                summary.Children.Add(item4);

                stack.Children.Add(summary);
            }
            catch (Exception ex)
            {
                stack.Children.Add(new TextBlock { Text = $"Ошибка: {ex.Message}", Foreground = Brushes.Red });
            }
            return stack;
        }

        private StackPanel BuildMaintenanceByTypeContent()
        {
            var stack = new StackPanel();
            try
            {
                var byType = db.MAINTENANCE
                    .Where(m => m.maintenance_type != null)
                    .GroupBy(m => m.maintenance_type)
                    .Select(g => new
                    {
                        Type = g.Key,
                        Count = g.Count(),
                        TotalCost = g.Sum(m => m.cost) ?? 0
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                var total = byType.Sum(x => x.Count);
                if (total == 0) total = 1;

                var colors = new[] { "#2196F3", "#4CAF50", "#FF9800", "#9C27B0", "#F44336" };

                for (int i = 0; i < byType.Count; i++)
                {
                    var t = byType[i];
                    var percent = (t.Count * 100.0 / total);

                    var row = new Grid();
                    row.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    row.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
                    headerPanel.Children.Add(new TextBlock { Text = t.Type, FontWeight = FontWeights.SemiBold, Width = 200 });
                    headerPanel.Children.Add(new TextBlock { Text = $"{t.Count} работ", Width = 100, Foreground = Brushes.Gray });
                    headerPanel.Children.Add(new TextBlock { Text = FormatMoney(t.TotalCost), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336")) });
                    Grid.SetRow(headerPanel, 0);
                    row.Children.Add(headerPanel);

                    var progressBar = new ProgressBar
                    {
                        Value = percent,
                        Maximum = 100,
                        Height = 15,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors[i % colors.Length])),
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0")),
                        Margin = new Thickness(0, 5, 0, 10)
                    };
                    Grid.SetRow(progressBar, 1);
                    row.Children.Add(progressBar);

                    stack.Children.Add(row);
                }

                if (byType.Count == 0)
                    stack.Children.Add(new TextBlock { Text = "Нет данных", Foreground = Brushes.Gray });
            }
            catch (Exception ex)
            {
                stack.Children.Add(new TextBlock { Text = $"Ошибка: {ex.Message}", Foreground = Brushes.Red });
            }
            return stack;
        }

        private StackPanel BuildRecentMaintenanceContent()
        {
            var stack = new StackPanel();
            try
            {
                var recent = db.MAINTENANCE
                    .Include("EQUIPMENT")
                    .Include("MASTERS.USERS")
                    .Include("STATUSASSETS")
                    .OrderByDescending(m => m.maintenance_date)
                    .Take(5)
                    .ToList();

                foreach (var m in recent)
                {
                    var row = new Border
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5")),
                        CornerRadius = new CornerRadius(5),
                        Padding = new Thickness(10),
                        Margin = new Thickness(0, 0, 0, 5)
                    };

                    var grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });

                    var dateText = m.maintenance_date.HasValue ? m.maintenance_date.Value.ToString("dd.MM.yyyy") : "—";
                    var dateBlock = new TextBlock { Text = dateText, FontWeight = FontWeights.SemiBold };
                    Grid.SetColumn(dateBlock, 0);
                    grid.Children.Add(dateBlock);

                    var equipName = m.EQUIPMENT?.asset_id ?? $"Оборудование #{m.equipment_id}";
                    var equipBlock = new TextBlock { Text = equipName, TextTrimming = TextTrimming.CharacterEllipsis };
                    Grid.SetColumn(equipBlock, 1);
                    grid.Children.Add(equipBlock);

                    var masterName = m.MASTERS?.USERS?.full_name ?? "—";
                    var masterBlock = new TextBlock { Text = masterName, Foreground = Brushes.Gray };
                    Grid.SetColumn(masterBlock, 2);
                    grid.Children.Add(masterBlock);

                    var statusName = m.STATUSASSETS?.Status ?? "—";
                    var statusBlock = new TextBlock { Text = statusName, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3")) };
                    Grid.SetColumn(statusBlock, 3);
                    grid.Children.Add(statusBlock);

                    var costText = m.cost.HasValue ? FormatMoney(m.cost.Value) : "—";
                    var costBlock = new TextBlock { Text = costText, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")) };
                    Grid.SetColumn(costBlock, 4);
                    grid.Children.Add(costBlock);

                    foreach (UIElement child in grid.Children)
                    {
                        if (child is FrameworkElement fe)
                            fe.VerticalAlignment = VerticalAlignment.Center;
                    }

                    row.Child = grid;
                    stack.Children.Add(row);
                }

                if (recent.Count == 0)
                    stack.Children.Add(new TextBlock { Text = "Нет данных", Foreground = Brushes.Gray });
            }
            catch (Exception ex)
            {
                stack.Children.Add(new TextBlock { Text = $"Ошибка: {ex.Message}", Foreground = Brushes.Red });
            }
            return stack;
        }
        #endregion

        #region Вспомогательные методы
        private Border CreateSectionBlock(string title, UIElement content)
        {
            var border = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(20),
                Margin = new Thickness(0, 0, 0, 15),
                Effect = new DropShadowEffect { BlurRadius = 5, ShadowDepth = 1, Opacity = 0.1 }
            };

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 15),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"))
            });
            stack.Children.Add(content);
            border.Child = stack;

            return border;
        }

        private Border CreateSummaryItem(string label, string value, string color)
        {
            var border = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15),
                Margin = new Thickness(5),
                MinHeight = 90
            };

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock { Text = label, FontSize = 12, Foreground = Brushes.White, Opacity = 0.9 });
            stack.Children.Add(new TextBlock
            {
                Text = value,
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 8, 0, 0),
                TextWrapping = TextWrapping.Wrap
            });
            border.Child = stack;

            return border;
        }

        private string FormatMoney(decimal amount)
        {
            return $"{amount:N0} ₽";
        }

        private void PeriodFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            periodStart = DpPeriodStart.SelectedDate;
            periodEnd = DpPeriodEnd.SelectedDate;
        }

        private void BtnApplyPeriod_Click(object sender, RoutedEventArgs e)
        {
            periodStart = DpPeriodStart.SelectedDate;
            periodEnd = DpPeriodEnd.SelectedDate;
            LoadKpiData();
            ShowSection(currentSection);
        }
        #endregion
    }
}