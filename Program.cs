using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using TrelloCloneAPI.Data;
using TrelloCloneAPI.Models;
using TrelloCloneAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TrelloDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "server=localhost;port=3306;database=TrelloClone;user=root;password=Password123!";
    var serverVersion = new MySqlServerVersion(new Version(8, 0, 33));
    options.UseMySql(connectionString, serverVersion, mysqlOptions =>
    {
        mysqlOptions.EnableStringComparisonTranslations();
    })
    .EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
});

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IWorkspaceService, WorkspaceService>();
builder.Services.AddScoped<IBoardService, BoardService>();
builder.Services.AddScoped<IListService, ListService>();
builder.Services.AddScoped<IListManagementService, ListManagementService>();
builder.Services.AddScoped<ICardService, CardService>();
builder.Services.AddScoped<ICardManagementService, CardManagementService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IAttachmentService, AttachmentService>();
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();
builder.Services.AddScoped<IReportService, ReportService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token (do not prepend 'Bearer ' as Swagger will add it automatically)."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy => policy
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("Jwt");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? string.Empty))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

var app = builder.Build();

var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TrelloDbContext>();
    db.Database.EnsureCreated();
    await SeedDatabaseAsync(db);
}

app.MapPost("/auth/register", async (RegisterRequest request, TrelloDbContext context) =>
{
    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { error = "Email and password are required." });
    }

    var email = request.Email.Trim().ToLowerInvariant();
    if (await context.Users.AnyAsync(u => u.Email == email))
    {
        return Results.Conflict(new { error = "A user with that email already exists." });
    }

    var role = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Member");
    var user = new User
    {
        Id = Guid.NewGuid(),
        Email = email,
        FirstName = request.FirstName?.Trim(),
        LastName = request.LastName?.Trim(),
        PasswordHash = CreatePasswordHash(request.Password),
        RoleId = role?.Id,
        Status = "Active",
        CreatedAt = DateTime.UtcNow
    };

    context.Users.Add(user);
    await context.SaveChangesAsync();

    var roleName = role?.RoleName ?? "Member";
    var token = GenerateJwtToken(user, new[] { roleName });
    return Results.Ok(new AuthResponse(token, user.Email, new[] { roleName }, user.FirstName, user.LastName, user.Id.ToString()));
})
.AllowAnonymous();

app.MapPost("/auth/login", async (LoginRequest request, TrelloDbContext context) =>
{
    var email = request.Email?.Trim().ToLowerInvariant();
    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { error = "Email and password are required." });
    }

    var existingUser = await context.Users.Include(u => u.Role).SingleOrDefaultAsync(u => u.Email == email);
    if (existingUser is null || existingUser.PasswordHash is null || !VerifyPasswordHash(request.Password, existingUser.PasswordHash))
    {
        return Results.Unauthorized();
    }

    var roles = existingUser.Role is not null ? new[] { existingUser.Role.RoleName } : new[] { "Member" };
    var token = GenerateJwtToken(existingUser, roles);
    return Results.Ok(new AuthResponse(token, existingUser.Email, roles, existingUser.FirstName, existingUser.LastName, existingUser.Id.ToString()));
})
.AllowAnonymous();

app.MapGet("/auth/profile", async (ClaimsPrincipal user, TrelloDbContext context) =>
{
    var email = user.Identity?.Name;
    if (string.IsNullOrEmpty(email))
    {
        return Results.Unauthorized();
    }

    var existingUser = await context.Users.Include(u => u.Role).SingleOrDefaultAsync(u => u.Email == email);
    if (existingUser is null)
    {
        return Results.NotFound();
    }

    var roles = existingUser.Role is not null ? new[] { existingUser.Role.RoleName } : Array.Empty<string>();
    return Results.Ok(new UserInfo(existingUser.Email, roles));
})
.RequireAuthorization();

app.MapGet("/auth/users", async (TrelloDbContext context) =>
{
    var users = await context.Users.Include(u => u.Role)
        .Select(u => new { u.Id, u.Email, Roles = new[] { u.Role != null ? u.Role.RoleName : "Member" } })
        .ToListAsync();
    return Results.Ok(users);
})
.RequireAuthorization();

app.MapPut("/auth/role", async (RoleUpdateRequest request, TrelloDbContext context) =>
{
    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Role))
    {
        return Results.BadRequest(new { error = "Email and role are required." });
    }

    var email = request.Email.Trim().ToLowerInvariant();
    var targetUser = await context.Users.Include(u => u.Role).SingleOrDefaultAsync(u => u.Email == email);
    if (targetUser is null)
    {
        return Results.NotFound(new { error = "User not found." });
    }

    var role = await context.Roles.SingleOrDefaultAsync(r => r.RoleName == request.Role);
    if (role is null)
    {
        return Results.NotFound(new { error = "Role not found." });
    }

    targetUser.RoleId = role.Id;
    await context.SaveChangesAsync();
    return Results.Ok(new { targetUser.Email, Roles = new[] { role.RoleName } });
})
.RequireAuthorization("AdminOnly");

app.MapGet("/weatherforecast", () =>
{
    var summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast(
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)])).ToArray();

    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

await app.RunAsync();

static async Task SeedDatabaseAsync(TrelloDbContext db)
{
    if (!await db.Roles.AnyAsync())
    {
        db.Roles.AddRange(
            new Role { Id = Guid.NewGuid(), RoleName = "Admin" },
            new Role { Id = Guid.NewGuid(), RoleName = "Member" });
        await db.SaveChangesAsync();
    }

    if (!await db.Users.AnyAsync(u => u.Email == "admin@trello.local"))
    {
        var adminRole = await db.Roles.SingleAsync(r => r.RoleName == "Admin");
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@trello.local",
            PasswordHash = CreatePasswordHash("Admin123!"),
            RoleId = adminRole.Id,
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }
}

static string CreatePasswordHash(string password)
{
    var salt = RandomNumberGenerator.GetBytes(16);
    using var deriveBytes = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
    var hash = deriveBytes.GetBytes(32);
    return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
}

static bool VerifyPasswordHash(string password, string storedHash)
{
    var parts = storedHash.Split(':');
    if (parts.Length != 2) return false;

    var salt = Convert.FromBase64String(parts[0]);
    var expectedHash = Convert.FromBase64String(parts[1]);
    using var deriveBytes = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
    var actualHash = deriveBytes.GetBytes(32);
    return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
}

string GenerateJwtToken(User user, IEnumerable<string> roles)
{
    var jwtSettings = builder.Configuration.GetSection("Jwt");
    var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? string.Empty);
    var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Email),
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, user.Email)
    };

    if (!string.IsNullOrEmpty(user.FirstName))
        claims.Add(new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName));
    if (!string.IsNullOrEmpty(user.LastName))
        claims.Add(new Claim(JwtRegisteredClaimNames.FamilyName, user.LastName));

    claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

    var token = new JwtSecurityToken(
        issuer: jwtSettings["Issuer"],
        audience: jwtSettings["Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(double.TryParse(jwtSettings["DurationMinutes"], out var minutes) ? minutes : 60),
        signingCredentials: credentials);

    return new JwtSecurityTokenHandler().WriteToken(token);
}

record LoginRequest(string Email, string Password);
record RegisterRequest(string Email, string Password, string? FirstName = null, string? LastName = null);
record RoleUpdateRequest(string Email, string Role);
record AuthResponse(string Token, string Email, string[] Roles, string? FirstName = null, string? LastName = null, string? UserId = null);
record UserInfo(string Email, string[] Roles);

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
