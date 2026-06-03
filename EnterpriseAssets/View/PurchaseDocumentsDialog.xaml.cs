using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using EnterpriseAssets.Model.DataBase;

namespace EnterpriseAssets.View.Pages
{
    public partial class PurchaseDocumentsDialog : Window
    {
        private DB_AssetManage _context;
        private int _purchaseId;
        private int _currentUserId;
        private int? _currentMasterId;

        public PurchaseDocumentsDialog(int purchaseId, int currentUserId, DB_AssetManage context)
        {
            InitializeComponent();
            _context = context;
            _purchaseId = purchaseId;
            _currentUserId = currentUserId;
            LoadCurrentMaster();
            LoadDocuments();
        }

        private void LoadCurrentMaster()
        {
            var master = _context.MASTERS.FirstOrDefault(m => m.user_id == _currentUserId);
            _currentMasterId = master?.id;
        }

        private void LoadDocuments()
        {
            try
            {
                var purchase = _context.EQUIPMENT_PURCHASES.Find(_purchaseId);
                if (purchase == null) return;

                var reports = _context.REPORTS
                    .Include("MASTERS")
                    .Include("MASTERS.USERS")
                    .Where(r =>
                        r.report_data.Contains($"PURCHASE_ID:{_purchaseId}") ||
                        (r.file_path != null && r.file_path.Contains($"Purchase_{_purchaseId}")) ||
                        (r.period_start == purchase.order_date))
                    .ToList();

                var displayDocs = reports.Select(r => new
                {
                    Id = r.id,
                    FilePath = r.file_path,
                    FileName = !string.IsNullOrEmpty(r.file_path) ? Path.GetFileName(r.file_path) : "Без файла",
                    ReportType = r.report_type,
                    UploadedAt = r.created_at?.ToString("dd.MM.yyyy HH:mm") ?? "—",
                    UploadedBy = r.MASTERS?.USERS?.full_name ?? "—",
                    Icon = GetDocumentIcon(r.report_type),
                    TypeDisplay = GetDocumentTypeDisplay(r.report_type)
                }).ToList();

                DocumentsList.ItemsSource = displayDocs;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки документов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetDocumentIcon(string type)
        {
            return type switch
            {
                "Invoice" => "📄",
                "DeliveryNote" => "🚚",
                "Act" => "✅",
                "PurchaseReport" => "📊",
                _ => "📋"
            };
        }

        private string GetDocumentTypeDisplay(string type)
        {
            return type switch
            {
                "Invoice" => "Счёт",
                "DeliveryNote" => "Накладная",
                "Act" => "Акт приёмки",
                "PurchaseReport" => "Отчёт по закупке",
                _ => "Документ"
            };
        }

        private void BtnAttachDocument_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Все файлы|*.*|PDF файлы|*.pdf|Word|*.doc;*.docx|Excel|*.xls;*.xlsx|Изображения|*.jpg;*.jpeg;*.png",
                Title = "Выберите документ для загрузки"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);

                    string extension = Path.GetExtension(openFileDialog.FileName);
                    string newFileName = $"Purchase_{_purchaseId}_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
                    string destinationPath = Path.Combine(folderPath, newFileName);

                    File.Copy(openFileDialog.FileName, destinationPath, true);

                    var selectedItem = (ComboBoxItem)CmbDocumentType.SelectedItem;
                    string docType = selectedItem?.Tag?.ToString() ?? "Other";

                    var purchase = _context.EQUIPMENT_PURCHASES.Find(_purchaseId);

                    var report = new REPORTS
                    {
                        report_type = docType,
                        report_data = $"PURCHASE_ID:{_purchaseId}|NUMBER:{purchase?.purchase_number}",
                        period_start = purchase?.order_date,
                        period_end = purchase?.actual_delivery ?? purchase?.expected_delivery,
                        file_path = destinationPath,
                        generated_by = _currentMasterId,
                        created_at = DateTime.Now
                    };

                    _context.REPORTS.Add(report);
                    _context.SaveChanges();

                    MessageBox.Show("Документ успешно прикреплён!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    LoadDocuments();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки документа: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnOpenDocument_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string filePath)
            {
                if (File.Exists(filePath))
                {
                    Process.Start(filePath);
                }
                else
                {
                    MessageBox.Show("Файл не найден", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void BtnDeleteDocument_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int docId)
            {
                var result = MessageBox.Show("Удалить документ?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var doc = _context.REPORTS.Find(docId);
                        if (doc != null)
                        {
                            if (!string.IsNullOrEmpty(doc.file_path) && File.Exists(doc.file_path))
                                File.Delete(doc.file_path);

                            _context.REPORTS.Remove(doc);
                            _context.SaveChanges();

                            LoadDocuments();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}