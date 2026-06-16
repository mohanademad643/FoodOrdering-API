using AutoMapper;
using FoodOrdering.Application.Common.Models;
using FoodOrdering.Application.Features.Auth.DTOs;
using FoodOrdering.Domain.Entities;
using FoodOrdering.Domain.Interfaces;
using FoodOrdering.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FoodOrdering.Application.Features.Auth.RefreshTokens.Commands
{
    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, ApiResponse<AuthResultDto>>
    {
        private readonly ITokenService _tokenService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IConfiguration _config;

        public RefreshTokenCommandHandler(
            ITokenService tokenService,
            UserManager<ApplicationUser> userManager,
            IUnitOfWork uow,
            IMapper mapper,
            IConfiguration config)
        {
            _tokenService = tokenService;
            _userManager = userManager;
            _uow = uow;
            _mapper = mapper;
            _config = config;
        }

        public async Task<ApiResponse<AuthResultDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
            if (principal == null)
                return ApiResponse<AuthResultDto>.Fail("Invalid access token.", 401);

            var userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return ApiResponse<AuthResultDto>.Fail("Invalid token claims.", 401);

            var storedToken = await _uow.RefreshTokens.Query()
                .FirstOrDefaultAsync(t => t.Token == request.RefreshToken && t.UserId == userId, cancellationToken);

            if (storedToken == null || !storedToken.IsActive)
                return ApiResponse<AuthResultDto>.Fail("Invalid or expired refresh token.", 401);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.IsActive)
                return ApiResponse<AuthResultDto>.Fail("User not found.", 401);

            storedToken.IsRevoked = true;
            await _uow.RefreshTokens.UpdateAsync(storedToken, cancellationToken);

            var newAccessToken = await _tokenService.GenerateAccessTokenAsync(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken();
            var expiryDays = int.Parse(_config["Jwt:RefreshTokenExpiryDays"] ?? "7");

            var newRefreshTokenEntity = new RefreshToken
            {
                Token = newRefreshToken,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(expiryDays)
            };

            await _uow.RefreshTokens.AddAsync(newRefreshTokenEntity, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            var roles = await _userManager.GetRolesAsync(user);
            var dto = _mapper.Map<UserDto>(user);
            dto.Roles = roles.ToList();

            var expiryMinutes = int.Parse(_config["Jwt:AccessTokenExpiryMinutes"] ?? "60");

            return ApiResponse<AuthResultDto>.Ok(new AuthResultDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                AccessTokenExpiry = DateTime.UtcNow.AddMinutes(expiryMinutes),
                User = dto
            });
        }
    }
}
