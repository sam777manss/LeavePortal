using System;
using System.Collections.Generic;
using System.Text;

namespace LeavePortal.Core.DTOs.Auth
{
    public class AuthResponse
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
