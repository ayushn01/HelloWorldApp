using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace HelloWorld.Models
{

    public class ApplicationUser : IdentityUser
    {

        public string? ProfilePicture { get; set; }
        public DateTime LastSeen { get; set; }
    }

}
