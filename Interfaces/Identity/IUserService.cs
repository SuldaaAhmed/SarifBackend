using Backend.DTOs.Requests.Identity;
using Backend.DTOs.Responses.Identity;
using Backend.DTOs.Responses.Setup;
using Backend.Wrapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;

namespace Backend.Interfaces.Identity
{
    public interface IUserService
    {
        // 🔹 New Methods
        Task<ResponseWrapper<string>> CreateUserAsync(CreateUserRequest request);
        Task<ResponseWrapper<string>> UpdateUserAsync(string userId, UpdateUserRequest request);
            Task<ResponseWrapper<string>> DeleteUserAsync(string userId);
        Task<ResponseWrapper<string>> ConfirmEmailAsync(string userId, string token);
   

        Task<ResponseWrapper<PagedResponse<UserResponses>>> GetAllUsersAsync(int page = 1, int pageSize = 10);





        Task<bool> RoleExistsAsync(string roleName);
        Task<bool> AddUserToRoleAsync(string userId, string roleName);
        Task<IList<string>> GetUserRolesAsync(string email);
        Task<bool> RemoveUserFromRoleAsync(string userId, string roleName);
        Task<List<RoleWithCreatorDto>> GetAllRolesAsync();
        Task<bool> CreateRoleAsync(string name, string? description);

        Task<bool> UpdateRoleAsync(string roleId, string name, string? description);


        Task<IdentityResult> AssignRoleToUserAsync(
         string userId,
         string roleName);









    }
}
