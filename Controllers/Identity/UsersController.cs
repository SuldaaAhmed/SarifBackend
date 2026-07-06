using Backend.DTOs.Requests.Identity;
using Backend.DTOs.Responses.Identity;
using Backend.Interfaces.Identity;
using Backend.Services.Implementations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers.Identity
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService  _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("Create-user")]
        public async Task<IActionResult> CreateUserAsync([FromBody] CreateUserRequest request)
        {
            var response = await _userService.CreateUserAsync(request);

            if (response.Success)
                return Ok(response);

            return BadRequest(response);
        }

        [HttpPut("update-user/{id}")]
        // Note the 'string id' parameter added here
        public async Task<IActionResult> UpdateUserAsync(string id, [FromBody] UpdateUserRequest request)
        {
            // Pass both the id (from URL) and the request (from Body)
            var response = await _userService.UpdateUserAsync(id, request);

            if (response.Success)
                return Ok(response);

            return BadRequest(response);
        }

        //[Authorize(Roles = "Administrator,Admin,Manager")]
        [HttpDelete("delete-user/{Id}")]
        public async Task<IActionResult> DeleteEmployee(string Id)
        {
            var response = await _userService.DeleteUserAsync(Id);

            if (response.Success)
                return Ok(response);

            return BadRequest(response);
        }

        //[Authorize(Roles = "Administrator,Admin,Manager")]
        [HttpGet("get-users")]
        public async Task<IActionResult> GetAllUsers(int page = 1, int pageSize = 10)
        {
            var response = await _userService.GetAllUsersAsync(page, pageSize);

            if (response.Data != null && response.Data.Data.Any())
                return Ok(response);

            return BadRequest(response);
        }





        [HttpPost("Create-role")]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
        {
            var response = await _userService.CreateRoleAsync(request.RoleName ,request.Description);

            if (!response)
                return Ok(response);

            return BadRequest(response);
        }



        [HttpGet("roles")] // Changed to HttpGet
        public async Task<IActionResult> GetRoles()
        {   
                var roles = await _userService.GetAllRolesAsync();
                return Ok(roles); 
            
         
        }

    








        [HttpPost("assign-role/{userId}")]
        public async Task<IActionResult> AssignRoleToUser(
    string userId,
    [FromBody] AssignRoleRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var result = await _userService.AssignRoleToUserAsync(
                userId,
                request.RoleName);

            if (result.Succeeded)
            {
                return Ok(new
                {
                    success = true,
                    message = $"Role '{request.RoleName}' assigned successfully.",
                    userId,
                    roleName = request.RoleName
                });
            }

            return BadRequest(new
            {
                success = false,
                message = "Could not assign the role.",
                errors = result.Errors.Select(error => new
                {
                    error.Code,
                    error.Description
                })
            });
        }

        [HttpGet("with-roles")]
        public async Task<IActionResult> GetAllUsersWithRoles(
      [FromQuery] int page = 1,
      [FromQuery] int pageSize = 10)
        {
            var response = await _userService
                .GetAllUsersWithRoleAsync(page, pageSize);

            if (response.Success)
                return Ok(response);

            return BadRequest(response);
        }


        [HttpGet("{userId}/with-roles")]
        public async Task<IActionResult> GetUserWithRoles(
    [FromRoute] string userId)
        {
            var response = await _userService
                .GetUserWithRolesAsync(userId);

            if (response.Success)
                return Ok(response);

            return NotFound(response);
        }






    }
}
