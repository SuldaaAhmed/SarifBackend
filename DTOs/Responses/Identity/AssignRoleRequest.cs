using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.Responses.Identity
{
    public sealed class AssignRoleRequest
    {
        [Required]
        public string RoleName { get; set; } = string.Empty;
    }
}
