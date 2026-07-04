using AutoMapper;
using AutoMapper.QueryableExtensions;
using Backend.DTOs.Requests.Setup;
using Backend.DTOs.Responses.Setup;
using Backend.Interfaces;
using Backend.Interfaces.Setup;
using Backend.Models.Setup;
using Backend.Models.Subscription;
using Backend.Persistence;
using Backend.Utiliy;
using Backend.Wrapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

public class SetupService : CacheService, ISetupService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private const string AgencyCacheKey = "AgencyCache";
    private const string BranchCacheKey = "BranchCache";
    private const string MenuCacheKey = "MenuCache";
    private const string ModuleCacheKey = "ModuleCache";
    private const string MenuPermisstionCacheKey = "MenuPermissionCache";
    private const string RolePermisstionCacheKey = "RoleMenuPermissionCache";
    private const string PermissionCacheKey = "PermissionCache";

    public SetupService(
        AppDbContext context,
        IMapper mapper,
        IMemoryCache cache,
        ICurrentUserService currentUser ,
        IHttpContextAccessor httpContextAccessor
    ) : base(cache)
    {
        _context = context;
        _mapper = mapper;
        _currentUser = currentUser;
        _httpContextAccessor = httpContextAccessor;
    }

    // ================================
    // CREATE
    // ================================
    public async Task<ResponseWrapper<Guid>> CreateAgencyAsync(CreateAgencyDto dto)
    {
        if (string.IsNullOrEmpty(_currentUser.UserId))
        {
            return await ResponseWrapper<Guid>.FailureAsync("Unauthorized", "User not authenticated", 401);
        }

        return await ExecuteWriteAsync(async () =>
        {
            try
            {
                var entity = _mapper.Map<Agency>(dto);

                entity.Id = Guid.NewGuid();
                entity.UserId = _currentUser.UserId;
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;

                entity.Code = await GenerateAgencyCodeAsync();

                _context.Agencies.Add(entity);
                await _context.SaveChangesAsync();

                RemoveByPrefix(AgencyCacheKey);

                return entity.Id;
            }
            catch (DbUpdateException ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                throw new Exception($"Database error while creating agency: {msg}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Unexpected error while creating agency: {ex.Message}");
            }

        }, "Agency created successfully", "Error creating agency");
    }

    // ================================
    // GET ALL
    // ================================
    public async Task<ResponseWrapper<PagedResponse<AgencyDto>>> GetAllAgenciesAsync(int page = 1, int pageSize = 10)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;
        pageSize = Math.Min(pageSize, 100);

        var agencyId = _currentUser.AgencyId;
        var isAdmin = _currentUser.IsInRole("Administrator"); // ✅ muhiim

        return await ExecuteWithCacheAsync(
            cacheKey: $"{AgencyCacheKey}_{_currentUser.UserId}_{page}_{pageSize}",

            action: async () =>
            {
                var query = _context.Agencies.AsNoTracking();

                // ✅ USER caadi → filter by agency
                if (!isAdmin)
                {
                    if (agencyId == Guid.Empty)
                    {
                        return new PagedResponse<AgencyDto>(new List<AgencyDto>(), page, pageSize, 0);
                    }

                    query = query.Where(x => x.Id == agencyId);
                }

                // ✅ ADMIN → no filter (ar everything)

                var totalRecords = await query.CountAsync();

                var mapped = await query
                    .OrderByDescending(x => x.CreatedAt)
                    .ThenBy(x => x.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ProjectTo<AgencyDto>(_mapper.ConfigurationProvider)
                    .ToListAsync();

                return new PagedResponse<AgencyDto>(mapped, page, pageSize, totalRecords);
            },
            successMessageFactory: result => $"{result.Data.Count} of {result.TotalRecords} agencies fetched",
            cacheMessage: "Agencies fetched from cache",
            errorMessage: "Error fetching agencies"
        );
    }    // ================================
    public async Task<ResponseWrapper<bool>> UpdateAgencyAsync(Guid id, UpdateAgencyDto dto)
    {
        return await ExecuteWriteAsync(async () =>
        {
            try
            {
                var entity = await _context.Agencies.FindAsync(id);

                if (entity == null)
                    throw new Exception("Agency not found");

                _mapper.Map(dto, entity);

                entity.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                RemoveByPrefix(AgencyCacheKey);


                return true;
            }
            catch (DbUpdateException ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                throw new Exception($"Database error while updating agency: {msg}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating agency: {ex.Message}");
            }

        }, "Agency updated successfully", "Error updating agency");
    }

    // ================================
    // DELETE
    // ================================
    public async Task<ResponseWrapper<bool>> DeleteAgencyAsync(Guid id)
    {
        return await ExecuteWriteAsync(async () =>
        {
            try
            {
                var entity = await _context.Agencies.FindAsync(id);

                if (entity == null)
                    throw new Exception("Agency not found");

                _context.Agencies.Remove(entity);
                await _context.SaveChangesAsync();

                RemoveByPrefix(AgencyCacheKey);


                return true;
            }
            catch (DbUpdateException ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                throw new Exception($"Database error while deleting agency: {msg}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting agency: {ex.Message}");
            }

        }, "Agency deleted successfully", "Error deleting agency");
    }

    // ================================
    // CODE GENERATOR
    // ================================
    private async Task<string> GenerateAgencyCodeAsync()
    {
        try
        {
            var year = DateTime.UtcNow.Year;

            var count = await _context.Agencies
                .CountAsync(x => x.CreatedAt.Year == year);

            var nextNumber = count + 1;

            return $"A{year}{nextNumber:D3}";
        }
        catch (Exception ex)
        {
            throw new Exception($"Error generating agency code: {ex.Message}");
        }
    }



    public async Task<ResponseWrapper<List<MenuSingleDto>>> GetMenuAsync()
    {
        return await ExecuteWithCacheAsync(
            cacheKey: $"MenusSignle_{_currentUser.UserId}",

            action: async () =>
            {
                var query = _context.Menus
                    .AsNoTracking()
                    .OrderBy(x => x.Title);

                var result = await query
                    .Select(x => new MenuSingleDto
                    {
                        Id = x.Id,
                        Title = x.Title
                    })
                    .ToListAsync();

                return result;
            },

            successMessageFactory: result => $"{result.Count} menus fetched",
            cacheMessage: "Menus fetched from cache",
            errorMessage: "Error fetching menus"
        );
    }



    // ================================
    // CREATE
    // ================================
    public async Task<ResponseWrapper<Guid>> CreateBranchAsync(CreateBranchDto dto)
    {
        if (string.IsNullOrEmpty(_currentUser.UserId))
        {
            return await ResponseWrapper<Guid>.FailureAsync("Unauthorized", "User not authenticated", 401);
        }

        return await ExecuteWriteAsync(async () =>
        {
            try
            {
                var entity = _mapper.Map<Branch>(dto);

                entity.Id = Guid.NewGuid();
                entity.UserId = _currentUser.UserId;
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;

                entity.Code = await GenerateBranchCodeAsync();

                _context.Branches.Add(entity);
                await _context.SaveChangesAsync();

                _cache.Remove(BranchCacheKey);

                return entity.Id;
            }
            catch (DbUpdateException ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                throw new Exception($"Database error while creating branch: {msg}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Unexpected error while creating branch: {ex.Message}");
            }

        }, "Branch created successfully", "Error creating branch");
    }

    // ================================
    // GET ALL
    // ================================
    public async Task<ResponseWrapper<PagedResponse<BranchDto>>> GetAllBranchesAsync(int page = 1, int pageSize = 10)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;
        pageSize = Math.Min(pageSize, 100);

        var agencyId = _currentUser.AgencyId;
        var isAdmin = _currentUser.IsInRole("Administrator"); // ✅ muhiim

        return await ExecuteWithCacheAsync(
            cacheKey: $"{BranchCacheKey}_{_currentUser.UserId}_{page}_{pageSize}",

            action: async () =>
            {
                var query = _context.Branches.AsNoTracking();

                // ✅ USER caadi → filter by AgencyId
                if (!isAdmin)
                {
                    if (agencyId == Guid.Empty)
                    {
                        return new PagedResponse<BranchDto>(new List<BranchDto>(), page, pageSize, 0);
                    }

                    query = query.Where(x => x.AgencyId == agencyId); // 🔥 muhiim
                }

                // ✅ ADMIN → ar everything (no filter)

                var totalRecords = await query.CountAsync();

                var mapped = await query
                    .OrderByDescending(x => x.CreatedAt)
                    .ThenBy(x => x.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ProjectTo<BranchDto>(_mapper.ConfigurationProvider)
                    .ToListAsync();

                return new PagedResponse<BranchDto>(mapped, page, pageSize, totalRecords);
            },
            successMessageFactory: result => $"{result.Data.Count} of {result.TotalRecords} branches fetched",
            cacheMessage: "Branches fetched from cache",
            errorMessage: "Error fetching branches"
        );
    }    // UPDATE
         // ================================


    public async Task<ResponseWrapper<PagedResponse<MenuSummaryDto>>> GetAllSummaryMenuAsync(int page = 1, int pageSize = 10, string search = "")
    {
        // Sanitize inputs
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;
        pageSize = Math.Min(pageSize, 100);
        search = search?.Trim().ToLower() ?? "";

        // IMPORTANT: Cache key must include the page number
        // If "search" is used, it MUST be in the key too
        string cacheKey = $"MenuSummary_{_currentUser.UserId}_P{page}_PS{pageSize}_S{search}";

        return await ExecuteWithCacheAsync(
            cacheKey: cacheKey,
            action: async () =>
            {
                // 1. Base Query
                var query = _context.Menus
                    .Where(x => x.UserId == _currentUser.UserId)
                    .AsNoTracking();

                // 2. Apply Search (if applicable)
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(x => x.Title.ToLower().Contains(search));
                }

                // 3. Count total before skipping
                var totalRecords = await query.CountAsync();

                // 4. Skip and Take logic
                // Page 1: Skip(0).Take(10)
                // Page 2: Skip(10).Take(10)
                var mapped = await query
                    .OrderByDescending(x => x.CreatedAt)
                    .ThenBy(x => x.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ProjectTo<MenuSummaryDto>(_mapper.ConfigurationProvider)
                    .ToListAsync();

                return new PagedResponse<MenuSummaryDto>(mapped, page, pageSize, totalRecords);
            },
            successMessageFactory: result => $"{result.Data.Count} menus fetched",
            cacheMessage: "Results returned from cache",
            errorMessage: "Error fetching menu summary"
        );
    }


    public async Task<ResponseWrapper<bool>> UpdateBranchAsync(Guid id, UpdateBranchDto dto)
    {
        return await ExecuteWriteAsync(async () =>
        {
            try
            {
                var entity = await _context.Branches.FindAsync(id);

                if (entity == null)
                    throw new Exception("Branch not found");

                _mapper.Map(dto, entity);

                entity.UpdatedAt = DateTime.UtcNow;
                entity.UpdatedBy = _currentUser.UserId;

                await _context.SaveChangesAsync();

                _cache.Remove(BranchCacheKey);

                return true;
            }
            catch (DbUpdateException ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                throw new Exception($"Database error while updating branch: {msg}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating branch: {ex.Message}");
            }

        }, "Branch updated successfully", "Error updating agency");
    }

    // ================================
    // DELETE
    // ================================
    public async Task<ResponseWrapper<bool>> DeleteBranchAsync(Guid id)
    {
        return await ExecuteWriteAsync(async () =>
        {
            try
            {
                var entity = await _context.Branches.FindAsync(id);

                if (entity == null)
                    throw new Exception("Branch not found");

                _context.Branches.Remove(entity);
                await _context.SaveChangesAsync();

                _cache.Remove(BranchCacheKey);

                return true;
            }
            catch (DbUpdateException ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                throw new Exception($"Database error while deleting branch: {msg}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting branch: {ex.Message}");
            }

        }, "Branch deleted successfully", "Error deleting branch");
    }

    // ================================
    // CODE GENERATOR
    // ================================
    private async Task<string> GenerateBranchCodeAsync()
    {
        try
        {
            var year = DateTime.UtcNow.Year;

            var count = await _context.Branches
                .CountAsync(x => x.CreatedAt.Year == year);

            var nextNumber = count + 1;

            return $"A{year}{nextNumber:D3}";
        }
        catch (Exception ex)
        {
            throw new Exception($"Error generating agency code: {ex.Message}");
        }
    }







    // ================================
    // CREATE
    // ================================
    public async Task<ResponseWrapper<int>> CreateMenuAsync(CreateMenuDto dto)
    {
        if (string.IsNullOrEmpty(_currentUser.UserId))
        {
            return await ResponseWrapper<int>.FailureAsync(
                "Unauthorized",
                "User not authenticated",
                401
            );
        }

        return await ExecuteWriteAsync(async () =>
        {
            // ✅ Validate Parent
            if (dto.ParentId.HasValue)
            {
                var parentExists = await _context.Menus
                    .AnyAsync(x => x.Id == dto.ParentId.Value);

                if (!parentExists)
                    throw new Exception("Parent menu not found");
            }

            // ✅ Prevent Duplicate (same title under same parent)
            var duplicateExists = await _context.Menus.AnyAsync(x =>
                x.UserId == _currentUser.UserId &&
                x.Title.ToLower() == dto.Title.ToLower() &&
                x.ParentId == dto.ParentId
            );

            if (duplicateExists)
                throw new Exception("Menu with same title already exists in this level");

            // ✅ Prevent Order Conflict
            var orderExists = await _context.Menus.AnyAsync(x =>
                x.UserId == _currentUser.UserId &&
                x.ParentId == dto.ParentId &&
                x.OrderNo == dto.OrderNo
            );

            // 👉 Auto-fix order if conflict
            if (orderExists)
            {
                var maxOrder = await _context.Menus
                    .Where(x => x.UserId == _currentUser.UserId && x.ParentId == dto.ParentId)
                    .MaxAsync(x => (int?)x.OrderNo) ?? 0;

                dto.OrderNo = maxOrder + 1;
            }

            // ✅ Map DTO → Entity
            var entity = _mapper.Map<Menu>(dto);

            entity.UserId = _currentUser.UserId;
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            // ✅ Safety check
            if (entity.OrderNo < 0)
                entity.OrderNo = 0;

            _context.Menus.Add(entity);
            await _context.SaveChangesAsync();

            // ✅ Clear cache
            _cache.Remove(MenuCacheKey);

            return entity.Id;

        }, "Menu created successfully", "Error creating menu");
    }    // ================================
         // GET ALL
         // ================================
    public async Task<ResponseWrapper<List<ModuleMenuDto>>> GetAllMenuAsync()
    {
        return await ExecuteWithCacheAsync(
            cacheKey: $"{MenuCacheKey}_{_currentUser.UserId}",
            action: async () =>
            {
                // 🔴 STEP 1: CHECK SUBSCRIPTION
                var subscription = await _context.Subscriptions
                    .FirstOrDefaultAsync(x =>
                        x.AgencyId == _currentUser.AgencyId &&
                        x.Status == SubscriptionStatus.Active);

                if (subscription == null)
                {
                    return new List<ModuleMenuDto>(); // ❌ no access
                }

                // 🔴 STEP 2: GET PLAN PERMISSIONS
                var allowedPermissions = await _context.PlanPermissions
                    .Where(x => x.PlanId == subscription.PlanId)
                    .Select(x => x.Permission.KeyName)
                    .ToListAsync();

                // 🔹 STEP 3: GET MENUS
                var menus = await _context.Menus
                    //.Where(x => x.UserId == _currentUser.UserId)
                    .Include(x => x.Module)
                    .Include(x => x.MenuPermissions)
                        .ThenInclude(mp => mp.Permission)
                    .OrderBy(x => x.OrderNo)
                    .ThenBy(x => x.Id)
                    .AsNoTracking()
                    .ToListAsync();

                // 🔹 MODULE MAP
                var moduleMap = menus
                    .Where(x => x.Module != null)
                    .GroupBy(x => x.ModuleId)
                    .ToDictionary(g => g.Key, g => g.First().Module.Name);

                // 🔥 STEP 4: MAP + FILTER BY PLAN
                var menuDtos = menus.Select(m => new MenuDto
                {
                    Id = m.Id,
                    Title = m.Title,
                    Href = m.Href,
                    Icon = m.Icon,
                    ParentId = m.ParentId,
                    OrderNo = m.OrderNo,
                    ModuleId = m.ModuleId,

                    // 🔥 PLAN FILTER HERE
                    Permissions = m.MenuPermissions
                        .Select(mp => mp.Permission.KeyName)
                        .Where(p => allowedPermissions.Contains(p))
                        .Distinct()
                        .ToList()
                }).ToList();

                // 🔹 STEP 5: ALLOWED MENUS
                var allowed = menuDtos
                    .Where(m => m.Permissions.Any())
                    .Select(m => m.Id)
                    .ToHashSet();

                var lookup = menuDtos.ToDictionary(x => x.Id);

                // 🔥 INCLUDE PARENTS
                foreach (var menu in menuDtos)
                {
                    var current = menu;

                    while (current.ParentId != null)
                    {
                        if (!lookup.ContainsKey(current.ParentId.Value))
                            break;

                        allowed.Add(current.ParentId.Value);
                        current = lookup[current.ParentId.Value];
                    }
                }

                // 🔹 STEP 6: FILTER
                var filtered = menuDtos
                    .Where(m => allowed.Contains(m.Id))
                    .ToList();

                // 🔹 STEP 7: BUILD TREE
                var tree = BuildMenuTree(filtered);

                // 🔹 STEP 8: GROUP BY MODULE
                var result = tree
                    .GroupBy(m => m.ModuleId)
                    .Select(g => new ModuleMenuDto
                    {
                        ModuleId = g.Key,
                        ModuleName = moduleMap.TryGetValue(g.Key, out var name) ? name : "",
                        Menus = g.ToList()
                    })
                    .ToList();

                return result;
            },
            successMessageFactory: result => $"{result.Count} modules fetched",
            cacheMessage: "Menu fetched from cache",
            errorMessage: "Error fetching menu"
        );
    }
    private List<MenuDto> BuildMenuTree(List<MenuDto> menus)
    {
        var lookup = menus.ToDictionary(x => x.Id);

        foreach (var menu in menus)
        {
            menu.Children = new List<MenuDto>(); // ✅ always reset
        }

        var root = new List<MenuDto>();

        foreach (var menu in menus)
        {
            if (menu.ParentId == null)
            {
                root.Add(menu);
            }
            else if (lookup.TryGetValue(menu.ParentId.Value, out var parent))
            {
                parent.Children.Add(menu);
            }
        }

        return root;
    }    // ================================
    public async Task<ResponseWrapper<bool>> UpdateMenuAsync(int id, UpdateMenuDto dto)
    {
        return await ExecuteWriteAsync(async () =>
        {
            try
            {
                var entity = await _context.Menus.FindAsync(id);

                if (entity == null)
                    throw new Exception("Menu not found");

                _mapper.Map(dto, entity);

                entity.UpdatedAt = DateTime.UtcNow;
                entity.UpdatedBy = _currentUser.UserId;

                await _context.SaveChangesAsync();

                _cache.Remove(MenuCacheKey);

                return true;
            }
            catch (DbUpdateException ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                throw new Exception($"Database error while updating menu: {msg}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating menu: {ex.Message}");
            }

        }, "Menu updated successfully", "Error updating Menu");
    }

    // ================================
    // DELETE
    // ================================
    public async Task<ResponseWrapper<bool>> DeleteMenuAsync(int id)
    {
        return await ExecuteWriteAsync(async () =>
        {
            try
            {
                var entity = await _context.Menus.FindAsync(id);

                if (entity == null)
                    throw new Exception("Menu not found");

                _context.Menus.Remove(entity);
                await _context.SaveChangesAsync();

                _cache.Remove(MenuCacheKey);

                return true;
            }
            catch (DbUpdateException ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                throw new Exception($"Database error while deleting Menu: {msg}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting menu: {ex.Message}");
            }

        }, "menu deleted successfully", "Error deleting menu");
    }



    // CREATE
    // ================================
    public async Task<ResponseWrapper<int>> CreateModuleAsync(CreateModuleDto dto)
    {
        if (string.IsNullOrEmpty(_currentUser.UserId))
        {
            return await ResponseWrapper<int>.FailureAsync(
                "Unauthorized",
                "User not authenticated",
                401
            );
        }

        return await ExecuteWriteAsync(async () =>
        {

            // ✅ Map DTO → Entity
            var entity = _mapper.Map<Module>(dto);

            entity.UserId = _currentUser.UserId;
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;


            _context.Modules.Add(entity);
            await _context.SaveChangesAsync();

            // ✅ Clear cache
            _cache.Remove(ModuleCacheKey);

            return entity.Id;

        }, "Module created successfully", "Error creating module");
    }
    // ================================
    // GET ALL
    // ================================
    public async Task<ResponseWrapper<PagedResponse<ModuleDto>>> GetAllModuleAsync(int page = 1, int pageSize = 10)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;
        pageSize = Math.Min(pageSize, 100);

        return await ExecuteWithCacheAsync(
            cacheKey: $"{ModuleCacheKey}_{_currentUser.UserId}_{page}_{pageSize}",
            action: async () =>
            {
                var baseQuery = _context.Modules
                    .Where(x => x.UserId == _currentUser.UserId)
                    .AsNoTracking();

                var totalRecords = await baseQuery.CountAsync();

                var mapped = await baseQuery
                    .OrderByDescending(x => x.CreatedAt)
                    .ThenBy(x => x.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ProjectTo<ModuleDto>(_mapper.ConfigurationProvider)
                    .ToListAsync();

                return new PagedResponse<ModuleDto>(mapped, page, pageSize, totalRecords);
            },
            successMessageFactory: result => $"{result.Data.Count} of {result.TotalRecords} Module fetched",
            cacheMessage: "Module fetched from cache",
            errorMessage: "Error fetching module"
        );
    }    // ================================
    // UPDATE
    // ================================
    public async Task<ResponseWrapper<bool>> UpdateModuleAsync(int id, UpdateModuleDto dto)
    {
        return await ExecuteWriteAsync(async () =>
        {
            try
            {
                var entity = await _context.Modules.FindAsync(id);

                if (entity == null)
                    throw new Exception("Module not found");

                _mapper.Map(dto, entity);

                entity.UpdatedAt = DateTime.UtcNow;
                entity.UpdatedBy = _currentUser.UserId;

                await _context.SaveChangesAsync();

                _cache.Remove(ModuleCacheKey);

                return true;
            }
            catch (DbUpdateException ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                throw new Exception($"Database error while updating module: {msg}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating module: {ex.Message}");
            }

        }, "Menu updated successfully", "Error updating module");
    }

    // ================================
    // DELETE
    // ================================
    public async Task<ResponseWrapper<bool>> DeleteModuleAsync(int id)
    {
        return await ExecuteWriteAsync(async () =>
        {
            try
            {
                var entity = await _context.Modules.FindAsync(id);

                if (entity == null)
                    throw new Exception("Menu not found");

                _context.Modules.Remove(entity);
                await _context.SaveChangesAsync();

                _cache.Remove(ModuleCacheKey);

                return true;
            }
            catch (DbUpdateException ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                throw new Exception($"Database error while deleting module: {msg}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting module: {ex.Message}");
            }

        }, "module deleted successfully", "Error deleting module");
    }









    // CREATE
    // ================================
    public async Task<ResponseWrapper<List<int>>> CreateMenuPermissionAsync(CreateMenuPermissionDto dto)
    {
        if (string.IsNullOrEmpty(_currentUser.UserId))
        {
            return await ResponseWrapper<List<int>>.FailureAsync(
                "Unauthorized",
                "User not authenticated",
                401
            );
        }

        return await ExecuteWriteAsync(async () =>
        {
            var entities = new List<MenuPermission>();

            foreach (var permissionId in dto.PermissionIds)
            {
                var entity = new MenuPermission
                {
                    MenuId = dto.MenuId,
                    PermissionId = permissionId,
                    UserId = _currentUser.UserId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                entities.Add(entity);
            }

            await _context.MenuPermissions.AddRangeAsync(entities);
            await _context.SaveChangesAsync();

            // ✅ Clear cache
            _cache.Remove(MenuPermisstionCacheKey);

            return entities.Select(x => x.Id).ToList();

        }, "Menu Permissions created successfully", "Error creating Menu Permissions");
    }    // ================================
         // GET ALL
         // ================================
    public async Task<ResponseWrapper<PagedResponse<MenuPermissionGroupedDto>>> GetAllMenuPermissionAsync(int page = 1, int pageSize = 10)
    {
        if (pageSize <= 0) pageSize = 10;
        pageSize = Math.Min(pageSize, 100);

        return await ExecuteWithCacheAsync(
            cacheKey: $"{MenuPermisstionCacheKey}_{_currentUser.UserId}_{page}_{pageSize}",
            action: async () =>
            {
                // 🔹 1. Get user permissions from token
                var user = _httpContextAccessor.HttpContext?.User;

                var userPermissions = user?.Claims
                    .Where(c => c.Type == "permission")
                    .Select(c => c.Value)
                    .ToList() ?? new List<string>();

                var isAdmin = user?.IsInRole("Administrator") ?? false;

                // 🔹 2. Base query
                var query = _context.MenuPermissions
                    .Include(x => x.Menu)
                        .ThenInclude(m => m.Parent)
                    .Include(x => x.Permission)
                    .AsNoTracking()
                    .AsQueryable();

                // 🔥 3. Apply filtering ONLY for non-admin
                if (!isAdmin)
                {
                    query = query.Where(x =>
                        userPermissions.Contains(x.Permission.KeyName)
                    );
                }

                // 🔹 4. Count grouped menus
                var totalRecords = await query
                    .Select(x => x.MenuId)
                    .Distinct()
                    .CountAsync();

                // 🔹 5. Group + project
                var grouped = await query
                    .GroupBy(x => new
                    {
                        x.MenuId,
                        MenuTitle = x.Menu.Title,
                        ParentId = x.Menu.ParentId,
                        ParentTitle = x.Menu.Parent != null ? x.Menu.Parent.Title : null
                    })
                    .Select(g => new MenuPermissionGroupedDto
                    {
                        MenuId = g.Key.MenuId,
                        MenuTitle = g.Key.MenuTitle,
                        ParentId = g.Key.ParentId,
                        ParentTitle = g.Key.ParentTitle,

                        Permissions = g
                            .Select(x => x.Permission.Name)
                            .Distinct()
                            .ToList(),

                        PermissionKeys = g
                            .Select(x => x.Permission.KeyName)
                            .Distinct()
                            .ToList()
                    })
                    .OrderByDescending(x => x.MenuId)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return new PagedResponse<MenuPermissionGroupedDto>(
                    grouped,
                    page,
                    pageSize,
                    totalRecords
                );
            },
            successMessageFactory: result =>
                $"{result.Data.Count} of {result.TotalRecords} grouped menu permissions fetched",
            cacheMessage: "Menu permission fetched from cache",
            errorMessage: "Error fetching menu permission"
        );
    }
    public async Task<ResponseWrapper<bool>> UpdateMenuPermissionAsync(int id, UpdateMenuPermissionDto dto)
    {
        return await ExecuteWriteAsync(async () =>
        {
            // Step 1: Check if record exists (optional use id)
            var existing = await _context.MenuPermissions
                .Where(x => x.MenuId == dto.MenuId && x.UserId == _currentUser.UserId)
                .ToListAsync();

            if (!existing.Any())
                throw new Exception("Menu Permission not found");

            // Step 2: Remove old permissions
            _context.MenuPermissions.RemoveRange(existing);

            // Step 3: Add new permissions
            var newEntities = dto.PermissionIds.Select(permissionId => new MenuPermission
            {
                MenuId = dto.MenuId,
                PermissionId = permissionId,
                UserId = _currentUser.UserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = _currentUser.UserId
            }).ToList();

            await _context.MenuPermissions.AddRangeAsync(newEntities);
            await _context.SaveChangesAsync();

            // Step 4: Clear cache
            _cache.Remove(MenuPermisstionCacheKey);

            return true;

        }, "Menu permissions updated successfully", "Error updating menu permissions");
    }
    // ================================
    // DELETE
    // ================================
    public async Task<ResponseWrapper<bool>> DeleteMenuPermissionAsync(int id)
    {
        return await ExecuteWriteAsync(async () =>
        {
            try
            {
                var entity = await _context.MenuPermissions.FindAsync(id);

                if (entity == null)
                    throw new Exception("Menu permission not found");

                _context.MenuPermissions.Remove(entity);
                await _context.SaveChangesAsync();

                _cache.Remove(MenuPermisstionCacheKey);

                return true;
            }
            catch (DbUpdateException ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                throw new Exception($"Database error while deleting menu permission: {msg}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting menu permission: {ex.Message}");
            }

        }, "menu permission deleted successfully", "Error deleting menu permission");
    }







    // CREATE
    // ================================
    public async Task<ResponseWrapper<List<int>>> CreateRolePermissionAsync(CreateRolePermissionDto dto)
    {
        if (string.IsNullOrEmpty(_currentUser.UserId))
        {
            return await ResponseWrapper<List<int>>.FailureAsync(
                "Unauthorized",
                "User not authenticated",
                401
            );
        }

        return await ExecuteWriteAsync(async () =>
        {
            var permissionIds = dto.PermissionIds.Distinct().ToList();

            if (!permissionIds.Any())
                throw new Exception("No permissions provided");

            // ✅ Validate Role
            if (!await _context.Roles.AnyAsync(r => r.Id == dto.RoleId))
                throw new Exception("Invalid RoleId");

            // ✅ Validate Permissions
            var validPermissions = await _context.Permissions
                .Where(p => permissionIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync();

            var invalid = permissionIds.Except(validPermissions).ToList();
            if (invalid.Any())
                throw new Exception($"Invalid PermissionIds: {string.Join(",", invalid)}");

            // ✅ Check existing
            var existing = await _context.RolePermissions
                .Where(x => x.RoleId == dto.RoleId)
                .Select(x => x.PermissionId)
                .ToListAsync();

            var newPermissions = validPermissions.Except(existing).ToList();

            if (!newPermissions.Any())
                return new List<int>();

            // ✅ Insert
            var entities = newPermissions.Select(pid => new RolePermission
            {
                RoleId = dto.RoleId,
                PermissionId = pid,
                UserId = _currentUser.UserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }).ToList();

            await _context.RolePermissions.AddRangeAsync(entities);
            await _context.SaveChangesAsync();

            _cache.Remove(RolePermisstionCacheKey);

            return entities.Select(x => x.Id).ToList();

        }, "Role permissions created successfully", "Error creating Role permissions");
    }    // ================================
    // GET ALL
    // ================================
    public async Task<ResponseWrapper<PagedResponse<RolePermissionDto>>> GetAllRolePermissionAsync(int page = 1, int pageSize = 10)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;
        pageSize = Math.Min(pageSize, 100);

        return await ExecuteWithCacheAsync(
            cacheKey: $"{RolePermisstionCacheKey}_{_currentUser.UserId}_{page}_{pageSize}",
            action: async () =>
            {
                var baseQuery = _context.RolePermissions
                    .Where(x => x.UserId == _currentUser.UserId)
                    .AsNoTracking();

                var totalRecords = await baseQuery.CountAsync();

                var mapped = await baseQuery
                    .OrderByDescending(x => x.CreatedAt)
                    .ThenBy(x => x.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ProjectTo<RolePermissionDto>(_mapper.ConfigurationProvider)
                    .ToListAsync();

                return new PagedResponse<RolePermissionDto>(mapped, page, pageSize, totalRecords);
            },
            successMessageFactory: result => $"{result.Data.Count} of {result.TotalRecords} Role Permission fetched",
            cacheMessage: "Role permission fetched from cache",
            errorMessage: "Error fetching Role permission"
        );
    }    // ================================

    // ================================
    // DELETE
    // ================================
    public async Task<ResponseWrapper<bool>> DeleteRolePermissionAsync(int id)
    {
        return await ExecuteWriteAsync(async () =>
        {
            try
            {
                var entity = await _context.RolePermissions.FindAsync(id);

                if (entity == null)
                    throw new Exception("Role permission not found");

                _context.RolePermissions.Remove(entity);
                await _context.SaveChangesAsync();

                _cache.Remove(RolePermisstionCacheKey);

                return true;
            }
            catch (DbUpdateException ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                throw new Exception($"Database error while deleting role permission: {msg}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting role permission: {ex.Message}");
            }

        }, "role permission deleted successfully", "Error deleting role permission");
    }






    // CREATE
    // ================================
    public async Task<ResponseWrapper<int>> CreatePermissionAsync(CreatePermissionDto dto)
    {
        if (string.IsNullOrEmpty(_currentUser.UserId))
        {
            return await ResponseWrapper<int>.FailureAsync(
                "Unauthorized",
                "User not authenticated",
                401
            );
        }

        return await ExecuteWriteAsync(async () =>
        {

            // ✅ Map DTO → Entity
            var entity = _mapper.Map<Permission>(dto);

            entity.UserId = _currentUser.UserId;
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;


            _context.Permissions.Add(entity);
            await _context.SaveChangesAsync();

            // ✅ Clear cache
            _cache.Remove(PermissionCacheKey);

            return entity.Id;

        }, "Permission created successfully", "Error creating  Permission");
    }
    // ================================
    // GET ALL
    // ================================
    public async Task<ResponseWrapper<PagedResponse<PermissionDto>>> GetAllPermissionAsync(int page = 1, int pageSize = 10)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;
        pageSize = Math.Min(pageSize, 100);

        return await ExecuteWithCacheAsync(
            cacheKey: $"{PermissionCacheKey}_{_currentUser.UserId}_{page}_{pageSize}",
            action: async () =>
            {
                var baseQuery = _context.Permissions
                    .Where(x => x.UserId == _currentUser.UserId)
                    .AsNoTracking();

                var totalRecords = await baseQuery.CountAsync();

                var mapped = await baseQuery
                    .OrderByDescending(x => x.CreatedAt)
                    .ThenBy(x => x.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ProjectTo<PermissionDto>(_mapper.ConfigurationProvider)
                    .ToListAsync();

                return new PagedResponse<PermissionDto>(mapped, page, pageSize, totalRecords);
            },
            successMessageFactory: result => $"{result.Data.Count} of {result.TotalRecords} Permission fetched",
            cacheMessage: "permission fetched from cache",
            errorMessage: "Error fetching permission"
        );
    }    // ================================
    // UPDATE
    // ================================
    public async Task<ResponseWrapper<bool>> UpdatePermissionAsync(int id, UpdatePermissionDto dto)
    {
        return await ExecuteWriteAsync(async () =>
        {
            try
            {
                var entity = await _context.Permissions.FindAsync(id);

                if (entity == null)
                    throw new Exception("Permission not found");

                _mapper.Map(dto, entity);

                entity.UpdatedAt = DateTime.UtcNow;
                entity.UpdatedBy = _currentUser.UserId;

                await _context.SaveChangesAsync();

                _cache.Remove(PermissionCacheKey);

                return true;
            }
            catch (DbUpdateException ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                throw new Exception($"Database error while updating permission: {msg}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating permission: {ex.Message}");
            }

        }, "permission updated successfully", "Error updating permission");
    }

    // ================================
    // DELETE
    // ================================
    public async Task<ResponseWrapper<bool>> DeletePermissionAsync(int id)
    {
        return await ExecuteWriteAsync(async () =>
        {
            try
            {
                var entity = await _context.Permissions.FindAsync(id);

                if (entity == null)
                    throw new Exception("Permission not found");

                _context.Permissions.Remove(entity);
                await _context.SaveChangesAsync();

                _cache.Remove(PermissionCacheKey);

                return true;
            }
            catch (DbUpdateException ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                throw new Exception($"Database error while deleting permission: {msg}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting menu permission: {ex.Message}");
            }

        }, "menu permission deleted successfully", "Error deleting menu permission");
    }










}