using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using planirovanie.Data;
using planirovanie.Models;
using planirovanie.Services;
using Microsoft.AspNetCore.Antiforgery;


var builder = WebApplication.CreateBuilder(args);

// 1. Подключение к MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 46))));
// 2. Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 3. Cookie аутентификации
builder.Services.ConfigureApplicationCookie(options => {
    options.LoginPath = "/Login";
    options.LogoutPath = "/Logout";
    options.AccessDeniedPath = "/AccessDenied";
});

// 4. Сервисы
builder.Services.AddScoped<EventService>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddHttpContextAccessor();

// 6. Добавляем Razor Pages
builder.Services.AddRazorPages();

var app = builder.Build();

// 7. Инициализация ролей и админа
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    // Создание роли Администратора
    if (!await roleManager.RoleExistsAsync("Administrator"))
        await roleManager.CreateAsync(new IdentityRole("Administrator"));

    // Создание роли Исполнителя
    if (!await roleManager.RoleExistsAsync("Executor"))
        await roleManager.CreateAsync(new IdentityRole("Executor"));

    // ДОБАВЛЕНО: Создание роли обычного пользователя (User)
    if (!await roleManager.RoleExistsAsync("User"))
        await roleManager.CreateAsync(new IdentityRole("User"));

    // Создание администратора
    var adminEmail = "admin@volgodonsk.local";
    if (await userManager.FindByEmailAsync(adminEmail) == null)
    {
        var admin = new ApplicationUser 
        { 
            UserName = adminEmail.Split('@')[0],
            Email = adminEmail,
            FullName = null,
            Position = null
        };
        var result = await userManager.CreateAsync(admin, "Admin12345!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, "Administrator");
            Console.WriteLine("✅ Администратор создан: admin@volgodonsk.local / Admin12345!");
        }
    }
    
    
    var userEmail = "user@volgodonsk.local";
    if (await userManager.FindByEmailAsync(userEmail) == null)
    {
        var user = new ApplicationUser 
        { 
            UserName = userEmail.Split('@')[0],
            Email = userEmail,
            FullName = null,
            Position = null
        };
        var result = await userManager.CreateAsync(user, "User12345!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, "User");
            Console.WriteLine("✅ Пользователь создан: user@volgodonsk.local / User12345!");
        }
    }
    
}

// 8. Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
// Global antiforgery middleware required for endpoints with antiforgery metadata
app.UseAntiforgery();

// POST-эндпоинт для входа (выполняет SignIn на уровне HTTP-ответа)
app.MapPost("/signin", async (HttpContext httpContext, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager) =>
{
    // Read body manually to avoid automatic form model binding which adds antiforgery metadata.
    using var reader = new StreamReader(httpContext.Request.Body);
    var body = await reader.ReadToEndAsync();
    var form = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(body);

    var email = form.ContainsKey("Email") ? form["Email"].ToString() : string.Empty;
    var password = form.ContainsKey("Password") ? form["Password"].ToString() : string.Empty;
    var remember = form.ContainsKey("RememberMe") && bool.TryParse(form["RememberMe"].ToString(), out var r) && r;

    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        return Results.Redirect("/login?error=1");

    // Ищем пользователя: если введён email (с @) — по Email, иначе — по UserName.
    // PasswordSignInAsync ищет только по UserName, поэтому передаём именно его.
    ApplicationUser? user = email.Contains('@')
        ? await userManager.FindByEmailAsync(email)
        : await userManager.FindByNameAsync(email);

    if (user == null)
        return Results.Redirect("/login?error=1");

    var result = await signInManager.PasswordSignInAsync(user.UserName, password, remember, lockoutOnFailure: false);
    if (result.Succeeded)
        return Results.Redirect("/");

    return Results.Redirect("/login?error=1");
});

// 9. ✅ ВАЖНО: Маппинг Razor Pages ДО Blazor
app.MapRazorPages();
app.MapGet("/admin", () => Results.Redirect("/admin/dashboard"));
app.MapGet("/admin/", () => Results.Redirect("/admin/dashboard"));
app.MapRazorComponents<planirovanie.App>()
    .AddInteractiveServerRenderMode();

// Возвращает antiforgery RequestToken (используется клиентским JS для форм)
app.MapGet("/antiforgery-token", (IAntiforgery antiforgery, HttpContext httpContext) =>
{
    var tokens = antiforgery.GetAndStoreTokens(httpContext);
    return Results.Json(new { token = tokens.RequestToken });
});

// LoginInputModel вынесена в отдельный файл Models/LoginInputModel.cs

app.Run();