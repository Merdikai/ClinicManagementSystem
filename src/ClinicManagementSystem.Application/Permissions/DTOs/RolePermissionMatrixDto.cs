using System;
using System.Collections.Generic;

namespace ClinicManagementSystem.Application.Permissions.DTOs;

public class RolePermissionMatrixDto
{
    public string RoleName { get; set; } = string.Empty;
    public List<string> GrantedPermissions { get; set; } = new();
    public List<string> AllAvailablePermissions { get; set; } = new();
}

public class UpdateRolePermissionsDto
{
    public string RoleName { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
}
