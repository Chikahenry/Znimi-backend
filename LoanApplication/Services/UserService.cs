using LoanApplication.Data;
using LoanApplication.Domain.Enums;
using LoanApplication.Domain.Models;
using LoanApplication.Dtos;
using Microsoft.EntityFrameworkCore;
using LoginRequest = LoanApplication.Dtos.LoginRequest;

namespace LoanApplication.Services
{
    public interface IUserService
    {
        Task<User> Register(RegisterUserRequest request, int? createdByUserId = null);
        Task<LoginResponse> Login(LoginRequest request);
        Task<User> GetUserById(int userId);
        Task<List<User>> GetAllUsers(UserRole? role = null, bool? isActive = null);
        Task<User> UpdateUser(int userId, UpdateUserRequest request, int updatedByUserId);
        Task<bool> DeactivateUser(int userId, int deactivatedByUserId);
        Task<bool> ChangePassword(int userId, ChangePasswordRequest request);
        Task<List<UserActivityLog>> GetUserActivities(int userId, DateTime? fromDate = null, DateTime? toDate = null);
        Task LogActivity(int userId, string activityType, string description);
    }


    public class UserService : IUserService
    {
        private readonly LoanManagementDbContext _context;
        private readonly IAuditService _auditService;
        private readonly PasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;

        public UserService(
            LoanManagementDbContext context,
            IAuditService auditService,
            PasswordHasher passwordHasher,
            ITokenService tokenService)
        {
            _context = context;
            _auditService = auditService;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
        }

        public async Task<User> Register(RegisterUserRequest request, int? createdByUserId = null)
        {
            // Check if email already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

            if (existingUser != null)
                throw new Exception("User with this email already exists");

            // Validate role assignment
            if (createdByUserId.HasValue)
            {
                var creator = await _context.Users.FindAsync(createdByUserId.Value);
                if (creator == null || creator.Role != UserRole.Owner)
                    throw new Exception("Only owners can create new users");
            }

            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email.ToLower(),
                PasswordHash = _passwordHasher.HashPassword(request.Password),
                Role = request.Role,
                PhoneNumber = request.PhoneNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Log audit
            if (createdByUserId.HasValue)
            {
                await _auditService.LogAction(
                    createdByUserId.Value,
                    "CreateUser",
                    "User",
                    user.UserId,
                    "null",
                    new { user.Email, user.Role, user.FullName },
                    "New user registered"
                );
            }

            return user;
        }

        public async Task<LoginResponse> Login(LoginRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

            if (user == null)
                throw new Exception("Invalid email or password");

            if (!user.IsActive)
                throw new Exception("User account is deactivated");

            if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                // Log failed login attempt
                await LogActivity(user.UserId, "FailedLogin", $"Failed login attempt");
                throw new Exception("Invalid email or password");
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Generate token
            var token = _tokenService.GenerateToken(user);

            // Log successful login
            await LogActivity(user.UserId, "Login", $"User logged in successfully ");

            return new LoginResponse
            {
                Token = token,
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };
        }

        public async Task<User> GetUserById(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new Exception("User not found");

            return user;
        }

        public async Task<List<User>> GetAllUsers(UserRole? role = null, bool? isActive = null)
        {
            var query = _context.Users.AsQueryable();

            if (role.HasValue)
                query = query.Where(u => u.Role == role.Value);

            if (isActive.HasValue)
                query = query.Where(u => u.IsActive == isActive.Value);

            return await query
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }

        public async Task<User> UpdateUser(int userId, UpdateUserRequest request, int updatedByUserId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new Exception("User not found");

            var updater = await _context.Users.FindAsync(updatedByUserId);
            if (updater == null || updater.Role != UserRole.Owner)
                throw new Exception("Only owners can update users");

            var oldValues = new
            {
                user.FullName,
                user.Email,
                user.PhoneNumber,
                user.Role
            };

            user.FullName = request.FullName ?? user.FullName;
            user.Email = request.Email?.ToLower() ?? user.Email;
            user.PhoneNumber = request.PhoneNumber ?? user.PhoneNumber;

            if (request.Role.HasValue)
                user.Role = request.Role.Value;

            await _context.SaveChangesAsync();

            await _auditService.LogAction(
                updatedByUserId,
                "UpdateUser",
                "User",
                userId,
                oldValues,
                new { user.FullName, user.Email, user.PhoneNumber, user.Role },
                "User information updated"
            );

            return user;
        }

        public async Task<bool> DeactivateUser(int userId, int deactivatedByUserId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new Exception("User not found");

            var deactivator = await _context.Users.FindAsync(deactivatedByUserId);
            if (deactivator == null || deactivator.Role != UserRole.Owner)
                throw new Exception("Only owners can deactivate users");

            if (userId == deactivatedByUserId)
                throw new Exception("Cannot deactivate your own account");

            user.IsActive = false;
            await _context.SaveChangesAsync();

            await _auditService.LogAction(
                deactivatedByUserId,
                "DeactivateUser",
                "User",
                userId,
                new { IsActive = true },
                new { IsActive = false },
                "User account deactivated"
            );

            return true;
        }

        public async Task<bool> ChangePassword(int userId, ChangePasswordRequest request)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new Exception("User not found");

            if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
                throw new Exception("Current password is incorrect");

            user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            await _auditService.LogAction(
                userId,
                "ChangePassword",
                "User",
                userId,
                "null",
                "null",
                "User changed password"
            );

            return true;
        }

        public async Task<List<UserActivityLog>> GetUserActivities(int userId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.UserActivityLogs
                .Where(a => a.UserId == userId)
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(a => a.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(a => a.CreatedAt <= toDate.Value);

            return await query
                .OrderByDescending(a => a.CreatedAt)
                .Take(1000)
                .ToListAsync();
        }

        public async Task LogActivity(int userId, string activityType, string description)
        {
            var activity = new UserActivityLog
            {
                UserId = userId,
                ActivityType = activityType,
                Description = description,
                IpAddress = "",
                CreatedAt = DateTime.UtcNow
            };

            _context.UserActivityLogs.Add(activity);
            await _context.SaveChangesAsync();
        }
    }
}
