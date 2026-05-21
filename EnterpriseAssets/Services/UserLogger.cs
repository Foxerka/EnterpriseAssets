using EnterpriseAssets.Model.DataBase;
using System;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Windows;

namespace EnterpriseAssets.Services
{
    public class UserLogger
    {
        private DB_AssetManage db = new DB_AssetManage();
        private int _currentUserId;

        public UserLogger(int currentUserId)
        {
            _currentUserId = currentUserId;
        }

        // Основной метод логирования
        public void Log(string actionType, string entityType = null, int? entityId = null,
                        string description = null, string oldValue = null, string newValue = null)
        {
            try
            {
                var log = new USER_LOGS
                {
                    user_id = _currentUserId,
                    action_type = actionType,
                    entity_type = entityType,
                    entity_id = entityId,
                    old_value = oldValue,
                    new_value = newValue,
                    description = description,
                    ip_address = GetLocalIPAddress(),
                    created_at = DateTime.Now
                };
                db.USER_LOGS.Add(log);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка логирования: {ex.Message}");
            }
        }

        // Методы-помощники для разных типов действий
        public void LogLogin() => Log("LOGIN", "USER", _currentUserId, "Вход в систему");

        public void LogLogout() => Log("LOGOUT", "USER", _currentUserId, "Выход из системы");

        public void LogCreate(string entityType, int entityId, string name)
            => Log("CREATE", entityType, entityId, $"Создан {entityType}: {name}");

        public void LogUpdate(string entityType, int entityId, string name, string changes)
            => Log("UPDATE", entityType, entityId, $"Изменен {entityType}: {name}", changes, null);

        public void LogDelete(string entityType, int entityId, string name)
            => Log("DELETE", entityType, entityId, $"Удален {entityType}: {name}");

        public void LogView(string entityType, int entityId, string name)
            => Log("VIEW", entityType, entityId, $"Просмотрен {entityType}: {name}");

        private string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                var ip = host.AddressList.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                return ip?.ToString() ?? "127.0.0.1";
            }
            catch
            {
                return "127.0.0.1";
            }
        }
    }
}