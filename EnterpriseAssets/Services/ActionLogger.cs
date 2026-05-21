using EnterpriseAssets.Model;
using EnterpriseAssets.Model.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnterpriseAssets.Services
{
    public class ActionLogger
    {
        private DB_AssetManage db = new DB_AssetManage();

        // Сохранение действия в БД (когда создадите таблицу)
        public void LogAction(int userId, string actionType, string description, string entityType = null, int? entityId = null)
        {
            try
            {
                // TODO: Когда создадите таблицу USER_ACTIONS в БД, раскомментируйте этот код
                // var log = new USER_ACTIONS
                // {
                //     user_id = userId,
                //     action_type = actionType,
                //     action_description = description,
                //     entity_type = entityType,
                //     entity_id = entityId,
                //     created_at = DateTime.Now
                // };
                // db.USER_ACTIONS.Add(log);
                // db.SaveChanges();

                System.Diagnostics.Debug.WriteLine($"[LOG] User {userId}: {actionType} - {description}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка логирования: {ex.Message}");
            }
        }

        // Получение последних действий из БД (когда создадите таблицу)
        //public List<Model.UserAction> GetRecentActions(int count = 10)
        //{
        //    try
        //    {
        //        // TODO: Когда создадите таблицу USER_ACTIONS, раскомментируйте этот код
        //        // return db.USER_ACTIONS
        //        //     .OrderByDescending(a => a.created_at)
        //        //     .Take(count)
        //        //     .Select(a => new Model.UserAction
        //        //     {
        //        //         Id = a.id,
        //        //         UserId = a.user_id,
        //        //         UserName = a.USERS.full_name,
        //        //         ActionType = a.action_type,
        //        //         ActionDescription = a.action_description,
        //        //         EntityType = a.entity_type,
        //        //         EntityId = a.entity_id,
        //        //         CreatedAt = a.created_at,
        //        //         Icon = GetIconForAction(a.action_type)
        //        //     }).ToList();

        //        // Пока возвращаем демо-данные
       
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Diagnostics.Debug.WriteLine($"Ошибка получения действий: {ex.Message}");
        //    }
        //}


        private string GetIconForAction(string actionType)
        {
            return actionType switch
            {
                "CREATE" => "➕",
                "UPDATE" => "✏️",
                "DELETE" => "🗑️",
                "LOGIN" => "🔑",
                "LOGOUT" => "🚪",
                _ => "📌"
            };
        }
    }
}