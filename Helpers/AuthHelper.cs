using System.Security.Claims;
using TravelBlog.Models;

namespace TravelBlog.Helpers
{
    public static class AuthHelper
    {
        public const string SessionUserId = "UserId";
        public const string SessionUsername = "Username";
        public const string SessionUserRole = "UserRole";
        public const string SessionUserAvatar = "UserAvatar";
        public const string SessionFullName = "FullName";

        public static void SetUserSession(ISession session, User user)
        {
            session.SetInt32(SessionUserId, user.Id);
            session.SetString(SessionUsername, user.Username);
            session.SetString(SessionUserRole, user.Role.ToString());
            session.SetString(SessionFullName, user.FullName ?? user.Username);
            if (!string.IsNullOrEmpty(user.AvatarUrl))
                session.SetString(SessionUserAvatar, user.AvatarUrl);
        }

        public static void ClearSession(ISession session)
        {
            session.Remove(SessionUserId);
            session.Remove(SessionUsername);
            session.Remove(SessionUserRole);
            session.Remove(SessionFullName);
            session.Remove(SessionUserAvatar);
        }

        public static int? GetUserId(ISession session)
            => session.GetInt32(SessionUserId);

        public static string? GetUsername(ISession session)
            => session.GetString(SessionUsername);

        public static bool IsAdmin(ISession session)
            => session.GetString(SessionUserRole) == UserRole.Admin.ToString();

        public static bool IsLoggedIn(ISession session)
            => session.GetInt32(SessionUserId).HasValue;

        public static string GetFullName(ISession session)
            => session.GetString(SessionFullName) ?? "User";

        public static string? GetAvatar(ISession session)
            => session.GetString(SessionUserAvatar);
    }

    public static class PaginationHelper
    {
        public static (int skip, int take) GetPaging(int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);
            return ((page - 1) * pageSize, pageSize);
        }

        public static int TotalPages(int totalCount, int pageSize)
            => (int)Math.Ceiling((double)totalCount / Math.Max(1, pageSize));
    }
}
