using Microsoft.AspNetCore.Identity;

namespace planirovanie.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }   // Имя + фамилия + отчество
        public string Position { get; set; }   // Должность
    }
}