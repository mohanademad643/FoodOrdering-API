using FoodOrdering.Application.Common.Models;
using FoodOrdering.Application.Features.Auth.RevokeToken.Commands;
using FoodOrdering.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodOrdering.Application.Features.Auth.RevokeToken
{
    public class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;

        public RevokeTokenCommandHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
        {
            var token = await _uow.RefreshTokens.Query()
                .FirstOrDefaultAsync(t => t.Token == request.RefreshToken, cancellationToken);

            if (token == null || !token.IsActive)
                return ApiResponse<bool>.Fail("Token not found or already revoked.");

            token.IsRevoked = true;
            await _uow.RefreshTokens.UpdateAsync(token, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            return ApiResponse<bool>.Ok(true, "Token revoked.");
        }
    }
}
