using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using planirovanie.Models;
using System.ComponentModel.DataAnnotations;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string ReturnUrl { get; set; } = "/";

    public async Task<IActionResult> OnPostAsync(string returnUrl = "/")
    {
        ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Ищем пользователя: если введён email (с @) — по Email, иначе — по UserName.
        // PasswordSignInAsync ищет только по UserName, поэтому передаём именно его.
        var user = Input.Email.Contains('@')
            ? await _userManager.FindByEmailAsync(Input.Email)
            : await _userManager.FindByNameAsync(Input.Email);

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Неверный логин или пароль.");
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName,
            Input.Password,
            Input.RememberMe,
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            return LocalRedirect(returnUrl);
        }

        ModelState.AddModelError(string.Empty, "Неверный логин или пароль.");
        return Page();
    }

    public class InputModel
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}