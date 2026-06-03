using System;
using System.Linq;
using EnterpriseAssets.Model.DataBase;

namespace EnterpriseAssets.Model
{
    /// <summary>
    /// Статический класс для хранения информации о текущей сессии пользователя
    /// </summary>
    public static class Session
    {
        private static User _currentUser;
        private static MASTERS _currentMaster;

        /// <summary>
        /// Текущий пользователь (DTO из ViewModel)
        /// </summary>
        public static User CurrentUser
        {
            get => _currentUser;
            set
            {
                _currentUser = value;
                LoadMaster();
            }
        }

        /// <summary>
        /// ID текущего пользователя
        /// </summary>
        public static int CurrentUserId => _currentUser?.Id ?? 0;

        /// <summary>
        /// Имя текущего пользователя
        /// </summary>
        public static string CurrentUserName => _currentUser?.FullName ?? "Неизвестно";

        /// <summary>
        /// Логин текущего пользователя
        /// </summary>
        public static string CurrentUserLogin => _currentUser?.Username ?? "";

        /// <summary>
        /// Роль текущего пользователя
        /// </summary>
        public static string CurrentUserRole => _currentUser?.RoleName ?? "Пользователь";

        /// <summary>
        /// Является ли текущий пользователь администратором или руководителем
        /// </summary>
        public static bool IsAdmin
        {
            get
            {
                if (_currentUser == null) return false;
                var role = _currentUser.RoleName?.Trim();
                return role == "Администратор" || role == "Руководитель";
            }
        }

        /// <summary>
        /// Текущий мастер (сотрудник), связанный с пользователем
        /// </summary>
        public static MASTERS CurrentMaster => _currentMaster;

        /// <summary>
        /// ID текущего мастера
        /// </summary>
        public static int? CurrentMasterId => _currentMaster?.id;

        /// <summary>
        /// Загрузка мастера для текущего пользователя
        /// </summary>
        private static void LoadMaster()
        {
            if (_currentUser == null)
            {
                _currentMaster = null;
                return;
            }

            try
            {
                using (var db = new DB_AssetManage())
                {
                    _currentMaster = db.MASTERS.FirstOrDefault(m => m.user_id == _currentUser.Id);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading master: {ex.Message}");
                _currentMaster = null;
            }
        }

        /// <summary>
        /// Проверка, может ли пользователь редактировать объект
        /// </summary>
        /// <param name="ownerMasterId">ID мастера, которому принадлежит объект</param>
        /// <returns>true, если пользователь админ или владелец</returns>
        public static bool CanEdit(int? ownerMasterId)
        {
            if (IsAdmin) return true;
            if (!ownerMasterId.HasValue) return false;
            return _currentMaster?.id == ownerMasterId.Value;
        }

        /// <summary>
        /// Проверка, является ли объект принадлежащим текущему пользователю
        /// </summary>
        public static bool IsOwner(int? ownerMasterId)
        {
            if (!ownerMasterId.HasValue) return false;
            return _currentMaster?.id == ownerMasterId.Value;
        }

        /// <summary>
        /// Очистка сессии (при выходе)
        /// </summary>
        public static void Clear()
        {
            _currentUser = null;
            _currentMaster = null;
        }
    }
}