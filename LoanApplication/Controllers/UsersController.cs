using LoanApplication.Domain.Enums;
using LoanApplication.Domain.Models;
using LoanApplication.Dtos;
using LoanApplication.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using LoginRequest = LoanApplication.Dtos.LoginRequest;

namespace LoanApplication.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("register")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> Register([FromBody] RegisterUserRequest request)
        {
            try
            {
                var createdByUserId = GetCurrentUserId();
                var user = await _userService.Register(request, createdByUserId??2);

                // Log activity
                await _userService.LogActivity(
                    user.UserId,
                    "CreateUser",
                    $"Created new user: {user.Email} with role {user.Role}"
                );

                return Ok(new
                {
                    success = true,
                    message = "User registered successfully",
                    userId = user.UserId,
                    email = user.Email,
                    role = user.Role.ToString()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var response = await _userService.Login(request);

                return Ok(new
                {
                    success = true,
                    message = "Login successful",
                    data = response
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> GetAllUsers([FromQuery] string role = null, [FromQuery] bool? isActive = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                UserRole? userRole = null;

                if (!string.IsNullOrEmpty(role) && Enum.TryParse<UserRole>(role, out var parsedRole))
                    userRole = parsedRole;

                var users = await _userService.GetAllUsers(userRole, isActive);

                // Log activity
                await _userService.LogActivity(
                    userId.Value,
                    "ViewUsers",
                    $"Viewed all users list (filtered by: role={role}, active={isActive})"
                    );

                return Ok(new
                {
                    success = true,
                    data = users.Select(u => new
                    {
                        u.UserId,
                        u.FullName,
                        u.Email,
                        u.PhoneNumber,
                        Role = u.Role.ToString(),
                        u.IsActive,
                        u.CreatedAt,
                        u.LastLoginAt
                    })
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetUserById(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                // Users can only view their own profile unless they're Owner
                if (currentUserId != id && currentUserRole != "Owner")
                    return Forbid();

                var user = await _userService.GetUserById(id);

                // Log activity
                await _userService.LogActivity(
                    currentUserId.Value,
                    "ViewUserProfile",
                    $"Viewed user profile: {user.Email}"
                );

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        user.UserId,
                        user.FullName,
                        user.Email,
                        user.PhoneNumber,
                        Role = user.Role.ToString(),
                        user.IsActive,
                        user.CreatedAt,
                        user.LastLoginAt
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var user = await _userService.UpdateUser(id, request, currentUserId.Value);

                // Log activity
                await _userService.LogActivity(
                    currentUserId.Value,
                    "UpdateUser",
                    $"Updated user: {user.Email}"
                );

                return Ok(new
                {
                    success = true,
                    message = "User updated successfully",
                    data = new
                    {
                        user.UserId,
                        user.FullName,
                        user.Email,
                        user.PhoneNumber,
                        Role = user.Role.ToString()
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("{id}/deactivate")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                await _userService.DeactivateUser(id, currentUserId.Value);

                // Log activity
                await _userService.LogActivity(
                    currentUserId.Value,
                    "DeactivateUser",
                    $"Deactivated user ID: {id}"
                );

                return Ok(new
                {
                    success = true,
                    message = "User deactivated successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _userService.ChangePassword(userId.Value, request);

                // Log activity
                await _userService.LogActivity(
                    userId.Value,
                    "ChangePassword",
                    "Changed password"
                );

                return Ok(new
                {
                    success = true,
                    message = "Password changed successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("{id}/activities")]
        [Authorize]
        public async Task<IActionResult> GetUserActivities(
            int id,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                // Users can only view their own activities unless they're Owner
                if (currentUserId != id && currentUserRole != "Owner")
                    return Forbid();

                var activities = await _userService.GetUserActivities(id, fromDate, toDate);

                // Log activity
                await _userService.LogActivity(
                    currentUserId.Value,
                    "ViewUserActivities",
                    $"Viewed activity log for user ID: {id}"
                );

                return Ok(new
                {
                    success = true,
                    data = activities.Select(a => new UserActivityResponse
                    {
                        ActivityId = a.ActivityId,
                        ActivityType = a.ActivityType,
                        Description = a.Description,
                        IpAddress = a.IpAddress,
                        CreatedAt = a.CreatedAt
                    })
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userId = GetCurrentUserId();

                // Log activity
                await _userService.LogActivity(
                    userId.Value,
                    "Logout",
                    "User logged out"
                );

                return Ok(new
                {
                    success = true,
                    message = "Logged out successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // Helper methods
        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : null;
        }

        private string GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value;
        }

        private string GEtIPAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }
    }
}
