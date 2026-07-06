using Backend.DTOs.Requests.Setup;
using Backend.DTOs.Responses.Setup;
using Backend.Wrapper;

namespace Backend.Interfaces.Setup
{
    public interface ISetupService
    {
        /// <summary>
        /// Agency methods
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        Task<ResponseWrapper<Guid>> CreateAgencyAsync(CreateAgencyDto dto);

        Task<ResponseWrapper<PagedResponse<AgencyDto>>> GetAllAgenciesAsync(int page = 1, int pageSize = 10);

        Task<ResponseWrapper<bool>> UpdateAgencyAsync(Guid id, UpdateAgencyDto dto);

        Task<ResponseWrapper<bool>> DeleteAgencyAsync(Guid id);



        /// <summary>
        /// Branch  
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>

        Task<ResponseWrapper<Guid>> CreateBranchAsync(CreateBranchDto dto);

        Task<ResponseWrapper<PagedResponse<BranchDto>>> GetAllBranchesAsync(int page = 1, int pageSize = 10);

        Task<ResponseWrapper<bool>> UpdateBranchAsync(Guid id, UpdateBranchDto dto);

        Task<ResponseWrapper<bool>> DeleteBranchAsync(Guid id);







        /// <summary>
        /// Menu Mehods 
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>

        Task<ResponseWrapper<int>> CreateMenuAsync(CreateMenuDto dto);

        Task<ResponseWrapper<List<ModuleMenuDto>>> GetAllMenuAsync();
        Task<ResponseWrapper<bool>> UpdateMenuAsync(int id, UpdateMenuDto dto);

        Task<ResponseWrapper<bool>> DeleteMenuAsync(int  id);
        Task<ResponseWrapper<List<PermissionDto>>> GetAllPermissionsAsync();




        /// <summary>
        /// Module Mehods 
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>

        Task<ResponseWrapper<int>> CreateModuleAsync(CreateModuleDto dto);

        Task<ResponseWrapper<PagedResponse<ModuleDto>>> GetAllModuleAsync(int page = 1, int pageSize = 10);

        Task<ResponseWrapper<bool>> UpdateModuleAsync(int id, UpdateModuleDto dto);

        Task<ResponseWrapper<bool>> DeleteModuleAsync(int id);






        /// <summary>
        /// MenuPermission Mehods 
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>

        Task<ResponseWrapper<List<int>>> CreateMenuPermissionAsync(CreateMenuPermissionDto dto);

        Task<ResponseWrapper<PagedResponse<MenuPermissionGroupedDto>>> GetAllMenuPermissionAsync(int page = 1, int pageSize = 10);

        Task<ResponseWrapper<bool>> UpdateMenuPermissionAsync(int id, UpdateMenuPermissionDto dto);

        Task<ResponseWrapper<bool>> DeleteMenuPermissionAsync(int id);







        /// <summary>
        /// RolePermission Mehods 
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>

        Task<ResponseWrapper<List<int>>> CreateRolePermissionAsync(CreateRolePermissionDto dto);

        Task<ResponseWrapper<PagedResponse<RolePermissionDto>>> GetAllRolePermissionAsync(int page = 1, int pageSize = 10);
        Task<ResponseWrapper<bool>> DeleteRolePermissionAsync(int id);







        /// <summary>
        /// Permission Mehods 
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>

        Task<ResponseWrapper<int>> CreatePermissionAsync(CreatePermissionDto dto);

        Task<ResponseWrapper<PagedResponse<PermissionDto>>> GetAllPermissionAsync(int page = 1, int pageSize = 10);

        Task<ResponseWrapper<bool>> UpdatePermissionAsync(int id, UpdatePermissionDto dto);

        Task<ResponseWrapper<bool>> DeletePermissionAsync(int id);



        Task<ResponseWrapper<PagedResponse<MenuSummaryDto>>> GetAllSummaryMenuAsync(int page = 1, int pageSize = 10, string search = "");


        Task<ResponseWrapper<List<MenuSingleDto>>> GetMenuAsync();

    }







}
