using EnterpriseAssets.Model.DataBase;

namespace EnterpriseAssets.Model
{
    public static class CurrentUser
    {
        public static USERS User { get; set; }

        public static int Id => User?.id ?? 0;

        public static string Username => User?.username ?? string.Empty;

        public static string FullName => User?.full_name ?? string.Empty;

        public static string RoleName => User?.ROLES?.name ?? string.Empty;

        public static bool IsAuthenticated => User != null;

        public static bool IsAdmin => RoleName == "Администратор";

        public static void SetUser(USERS user)
        {
            User = user;
        }

        public static void ClearUser()
        {
            User = null;
        }
    }
}