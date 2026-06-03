using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using EnterpriseAssets.Model.DataBase;
using Microsoft.Win32;

namespace EnterpriseAssets.View
{
    public partial class CreateReportWindow : Window
    {
        private DB_AssetManage db = new DB_AssetManage();
        private int currentUserId;
        private string selectedFilePath;

        public CreateReportWindow(int userId)
        {
            InitializeComponent();
            currentUserId = userId;

            // Устанавливаем период по умолчанию (текущий месяц)
            DpPeriodStart.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            DpPeriodEnd.SelectedDate = DateTime.Now;
        }

        private void RbText_Checked(object sender, RoutedEventArgs e)
        {
            if (TextPanel != null)
            {
                TextPanel.Visibility = Visibility.Visible;
                FilePanel.Visibility = Visibility.Collapsed;
            }
        }

        private void RbFile_Checked(object sender, RoutedEventArgs e)
        {
            if (TextPanel != null)
            {
                TextPanel.Visibility = Visibility.Collapsed;
                FilePanel.Visibility = Visibility.Visible;
            }
        }

        private void BtnSelectFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Выберите файл для отчета",
                Filter = "Все поддерживаемые файлы|*.pdf;*.xlsx;*.xls;*.docx;*.doc;*.txt|PDF файлы|*.pdf|Excel файлы|*.xlsx;*.xls|Word файлы|*.docx;*.doc|Текстовые файлы|*.txt|Все файлы|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                selectedFilePath = openFileDialog.FileName;
                TxtSelectedFile.Text = Path.GetFileName(selectedFilePath);
                TxtSelectedFile.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void BtnCreateReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация
                if (!ValidateInput())
                {
                    return;
                }

                // Получаем мастера для текущего пользователя
                var currentMaster = db.MASTERS.FirstOrDefault(m => m.user_id == currentUserId);
                if (currentMaster == null)
                {
                    MessageBox.Show("Не удалось найти профиль сотрудника. Обратитесь к администратору.",
                                  "Ошибка",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Error);
                    return;
                }

                // Создаем новый отчет
                var newReport = new REPORTS
                {
                    report_type = (CmbReportType.SelectedItem as ComboBoxItem)?.Content.ToString(),
                    period_start = DpPeriodStart.SelectedDate,
                    period_end = DpPeriodEnd.SelectedDate,
                    generated_by = currentMaster.id,
                    created_at = DateTime.Now
                };

                // Если выбран текстовый режим
                if (RbText.IsChecked == true)
                {
                    newReport.report_data = TxtReportData.Text;
                    newReport.file_path = null;
                }
                // Если выбран режим файла
                else if (RbFile.IsChecked == true)
                {
                    // Создаем папку Reports если её нет
                    string reportsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
                    if (!Directory.Exists(reportsFolder))
                    {
                        Directory.CreateDirectory(reportsFolder);
                    }

                    // Генерируем уникальное имя файла
                    string fileExtension = Path.GetExtension(selectedFilePath);
                    string uniqueFileName = $"Report_{currentMaster.id}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
                    string destinationPath = Path.Combine(reportsFolder, uniqueFileName);

                    // Копируем файл
                    File.Copy(selectedFilePath, destinationPath, true);

                    newReport.file_path = destinationPath;
                    newReport.report_data = null;
                }

                // Сохраняем в базу
                db.REPORTS.Add(newReport);
                db.SaveChanges();

                MessageBox.Show("Отчет успешно создан!",
                              "Успех",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании отчета: {ex.Message}",
                              "Ошибка",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        private bool ValidateInput()
        {
            // Проверка типа отчета
            if (CmbReportType.SelectedItem == null)
            {
                MessageBox.Show("Выберите тип отчета",
                              "Внимание",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
                return false;
            }

            // Проверка периода
            if (!DpPeriodStart.SelectedDate.HasValue)
            {
                MessageBox.Show("Укажите дату начала периода",
                              "Внимание",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
                return false;
            }

            if (!DpPeriodEnd.SelectedDate.HasValue)
            {
                MessageBox.Show("Укажите дату окончания периода",
                              "Внимание",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
                return false;
            }

            if (DpPeriodStart.SelectedDate > DpPeriodEnd.SelectedDate)
            {
                MessageBox.Show("Дата начала периода не может быть позже даты окончания",
                              "Внимание",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
                return false;
            }

            // Проверка содержимого
            if (RbText.IsChecked == true)
            {
                if (string.IsNullOrWhiteSpace(TxtReportData.Text))
                {
                    MessageBox.Show("Введите содержимое отчета",
                                  "Внимание",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return false;
                }
            }
            else if (RbFile.IsChecked == true)
            {
                if (string.IsNullOrEmpty(selectedFilePath) || !File.Exists(selectedFilePath))
                {
                    MessageBox.Show("Выберите файл для отчета",
                                  "Внимание",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return false;
                }
            }

            return true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}