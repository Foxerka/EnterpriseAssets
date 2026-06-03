using System;
using System.Windows.Media;

namespace EnterpriseAssets.Model
{
    public class WorkActViewModel
    {
        public int Id { get; set; }
        public string ActNumber { get; set; }
        public DateTime? ActDate { get; set; }
        public string Description { get; set; }
        public string EquipmentDisplay { get; set; }
        public string AssetDisplay { get; set; }
        public string MasterName { get; set; }
        public int? MasterId { get; set; }
        public int? StatusId { get; set; }
        public string StatusName { get; set; }
        public decimal? Quantity { get; set; }
        public int? EquipmentId { get; set; }
        public bool HasCompletionAct { get; set; }
        public DateTime? CompletionDate { get; set; }
        public int MaterialsCount { get; set; }

        // Права доступа
        public bool IsMyAct { get; set; }           // Это мой акт?
        public bool CanEdit { get; set; }           // Могу ли редактировать?
        public bool CanChangeStatus { get; set; }   // Могу ли менять статус?

        // Вычисляемые свойства
        public string Icon => GetStatusIcon(StatusName);
        public Brush StatusColor => new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString(GetStatusColorHex(StatusName)));
        public Brush StatusTextColor => new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString(GetStatusTextColorHex(StatusName)));
        public Brush CompletionBadgeColor => HasCompletionAct
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BDC3C7"));
        public string CompletionBadgeText => HasCompletionAct ? "✓ Завершён" : "○ В работе";
        public string CompletionDateText => CompletionDate.HasValue
            ? $"Завершён: {CompletionDate.Value:dd.MM.yyyy}"
            : "Не завершён";
        public string QuantityText => Quantity.HasValue ? $"{Quantity} ед." : "—";
        public string MaterialsText => MaterialsCount > 0 ? $"Материалов: {MaterialsCount}" : "Без материалов";
        public string AccessBadgeText => IsMyAct ? "👤 Мой акт" : "🔒 Чужой";
        public Brush AccessBadgeColor => IsMyAct
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#95A5A6"));

        private string GetStatusIcon(string status)
        {
            return status switch
            {
                "Черновик" => "📝",
                "В работе" => "🔧",
                "На проверке" => "👀",
                "Завершён" or "Завершен" => "✅",
                "Отменён" or "Отменен" => "❌",
                _ => "📄"
            };
        }

        private string GetStatusColorHex(string status)
        {
            return status switch
            {
                "Черновик" => "#95A5A6",
                "В работе" => "#F39C12",
                "На проверке" => "#3498DB",
                "Завершён" or "Завершен" => "#27AE60",
                "Отменён" or "Отменен" => "#E74C3C",
                _ => "#95A5A6"
            };
        }

        private string GetStatusTextColorHex(string status)
        {
            return status switch
            {
                "Завершён" or "Завершен" => "#27AE60",
                "Отменён" or "Отменен" => "#E74C3C",
                "В работе" => "#F39C12",
                "На проверке" => "#3498DB",
                _ => "#7F8C8D"
            };
        }
    }
}