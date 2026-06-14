using Microsoft.EntityFrameworkCore;
using TravelBlog.Data;
using TravelBlog.Models;
using TravelBlog.ViewModels;

namespace TravelBlog.Services
{
    public interface IUserService
    {
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByUsernameAsync(string username);
        Task<User> CreateAsync(RegisterViewModel model);
        Task<User> CreateAdminAsync(CreateAdminViewModel model);
        Task<bool> ValidatePasswordAsync(User user, string password);
        Task UpdateProfileAsync(int userId, EditProfileViewModel model);
        Task<List<UserSummaryViewModel>> GetAllUsersAsync(int page, int pageSize, string? search);
        Task<int> CountUsersAsync(string? search);
        Task ToggleUserActiveAsync(int userId);
    }

    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _db;
        private readonly IFileUploadService _fileService;

        public UserService(ApplicationDbContext db, IFileUploadService fileService)
        {
            _db = db;
            _fileService = fileService;
        }

        public async Task<User?> GetByIdAsync(int id)
            => await _db.Users.FindAsync(id);

        public async Task<User?> GetByEmailAsync(string email)
            => await _db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLower());

        public async Task<User?> GetByUsernameAsync(string username)
            => await _db.Users.FirstOrDefaultAsync(u => u.Username == username);

        public async Task<User> CreateAsync(RegisterViewModel model)
        {
            var user = new User
            {
                Username = model.Username.Trim(),
                Email = model.Email.ToLower().Trim(),
                FullName = model.FullName.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role = UserRole.User,
                CreatedAt = DateTime.UtcNow
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return user;
        }

        public async Task<User> CreateAdminAsync(CreateAdminViewModel model)
        {
            var user = new User
            {
                Username = model.Username.Trim(),
                Email = model.Email.ToLower().Trim(),
                FullName = model.FullName.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return user;
        }

        public Task<bool> ValidatePasswordAsync(User user, string password)
            => Task.FromResult(BCrypt.Net.BCrypt.Verify(password, user.PasswordHash));

        public async Task UpdateProfileAsync(int userId, EditProfileViewModel model)
        {
            var user = await _db.Users.FindAsync(userId)
                ?? throw new KeyNotFoundException("User not found");

            user.FullName = model.FullName.Trim();
            user.Bio = model.Bio?.Trim();
            user.UpdatedAt = DateTime.UtcNow;

            if (model.Avatar != null)
            {
                if (!string.IsNullOrEmpty(user.AvatarUrl))
                    _fileService.DeleteFile(user.AvatarUrl);
                user.AvatarUrl = await _fileService.UploadAsync(model.Avatar, "avatars");
            }

            await _db.SaveChangesAsync();
        }

        public async Task<List<UserSummaryViewModel>> GetAllUsersAsync(int page, int pageSize, string? search)
        {
            var query = _db.Users.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(u => u.Username.Contains(search) || u.Email.Contains(search) || (u.FullName != null && u.FullName.Contains(search)));

            return await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserSummaryViewModel
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    FullName = u.FullName,
                    Role = u.Role,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    BlogCount = u.Blogs.Count()
                }).ToListAsync();
        }

        public async Task<int> CountUsersAsync(string? search)
        {
            var query = _db.Users.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(u => u.Username.Contains(search) || u.Email.Contains(search) || (u.FullName != null && u.FullName.Contains(search)));
            return await query.CountAsync();
        }

        public async Task ToggleUserActiveAsync(int userId)
        {
            var user = await _db.Users.FindAsync(userId)
                ?? throw new KeyNotFoundException("User not found");
            user.IsActive = !user.IsActive;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }
}
