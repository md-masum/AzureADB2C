using System.ComponentModel.DataAnnotations;

namespace AzureADB2CApi
{
    public class UserUpdateModel
    {
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        public string DisplayName { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public Roles Role { get; set; } = Roles.User;
    }
}
