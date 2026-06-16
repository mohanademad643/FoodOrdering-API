using AutoMapper;
using FoodOrdering.Application.Common.Models;
using FoodOrdering.Application.Features.Reviews.DTOs;
using FoodOrdering.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace FoodOrdering.Application.Features.Reviews.Oueries.GetAllReviews
{
    public class GetAllReviewsQueryHandler
      : IRequestHandler<GetAllReviewsQuery, ApiResponse<PagedResult<ReviewDto>>>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public GetAllReviewsQueryHandler(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<ApiResponse<PagedResult<ReviewDto>>> Handle(
            GetAllReviewsQuery request,
            CancellationToken cancellationToken)
        {
            var query = _uow.Reviews
                .Query()
                .Include(r => r.User)
                .Include(r => r.Product)
                .AsQueryable();

            if (request.IsApproved.HasValue)
                query = query.Where(r => r.IsApproved == request.IsApproved.Value);

            if (request.Rating.HasValue)
                query = query.Where(r => r.Rating == request.Rating.Value);

            if (request.StartDate.HasValue)
                query = query.Where(r => r.CreatedAt >= request.StartDate.Value);

            if (request.EndDate.HasValue)
                query = query.Where(r => r.CreatedAt < request.EndDate.Value.Date.AddDays(1));

         
            var totalCount = await query.CountAsync(cancellationToken);

            var reviews = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            return ApiResponse<PagedResult<ReviewDto>>.Ok(new PagedResult<ReviewDto>
            {
                Items = _mapper.Map<IEnumerable<ReviewDto>>(reviews),
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            });
        }
    }
}
