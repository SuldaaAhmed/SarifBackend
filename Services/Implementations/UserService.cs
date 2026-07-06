using Azure.Core;
using Backend.DTOs.Requests.Identity;
using Backend.DTOs.Responses.Identity;
using Backend.Interfaces;
using Backend.Interfaces.Identity;
using Backend.Models.Identity;
using Backend.Persistence;
using Backend.Utiliy;
using Backend.Wrapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Backend.Services.Implementations
{
    public class UserService  : CacheService, IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ICurrentUserService _currentUser;
        private readonly AppDbContext _context;
        // 1. Define your cache keys as constants to avoid typos and ensure consistency
        private const string ContactRequestsCacheKey = "AllContactRequests_Key";


        public UserService(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager, ICurrentUserService currentUserService , AppDbContext  context ,IMemoryCache cache) : base(cache)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _currentUser = currentUserService;
            _context = context;
        }

        private const string UsersCacheKey = "Users";
        private string CacheKey => $"{UsersCacheKey}_{_currentUser.UserId}";

        public async Task<ResponseWrapper<string>> CreateUserAsync(CreateUserRequest request)
        {
            return await ExecuteWriteAsync(async () =>
            {
                try
                {
                    string userNameOnly = request.Email.Split('@')[0];

                    var user = new ApplicationUser
                    {
                        Email = request.Email,
                        UserName = userNameOnly,
                        EmailConfirmed = true,
                        Address = request.Address,
                        PhoneNumber = request.Phone,
                        FirstName = request.FullName,
                        Gender = request.Gender,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = _currentUser.UserId,
                        RefreshToken = "RefreshToken"
                    };

                    var result = await _userManager.CreateAsync(user, request.Password);

                    if (!result.Succeeded)
                        throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

                    if (!string.IsNullOrWhiteSpace(request.Role))
                    {
                        if (!await _roleManager.RoleExistsAsync(request.Role))
                            throw new Exception($"Role '{request.Role}' does not exist");

                        var roleResult = await _userManager.AddToRoleAsync(user, request.Role);

                        if (!roleResult.Succeeded)
                            throw new Exception(string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                    }

                    return $"User '{request.Email}' created successfully";
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error creating user: {ex.Message}");
                }

            }, "User created successfully", "Error creating user");
        }

        // 1. Add 'string userId' as a parameter to the method
        public async Task<ResponseWrapper<string>> UpdateUserAsync(string userId, UpdateUserRequest request)
        {
            return await ExecuteWriteAsync(async () =>
            {
                try
                {
                    var user = await _userManager.FindByIdAsync(userId);

                    if (user == null)
                        throw new Exception("User not found.");

                    if (!string.IsNullOrWhiteSpace(request.Email) && user.Email != request.Email)
                    {
                        var existingUser = await _userManager.FindByEmailAsync(request.Email);
                        if (existingUser != null && existingUser.Id != user.Id)
                            throw new Exception("Email already exists.");

                        user.Email = request.Email;
                        user.UserName = request.Email.Split('@')[0].ToLower();
                    }

                    user.FirstName = request.FullName;
                    user.PhoneNumber = request.Phone;
                    user.Gender = request.Gender;
                    user.Address = request.Address;
                    user.UpdatedAt = DateTime.UtcNow;

                    var updateResult = await _userManager.UpdateAsync(user);

                    if (!updateResult.Succeeded)
                        throw new Exception(string.Join(", ", updateResult.Errors.Select(e => e.Description)));

                    if (!string.IsNullOrWhiteSpace(request.Role))
                    {
                        var currentRoles = await _userManager.GetRolesAsync(user);

                        if (!currentRoles.Contains(request.Role))
                        {
                            await _userManager.RemoveFromRolesAsync(user, currentRoles);

                            if (!await _roleManager.RoleExistsAsync(request.Role))
                                throw new Exception("Role does not exist.");

                            var addRoleResult = await _userManager.AddToRoleAsync(user, request.Role);

                            if (!addRoleResult.Succeeded)
                                throw new Exception("Failed to assign role.");
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(request.NewPassword))
                    {
                        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                        var passwordResult = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);

                        if (!passwordResult.Succeeded)
                            throw new Exception(string.Join(", ", passwordResult.Errors.Select(e => e.Description)));
                    }

                    return "User updated successfully";
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error updating user: {ex.Message}");
                }

            }, "User updated successfully", "Error updating user");
        }

        public async Task<ResponseWrapper<string>> DeleteUserAsync(string userId)
        {
            // Halkan looma baahna try-catch gudaha ah maadaama ExecuteWriteAsync uu leeyahay
            return await ExecuteWriteAsync(async () =>
            {
                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                    throw new Exception("User not found.");

                var result = await _userManager.DeleteAsync(user);

                // MUHIIM: Identity haddii ay fashilanto waa inaan gacanta ku 'throw' gareynaa
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new Exception(errors);
                }

                // Ka tirtir cache-ga liiska users-ka
                RemoveByPrefix(UsersCacheKey);

                return "User deleted successfully.";

            }, "User deleted successfully", "Error deleting user");
        }
        public async Task<ResponseWrapper<string>> ConfirmEmailAsync(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return await ResponseWrapper<string>.FailureAsync("User not found.");

            var result = await _userManager.ConfirmEmailAsync(user, token);

            return result.Succeeded
                ? await ResponseWrapper<string>.SuccessAsync("Email confirmed successfully.")
                : await ResponseWrapper<string>.FailureAsync("Failed to confirm email.");
        }


        public async Task<ResponseWrapper<PagedResponse<UserResponses>>> GetAllUsersAsync(int page = 1, int pageSize = 10)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;
            pageSize = Math.Min(pageSize, 100);

            var cacheKey = $"{CacheKey}_{page}_{pageSize}";

            return await ExecuteWithCacheAsync(
                cacheKey: cacheKey,
                action: async () =>
                {
                    var query = _userManager.Users
                        .AsNoTracking();

                    var totalCount = await query.CountAsync();

                    var users = await query
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync();

                    var userIds = users.Select(u => u.Id).ToList();

                    var userRoles = await (
                        from ur in _context.UserRoles
                        join r in _context.Roles on ur.RoleId equals r.Id
                        where userIds.Contains(ur.UserId)
                        select new { ur.UserId, r.Name }
                    ).ToListAsync();

                    var result = users.Select(user =>
                    {
                        var roles = userRoles
                            .Where(r => r.UserId == user.Id)
                            .Select(r => r.Name)
                            .ToList();

                        return new UserResponses
                        {
                            Id = user.Id,
                            UserName = user.UserName,
                            Email = user.Email,
                            Phone = user.PhoneNumber,
                            Isactive = user.IsActive,
                            Gender = user.Gender,
                            FullName = user.FirstName,
                            Address = user.Address,
                            Role = roles.Any() ? string.Join(", ", roles) : "No Role"
                        };
                    }).ToList();

                    return new PagedResponse<UserResponses>(
                        result,
                        page,
                        pageSize,
                        totalCount
                    );
                },
                successMessageFactory: result => $"{result.Data.Count} of {result.TotalRecords} users fetched",
                cacheMessage: "Users fetched from cache",
                errorMessage: "Error fetching users"
            );
        }


        public async Task<bool> RoleExistsAsync(string roleName)
    => await _roleManager.RoleExistsAsync(roleName);

        public async Task<bool> AddUserToRoleAsync(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var result = await _userManager.AddToRoleAsync(user, roleName);
            return result.Succeeded;
        }


        public async Task<bool> CreateRoleAsync(string name, string? description)
        {
            // 🔐 Auth check
            if (string.IsNullOrWhiteSpace(_currentUser.UserId))
                throw new UnauthorizedAccessException("User is not authenticated.");

            // 🧪 Validation
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Role name is required.");

            var normalizedName = name.Trim().ToUpperInvariant();

            // ❌ Prevent duplicate roles
            var exists = await _roleManager.Roles
                .AnyAsync(r => r.NormalizedName == normalizedName);

            if (exists)
                throw new InvalidOperationException("Role already exists.");

            var role = new ApplicationRole
            {
                Name = name.Trim(),
                NormalizedName = normalizedName,
                Description = description,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUser.UserId,
            };

            var result = await _roleManager.CreateAsync(role);

            if (!result.Succeeded)
            {
                var errors = string.Join(" | ",
                    result.Errors.Select(e => $"{e.Code}: {e.Description}")
                );

                throw new InvalidOperationException(errors);
            }

            return true;
        }



        public async Task<IList<string>> GetUserRolesAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
        }

        public async Task<bool> RemoveUserFromRoleAsync(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;
            var result = await _userManager.RemoveFromRoleAsync(user, roleName);
            return result.Succeeded;
        }


        public async Task<List<RoleWithCreatorDto>> GetAllRolesAsync()
        {
            return await (
                from role in _roleManager.Roles
                join user in _context.Users
                    on role.CreatedBy equals user.Id into users
                from createdUser in users.DefaultIfEmpty()
                select new RoleWithCreatorDto
                {
                    Id = role.Id,
                    Name = role.Name!,
                    Description = role.Description,
                    CreatedAt = role.CreatedAt,
                    CreatedByUserName = createdUser != null ? createdUser.UserName : null
                }
            ).ToListAsync();
        }


        public async Task<bool> UpdateRoleAsync(string roleId, string name, string? description)
        {
            if (string.IsNullOrWhiteSpace(_currentUser.UserId))
                throw new UnauthorizedAccessException();

            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
                throw new KeyNotFoundException("Role not found");

            var trimmedName = name.Trim();

            role.Name = trimmedName;
            role.NormalizedName = trimmedName.ToUpperInvariant(); // ✅ REQUIRED
            role.Description = description;

            // ✅ Audit fields
            role.UpdatedAt = DateTime.UtcNow;
            role.UpdatedBy = _currentUser.UserId;

            var result = await _roleManager.UpdateAsync(role);

            if (!result.Succeeded)
            {
                // Identity already gives correct message like:
                // "Role name 'Manager' is already taken."
                throw new InvalidOperationException(
                    string.Join(", ", result.Errors.Select(e => e.Description))
                );
            }

            return true;
        }









        public async Task<IdentityResult> AssignRoleToUserAsync(
            string userId,
            string roleName)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "UserIdRequired",
                    Description = "User ID is required."
                });
            }

            if (string.IsNullOrWhiteSpace(roleName))
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "RoleNameRequired",
                    Description = "Role name is required."
                });
            }

            roleName = roleName.Trim();

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "UserNotFound",
                    Description = "The specified user was not found."
                });
            }

            var roleExists = await _roleManager.RoleExistsAsync(roleName);

            if (!roleExists)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "RoleNotFound",
                    Description = $"The role '{roleName}' does not exist."
                });
            }

            // Get all roles currently assigned to the user
            var currentRoles = await _userManager.GetRolesAsync(user);

            // Check whether the requested role is already assigned
            var alreadyHasRequestedRole = currentRoles.Any(role =>
                string.Equals(
                    role,
                    roleName,
                    StringComparison.OrdinalIgnoreCase));

            // Remove all roles except the requested role
            var rolesToRemove = currentRoles
                .Where(role => !string.Equals(
                    role,
                    roleName,
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (rolesToRemove.Length > 0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(
                    user,
                    rolesToRemove);

                if (!removeResult.Succeeded)
                {
                    return removeResult;
                }
            }

            // The user already has the requested role
            if (alreadyHasRequestedRole)
            {
                return IdentityResult.Success;
            }

            // Assign the new role
            var addResult = await _userManager.AddToRoleAsync(user, roleName);

            // Restore previous roles if assigning the new role fails
            if (!addResult.Succeeded && rolesToRemove.Length > 0)
            {
                await _userManager.AddToRolesAsync(user, rolesToRemove);
            }

            return addResult;
        }



        public async Task<ResponseWrapper<UserResponses>> GetUserWithRolesAsync(
    string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return await ResponseWrapper<UserResponses>.FailureAsync(
                        "User ID is required.");
                }

                var user = await _userManager.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == userId);

                if (user == null)
                {
                    return await ResponseWrapper<UserResponses>.FailureAsync(
                        "User not found.");
                }

                var roles = await (
                    from userRole in _context.UserRoles
                    join role in _context.Roles
                        on userRole.RoleId equals role.Id
                    where userRole.UserId == user.Id
                    select role.Name
                )
                .Where(roleName => roleName != null)
                .ToListAsync();

                var response = new UserResponses
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    Phone = user.PhoneNumber,
                    Isactive = user.IsActive,
                    Gender = user.Gender,
                    FullName = user.FirstName,
                    Address = user.Address,

                    Role = roles.Any()
                        ? string.Join(", ", roles)
                        : "No Role"
                };

                return await ResponseWrapper<UserResponses>.SuccessAsync(
                    response,
                    "User with roles fetched successfully.");
            }
            catch (Exception ex)
            {
                return await ResponseWrapper<UserResponses>.FailureAsync(
                    $"Error fetching user: {ex.Message}");
            }
        }




        public async Task<ResponseWrapper<PagedResponse<UserResponses>>>
    GetAllUsersWithRoleAsync(int page = 1, int pageSize = 10)
        {
            if (page < 1)
                page = 1;

            if (pageSize < 1)
                pageSize = 10;

            pageSize = Math.Min(pageSize, 100);

            var cacheKey = $"{CacheKey}_WithRoles_{page}_{pageSize}";

            return await ExecuteWithCacheAsync(
                cacheKey: cacheKey,
                action: async () =>
                {
                    var query = _userManager.Users
                        .AsNoTracking()
                        .OrderByDescending(user => user.CreatedAt);

                    var totalCount = await query.CountAsync();

                    var users = await query
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync();

                    var userIds = users
                        .Select(user => user.Id)
                        .ToList();

                    var userRoles = await (
                        from userRole in _context.UserRoles
                        join role in _context.Roles
                            on userRole.RoleId equals role.Id
                        where userIds.Contains(userRole.UserId)
                        select new
                        {
                            userRole.UserId,
                            RoleName = role.Name
                        }
                    ).ToListAsync();

                    var rolesByUser = userRoles
                        .Where(item => !string.IsNullOrWhiteSpace(item.RoleName))
                        .GroupBy(item => item.UserId)
                        .ToDictionary(
                            group => group.Key,
                            group => group
                                .Select(item => item.RoleName!)
                                .ToList()
                        );

                    var result = users.Select(user =>
                    {
                        rolesByUser.TryGetValue(user.Id, out var roles);

                        return new UserResponses
                        {
                            Id = user.Id,
                            UserName = user.UserName,
                            Email = user.Email,
                            Phone = user.PhoneNumber,
                            Isactive = user.IsActive,
                            Gender = user.Gender,
                            FullName = user.FirstName,
                            Address = user.Address,
                            Role = roles != null && roles.Count > 0
                                ? string.Join(", ", roles)
                                : "No Role"
                        };
                    }).ToList();

                    return new PagedResponse<UserResponses>(
                        result,
                        page,
                        pageSize,
                        totalCount
                    );
                },
                successMessageFactory: result =>
                    $"{result.Data.Count} of {result.TotalRecords} users fetched with roles",
                cacheMessage: "Users with roles fetched from cache",
                errorMessage: "Error fetching users with roles"
            );
        }




    }



}
