using Backend.DTOs.Requests.Setup;
using Backend.Interfaces.Setup;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers.Setup
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SetupController : ControllerBase
    {
        private readonly ISetupService _setupService;

        public SetupController(ISetupService setupService)
        {
            _setupService = setupService;
        }

        // ============================
        // AGENCY
        // ============================

        [HttpPost("agency")]
        public async Task<IActionResult> CreateAgency([FromBody] CreateAgencyDto dto)
        {
            var result = await _setupService.CreateAgencyAsync(dto);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("agency")]
        public async Task<IActionResult> GetAgencies(int page = 1, int pageSize = 10)
        {
            var result = await _setupService.GetAllAgenciesAsync(page, pageSize);
            return Ok(result);
        }

        [HttpPut("agency/{id}")]
        public async Task<IActionResult> UpdateAgency(Guid id, [FromBody] UpdateAgencyDto dto)
        {
            var result = await _setupService.UpdateAgencyAsync(id, dto);
            return Ok(result);
        }

        [HttpDelete("agency/{id}")]
        public async Task<IActionResult> DeleteAgency(Guid id)
        {
            var result = await _setupService.DeleteAgencyAsync(id);
            return Ok(result);
        }

        // ============================
        // BRANCH
        // ============================

        [HttpPost("branch")]
        public async Task<IActionResult> CreateBranch([FromBody] CreateBranchDto dto)
        {
            var result = await _setupService.CreateBranchAsync(dto);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("branch")]
        public async Task<IActionResult> GetBranches(int page = 1, int pageSize = 10)
        {
            var result = await _setupService.GetAllBranchesAsync(page, pageSize);
            return Ok(result);
        }

        [HttpPut("branch/{id}")]
        public async Task<IActionResult> UpdateBranch(Guid id, [FromBody] UpdateBranchDto dto)
        {
            var result = await _setupService.UpdateBranchAsync(id, dto);
            return Ok(result);
        }

        [HttpDelete("branch/{id}")]
        public async Task<IActionResult> DeleteBranch(Guid id)
        {
            var result = await _setupService.DeleteBranchAsync(id);
            return Ok(result);
        }

        // ============================
        // MENU
        // ============================

        [HttpPost("menu")]
        public async Task<IActionResult> CreateMenu([FromBody] CreateMenuDto dto)
        {
            var result = await _setupService.CreateMenuAsync(dto);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("menu")]
        public async Task<IActionResult> GetMenus(int page = 1, int pageSize = 10)
        {
            var result = await _setupService.GetAllMenuAsync();
            return Ok(result);
        }



     [HttpGet("menu-summary")]
public async Task<IActionResult> GetSummaryMenus([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string search = "")
{
    // Pass 'page', 'pageSize', and 'search' to the service
    var result = await _setupService.GetAllSummaryMenuAsync(page, pageSize, search);
    
    return Ok(result);
}

        [HttpPut("menu/{id}")]
        public async Task<IActionResult> UpdateMenu(int id, [FromBody] UpdateMenuDto dto)
        {
            var result = await _setupService.UpdateMenuAsync(id, dto);
            return Ok(result);
        }

        [HttpDelete("menu/{id}")]
        public async Task<IActionResult> DeleteMenu(int id)
        {
            var result = await _setupService.DeleteMenuAsync(id);
            return Ok(result);
        }

        // ============================
        // MODULE
        // ============================

        [HttpPost("module")]
        public async Task<IActionResult> CreateModule([FromBody] CreateModuleDto dto)
        {
            var result = await _setupService.CreateModuleAsync(dto);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("module")]
        public async Task<IActionResult> GetModules(int page = 1, int pageSize = 10)
        {
            var result = await _setupService.GetAllModuleAsync(page, pageSize);
            return Ok(result);
        }

        [HttpPut("module/{id}")]
        public async Task<IActionResult> UpdateModule(int id, [FromBody] UpdateModuleDto dto)
        {
            var result = await _setupService.UpdateModuleAsync(id, dto);
            return Ok(result);
        }

        [HttpDelete("module/{id}")]
        public async Task<IActionResult> DeleteModule(int id)
        {
            var result = await _setupService.DeleteModuleAsync(id);
            return Ok(result);
        }

        // ============================
        // MENU PERMISSION
        // ============================

        [HttpPost("menu-permission")]
        public async Task<IActionResult> CreateMenuPermission([FromBody] CreateMenuPermissionDto dto)
        {
            var result = await _setupService.CreateMenuPermissionAsync(dto);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("menu-permission")]
        public async Task<IActionResult> GetMenuPermissions(int page = 1, int pageSize = 10)
        {
            var result = await _setupService.GetAllMenuPermissionAsync(page, pageSize);
            return Ok(result);
        }

        [HttpGet("menus-summary")]
        public async Task<IActionResult> GetMenuSummary()
        {
            var result = await _setupService.GetMenuAsync();
            return Ok(result);
        }

        [HttpPut("menu-permission/{id}")]
        public async Task<IActionResult> UpdateMenuPermission(int id, [FromBody] UpdateMenuPermissionDto dto)
        {
            var result = await _setupService.UpdateMenuPermissionAsync(id, dto);
            return Ok(result);
        }

        [HttpDelete("menu-permission/{id}")]
        public async Task<IActionResult> DeleteMenuPermission(int id)
        {
            var result = await _setupService.DeleteMenuPermissionAsync(id);
            return Ok(result);
        }

        // ============================
        // ROLE PERMISSION
        // ============================

        [HttpPost("role-permission")]
        public async Task<IActionResult> CreateRolePermission([FromBody] CreateRolePermissionDto dto)
        {
            var result = await _setupService.CreateRolePermissionAsync(dto);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("role-permission")]
        public async Task<IActionResult> GetRolePermissions(int page = 1, int pageSize = 10)
        {
            var result = await _setupService.GetAllRolePermissionAsync(page, pageSize);
            return Ok(result);
        }

        [HttpDelete("role-permission/{id}")]
        public async Task<IActionResult> DeleteRolePermission(int id)
        {
            var result = await _setupService.DeleteRolePermissionAsync(id);
            return Ok(result);
        }

        // ============================
        // PERMISSION
        // ============================

        [HttpPost("permission")]
        public async Task<IActionResult> CreatePermission([FromBody] CreatePermissionDto dto)
        {
            var result = await _setupService.CreatePermissionAsync(dto);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("permission")]
        public async Task<IActionResult> GetPermissions(int page = 1, int pageSize = 10)
        {
            var result = await _setupService.GetAllPermissionAsync(page, pageSize);
            return Ok(result);
        }

        [HttpGet("permissions")]
        public async Task<IActionResult> GetAllPermissionsAsync()
        {
            var result = await _setupService.GetAllPermissionsAsync();
            return Ok(result);
        }

        [HttpPut("permission/{id}")]
        public async Task<IActionResult> UpdatePermission(int id, [FromBody] UpdatePermissionDto dto)
        {
            var result = await _setupService.UpdatePermissionAsync(id, dto);
            return Ok(result);
        }

        [HttpDelete("permission/{id}")]
        public async Task<IActionResult> DeletePermission(int id)
        {
            var result = await _setupService.DeletePermissionAsync(id);
            return Ok(result);
        }
    }
}