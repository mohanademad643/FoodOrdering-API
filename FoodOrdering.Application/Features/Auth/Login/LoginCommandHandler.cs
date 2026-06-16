////using AutoMapper;
////using FoodOrdering.Application.Common.Models;
////using FoodOrdering.Application.Features.Auth.DTOs;
////using FoodOrdering.Domain.Entities;
////using FoodOrdering.Domain.Interfaces;
////using FoodOrdering.Infrastructure.Services;
////using MediatR;
////using Microsoft.AspNetCore.Identity;
////using Microsoft.Extensions.Configuration;

////namespace FoodOrdering.Application.Features.Auth.Login
////{
////    public class LoginCommandHandler : IRequestHandler<LoginCommand, ApiResponse<AuthResultDto>>
////    {
////        private readonly UserManager<ApplicationUser> _userManager;
////        private readonly ITokenService _tokenService;
////        private readonly IUnitOfWork _uow;
////        private readonly IMapper _mapper;
////        private readonly IConfiguration _config;

////        public LoginCommandHandler(
////            UserManager<ApplicationUser> userManager,
////            ITokenService tokenService,
////            IUnitOfWork uow,
////            IMapper mapper,
////            IConfiguration config)
////        {
////            _userManager = userManager;
////            _tokenService = tokenService;
////            _uow = uow;
////            _mapper = mapper;
////            _config = config;
////        }

////        public async Task<ApiResponse<AuthResultDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
////        {
////            var user = await _userManager.FindByEmailAsync(request.Email);
////            if (user == null || !user.IsActive)
////                return ApiResponse<AuthResultDto>.Fail("Invalid credentials.", 401);

////            var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
////            if (!passwordValid)
////                return ApiResponse<AuthResultDto>.Fail("Invalid credentials.", 401);

////            var accessToken = await _tokenService.GenerateAccessTokenAsync(user);
////            var refreshToken = _tokenService.GenerateRefreshToken();
////            var expiryDays = int.Parse(_config["Jwt:RefreshTokenExpiryDays"] ?? "7");

////            var refreshTokenEntity = new RefreshToken
////            {
////                Token = refreshToken,
////                UserId = user.Id,
////                ExpiresAt = DateTime.UtcNow.AddDays(expiryDays)
////            };

////            await _uow.RefreshTokens.AddAsync(refreshTokenEntity, cancellationToken);
////            await _uow.SaveChangesAsync(cancellationToken);

////            var roles = await _userManager.GetRolesAsync(user);
////            var dto = _mapper.Map<UserDto>(user);
////            dto.Roles = roles.ToList();

////            var expiryMinutes = int.Parse(_config["Jwt:AccessTokenExpiryMinutes"] ?? "60");

////            return ApiResponse<AuthResultDto>.Ok(new AuthResultDto
////            {
////                AccessToken = accessToken,
////                RefreshToken = refreshToken,
////                AccessTokenExpiry = DateTime.UtcNow.AddMinutes(expiryMinutes),
////                User = dto
////            }, "Login successful.");
////        }
////    }
////}
//using AutoMapper;
//using FoodOrdering.Application.Common.Models;
//using FoodOrdering.Application.Features.Auth.DTOs;
//using FoodOrdering.Domain.Entities;
//using FoodOrdering.Domain.Interfaces;
//using FoodOrdering.Infrastructure.Services;
//using MediatR;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.Extensions.Configuration;

//namespace FoodOrdering.Application.Features.Auth.Login;

///// <summary>
///// Handles user authentication.
/////
///// Admin bootstrap logic (no DbSeeder required):
/////   When the incoming credentials exactly match AdminAccount:Email and
/////   AdminAccount:Password from configuration, and the admin account does not
/////   yet exist in the database, the handler creates it (with the Admin role)
/////   before proceeding with the normal login flow.
/////
/////   This means the very first login with admin credentials automatically
/////   provisions the account — no seed step needed.
///// </summary>
//public class LoginCommandHandler : IRequestHandler<LoginCommand, ApiResponse<AuthResultDto>>
//{
//    private readonly UserManager<ApplicationUser> _userManager;
//    private readonly ITokenService _tokenService;
//    private readonly IUnitOfWork _uow;
//    private readonly IMapper _mapper;
//    private readonly IConfiguration _config;

//    public LoginCommandHandler(
//        UserManager<ApplicationUser> userManager,
//        ITokenService tokenService,
//        IUnitOfWork uow,
//        IMapper mapper,
//        IConfiguration config)
//    {
//        _userManager = userManager;
//        _tokenService = tokenService;
//        _uow = uow;
//        _mapper = mapper;
//        _config = config;
//    }

//    public async Task<ApiResponse<AuthResultDto>> Handle(
//        LoginCommand request,
//        CancellationToken cancellationToken)
//    {
//        // ── Step 1: Admin bootstrap ───────────────────────────────────────────
//        // If the caller is using the configured admin credentials and the account
//        // does not exist yet, create it now (first-run provisioning).
//        await EnsureAdminExistsIfAdminCredentialsAsync(request.Email, request.Password);

//        // ── Step 2: Look up the user ──────────────────────────────────────────
//        var user = await _userManager.FindByEmailAsync(request.Email);

//        if (user == null || !user.IsActive)
//            return ApiResponse<AuthResultDto>.Fail("Invalid credentials.", 401);

//        // ── Step 3: Verify password ───────────────────────────────────────────
//        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
//        if (!passwordValid)
//            return ApiResponse<AuthResultDto>.Fail("Invalid credentials.", 401);

//        // ── Step 4: Update last-login timestamp ───────────────────────────────
//        user.CreatedAt = DateTime.UtcNow;
//        await _userManager.UpdateAsync(user);

//        // ── Step 5: Generate tokens ───────────────────────────────────────────
//        var accessToken = await _tokenService.GenerateAccessTokenAsync(user);
//        var refreshToken = _tokenService.GenerateRefreshToken();

//        // Persist the refresh token
//        var expiryDays = int.Parse(_config["Jwt:RefreshTokenExpiryDays"] ?? "7");
//        var refreshTokenEntity = new RefreshToken
//        {
//            Token = refreshToken,
//            UserId = user.Id,
//            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays)
//        };

//        await _uow.RefreshTokens.AddAsync(refreshTokenEntity, cancellationToken);
//        await _uow.SaveChangesAsync(cancellationToken);

//        // ── Step 6: Build the response ────────────────────────────────────────
//        var roles = await _userManager.GetRolesAsync(user);
//        var dto = _mapper.Map<UserDto>(user);
//        dto.Roles = roles.ToList();

//        var expiryMinutes = int.Parse(_config["Jwt:AccessTokenExpiryMinutes"] ?? "60");

//        return ApiResponse<AuthResultDto>.Ok(new AuthResultDto
//        {
//            AccessToken = accessToken,
//            RefreshToken = refreshToken,
//            AccessTokenExpiry = DateTime.UtcNow.AddMinutes(expiryMinutes),
//            User = dto
//        }, "Login successful.");
//    }

//    // ─────────────────────────────────────────────────────────────────────────
//    // Private helpers
//    // ─────────────────────────────────────────────────────────────────────────

//    /// <summary>
//    /// Checks whether the supplied credentials match the configured admin account.
//    /// If they do and the account doesn't exist yet, creates it with the Admin role.
//    /// This is the ONLY place admin account creation happens — no DbSeeder needed.
//    /// </summary>
//    private async Task EnsureAdminExistsIfAdminCredentialsAsync(
//        string email, string password)
//    {
//        var adminEmail = _config["AdminAccount:Email"] ?? string.Empty;
//        var adminPassword = _config["AdminAccount:Password"] ?? string.Empty;

//        // Only proceed when both email AND password match the config exactly.
//        // This prevents a random user from accidentally triggering admin creation.
//        if (!string.Equals(email, adminEmail, StringComparison.OrdinalIgnoreCase))
//            return;

//        if (password != adminPassword)
//            return;

//        // If the account already exists there is nothing to do.
//        var existing = await _userManager.FindByEmailAsync(adminEmail);
//        if (existing != null)
//        {
//            // Make sure the Admin role is assigned even if it was lost somehow.
//            if (!await _userManager.IsInRoleAsync(existing, "Admin"))
//                await _userManager.AddToRoleAsync(existing, "Admin");

//            return;
//        }

//        // First-time login with admin credentials → create the account now.
//        var adminUser = new ApplicationUser
//        {
//            FirstName = _config["AdminAccount:FirstName"] ?? "System",
//            LastName = _config["AdminAccount:LastName"] ?? "Admin",
//            Email = adminEmail,
//            UserName = adminEmail,
//            EmailConfirmed = true,
//            IsActive = true,
//            PreferredLanguage = "en"
//        };

//        var createResult = await _userManager.CreateAsync(adminUser, adminPassword);

//        if (!createResult.Succeeded)
//        {
//            // Surface identity errors so the caller gets a clear message
//            var errors = string.Join("; ",
//                createResult.Errors.Select(e => e.Description));

//            throw new InvalidOperationException(
//                $"Auto-provisioning of the admin account failed: {errors}");
//        }

//        await _userManager.AddToRoleAsync(adminUser, "Admin");
//    }
//}
using AutoMapper;
using FoodOrdering.Application.Common.Models;
using FoodOrdering.Application.Features.Auth.DTOs;
using FoodOrdering.Domain.Entities;
using FoodOrdering.Domain.Interfaces;
using FoodOrdering.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FoodOrdering.Application.Features.Auth.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, ApiResponse<AuthResultDto>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;   // ← added
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly IConfiguration _config;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,                  // ← added
        ITokenService tokenService,
        IUnitOfWork uow,
        IMapper mapper,
        IConfiguration config,
        ILogger<LoginCommandHandler> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _uow = uow;
        _mapper = mapper;
        _config = config;
        _logger = logger;
    }

    public async Task<ApiResponse<AuthResultDto>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Step 1: Admin bootstrap — auto-create admin on first login
            await EnsureAdminExistsIfAdminCredentialsAsync(request.Email, request.Password);

            // Step 2: Look up the user
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !user.IsActive)
                return ApiResponse<AuthResultDto>.Fail("Invalid credentials.", 401);

            // Step 3: Verify password
            var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!passwordValid)
                return ApiResponse<AuthResultDto>.Fail("Invalid credentials.", 401);

            // Step 4: Generate tokens
            var accessToken = await _tokenService.GenerateAccessTokenAsync(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            // Step 5: Persist refresh token
            // FIX: int.Parse crashes when config value is missing on the host.
            // Always use TryParse with a safe default.
            var expiryDays = ParseConfigInt(_config["Jwt:RefreshTokenExpiryDays"], 7);

            await _uow.RefreshTokens.AddAsync(new RefreshToken
            {
                Token = refreshToken,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(expiryDays)
            }, cancellationToken);

            await _uow.SaveChangesAsync(cancellationToken);

            // Step 6: Build response
            var roles = await _userManager.GetRolesAsync(user);
            var dto = _mapper.Map<UserDto>(user);
            dto.Roles = roles.ToList();

            var expiryMinutes = ParseConfigInt(_config["Jwt:AccessTokenExpiryMinutes"], 60);

            return ApiResponse<AuthResultDto>.Ok(new AuthResultDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiry = DateTime.UtcNow.AddMinutes(expiryMinutes),
                User = dto
            }, "Login successful.");
        }
        catch (InvalidOperationException)
        {
            // Re-throw admin-provisioning errors as-is (they have a clear message)
            throw;
        }
        catch (Exception ex)
        {
            // Any other unhandled error (missing config, DB down, etc.)
            // → log it and return a clean 500 instead of leaking a stack trace
            _logger.LogError(ex, "Unexpected error during login for {Email}", request.Email);
            return ApiResponse<AuthResultDto>.Fail(
                "An unexpected error occurred. Please try again.", 500);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Admin bootstrap
    // ─────────────────────────────────────────────────────────────────────────

    private async Task EnsureAdminExistsIfAdminCredentialsAsync(string email, string password)
    {
        var adminEmail = _config["AdminAccount:Email"] ?? string.Empty;
        var adminPassword = _config["AdminAccount:Password"] ?? string.Empty;

        // FIX: If the host environment variables are not set, log a warning
        // and skip silently — do NOT crash the login endpoint for everyone.
        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            _logger.LogWarning(
                "AdminAccount:Email or AdminAccount:Password is not set in configuration. " +
                "Admin auto-provisioning is disabled.");
            return;
        }

        // Only act when the caller is using the configured admin credentials
        if (!string.Equals(email, adminEmail, StringComparison.OrdinalIgnoreCase))
            return;

        if (password != adminPassword)
            return;

        // FIX: On a fresh hosted DB the Identity roles do not exist yet
        // (no seeder ran). Assigning a non-existent role throws a FK violation.
        // Ensure both roles exist before doing anything else.
        await EnsureRoleExistsAsync("Admin");
        await EnsureRoleExistsAsync("Customer");

        var existing = await _userManager.FindByEmailAsync(adminEmail);
        if (existing != null)
        {
            // Account exists — just make sure it still has the Admin role
            if (!await _userManager.IsInRoleAsync(existing, "Admin"))
                await _userManager.AddToRoleAsync(existing, "Admin");
            return;
        }

        // First login with admin credentials → create the account
        var adminUser = new ApplicationUser
        {
            FirstName = _config["AdminAccount:FirstName"] ?? "System",
            LastName = _config["AdminAccount:LastName"] ?? "Admin",
            Email = adminEmail,
            UserName = adminEmail,
            EmailConfirmed = true,
            IsActive = true,
            PreferredLanguage = "en"
        };

        var result = await _userManager.CreateAsync(adminUser, adminPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            _logger.LogError("Admin auto-provisioning failed: {Errors}", errors);
            throw new InvalidOperationException($"Admin account creation failed: {errors}");
        }

        await _userManager.AddToRoleAsync(adminUser, "Admin");
        _logger.LogInformation("Admin account auto-provisioned for {Email}", adminEmail);
    }

    private async Task EnsureRoleExistsAsync(string roleName)
    {
        if (!await _roleManager.RoleExistsAsync(roleName))
            await _roleManager.CreateAsync(new IdentityRole(roleName));
    }

    // FIX: Never use int.Parse on config values — use TryParse with a default.
    // int.Parse throws FormatException when the key is missing on the host.
    private static int ParseConfigInt(string? value, int defaultValue)
        => int.TryParse(value, out var parsed) ? parsed : defaultValue;
}