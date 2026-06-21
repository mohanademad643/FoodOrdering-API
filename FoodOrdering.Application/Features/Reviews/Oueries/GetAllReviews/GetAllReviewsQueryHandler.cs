using AutoMapper;
using FoodOrdering.Application.Common.Filtering;
using FoodOrdering.Application.Common.Filtering.Rules;
using FoodOrdering.Application.Common.Models;
using FoodOrdering.Application.Features.Reviews.DTOs;
using FoodOrdering.Domain.Entities;
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

            //if (request.IsApproved.HasValue)
            //    query = query.Where(r => r.IsApproved == request.IsApproved.Value);

            //if (request.Rating.HasValue)
            //    query = query.Where(r => r.Rating == request.Rating.Value);

            //if (!string.IsNullOrWhiteSpace(request.ProductSearchTerm))
            //{
            //    var productWords = request.ProductSearchTerm
            //        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            //        .Select(w => w.ToLower());
            //    query = query.Where(r => productWords.All(word =>
            //        r.Product.NameAr.ToLower().Contains(word) ||
            //        r.Product.NameEn.ToLower().Contains(word)));
            //}

            //if (!string.IsNullOrWhiteSpace(request.FullNameSearchTerm))
            //{
            //    var nameWords = request.FullNameSearchTerm
            //        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            //        .Select(w => w.ToLower());

            //    query = query.Where(r => nameWords.All(word =>
            //        r.User.FirstName.ToLower().Contains(word) ||
            //        r.User.LastName.ToLower().Contains(word)));
            //}

            //if (request.StartDate.HasValue)
            //    {
            //        var start = request.StartDate.Value.ToDateTime(TimeOnly.MinValue);
            //        query = query.Where(r => r.CreatedAt >= start);
            //    }

            //if (request.EndDate.HasValue)
            //    {
            //        var end = request.EndDate.Value.ToDateTime(TimeOnly.MinValue).AddDays(1);
            //        query = query.Where(r => r.CreatedAt < end);
            //    }


            //var totalCount = await query.CountAsync(cancellationToken);

            //var reviews = await query
            //    .OrderByDescending(r => r.CreatedAt)
            //    .Skip((request.Page - 1) * request.PageSize)
            //    .Take(request.PageSize)
            //    .ToListAsync(cancellationToken);
            //return ApiResponse<PagedResult<ReviewDto>>.Ok(new PagedResult<ReviewDto>
            //{
            //    Items = _mapper.Map<IEnumerable<ReviewDto>>(reviews),
            //    TotalCount = totalCount,
            //    Page = request.Page,
            //    PageSize = request.PageSize
            //});
            IFilterRule<Review>[] rules =
          {
                new BooleanFilterRule<Review>(
                    r => r.IsApproved,
                    request.IsApproved),

                new EqualityFilterRule<Review, int>(
                    r => r.Rating,
                    request.Rating),

                new DateRangeFilterRule<Review>(
                    r => r.CreatedAt,
                    request.StartDate,
                    request.EndDate),

                new KeywordSearchFilterRule<Review>(
                    request.ProductSearchTerm,
                    word => r => r.Product.NameAr.ToLower().Contains(word) ||
                                 r.Product.NameEn.ToLower().Contains(word)),

                new KeywordSearchFilterRule<Review>(
                    request.FullNameSearchTerm,
                    word => r => r.User.FirstName.ToLower().Contains(word) ||
                                 r.User.LastName.ToLower().Contains(word)),
            };

            foreach (var rule in rules.Where(r => r.IsApplicable()))
                query = rule.Apply(query);

            var paged = await PaginationHelper.ToPagedResultAsync(
               query,
               request.Page,
               request.PageSize,
               entities => _mapper.Map<IEnumerable<ReviewDto>>(entities),
               q => q.OrderByDescending(r => r.CreatedAt),
               cancellationToken);

            return ApiResponse<PagedResult<ReviewDto>>.Ok(paged);

          
        }
    }
}
