//using AutoMapper;
//using FoodOrdering.Application.Common.Models;
//using FoodOrdering.Application.Features.Products.DTOs;
//using FoodOrdering.Domain.Entities;
//using FoodOrdering.Domain.Interfaces;
//using MediatR;
//using Microsoft.EntityFrameworkCore;

//namespace FoodOrdering.Application.Features.Products.Queries.GetAllProducts
//{
//    public class GetAllProductsHandler : IRequestHandler<GetAllProductsQuery, ApiResponse<PagedResult<ReturnProductDto>>>
//    {
//        private readonly IUnitOfWork _uow;
//        private readonly IMapper _mapper;

//        public GetAllProductsHandler(IUnitOfWork uow, IMapper mapper) { _uow = uow; _mapper = mapper; }

//        public async Task<ApiResponse<PagedResult<ReturnProductDto>>> Handle(GetAllProductsQuery request, CancellationToken cancellationToken)
//        {
//            IQueryable<Product> query = _uow.Products.Query()
//                .Include(p => p.Category)
//                .Include(p => p.Reviews
//                .Where(r => r.IsApproved)).AsQueryable();

//            query = query.Where(p => p.IsAvailable == request.AvailableOnly);
//            //if (request.AvailableOnly)
//            //    query = query.Where(p => p.IsAvailable);

//            if (request.CategoryId.HasValue)
//                query = query.Where(p => p.CategoryId == request.CategoryId.Value);


//            if (!string.IsNullOrEmpty(request.SearchTerm))
//            {
//                var searchWords = request.SearchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
//                query = query.Where(p => searchWords.All(word =>
//                    p.NameAr.ToLower().Contains(word.ToLower()) ||
//                    p.NameEn.ToLower().Contains(word.ToLower()) ||
//                    p.DescriptionAr.ToLower().Contains(word.ToLower()) ||
//                    p.DescriptionEn.ToLower().Contains(word.ToLower())
//                ));
//            }
//            if (request.Rating.HasValue)
//                query = query.Where(p => p.Price == request.Rating.Value);

//            if (request.MinPrice.HasValue)
//                query = query.Where(p => p.Price >= request.MinPrice.Value);

//            if (request.MaxPrice.HasValue)
//                query = query.Where(p => p.Price <= request.MaxPrice.Value);

//            query = request.SortOrder?.ToLower() == "desc"
//                ? query.OrderByDescending(p => p.Price)
//                : query.OrderBy(p => p.Price);

//            var totalCount = await query.CountAsync(cancellationToken);
//            var products = await query
//                .Skip((request.Page - 1) * request.PageSize)
//                .Take(request.PageSize)
//                .ToListAsync(cancellationToken);

//            return ApiResponse<PagedResult<ReturnProductDto>>.Ok(new PagedResult<ReturnProductDto>
//            {
//                Items = _mapper.Map<IEnumerable<ReturnProductDto>>(products),
//                TotalCount = totalCount,
//                Page = request.Page,
//                PageSize = request.PageSize
//            });
//        }
//    }

//}
using AutoMapper;
using FoodOrdering.Application.Common.Filtering;
using FoodOrdering.Application.Common.Filtering.Rules;
using FoodOrdering.Application.Common.Models;
using FoodOrdering.Application.Features.Products.DTOs;
using FoodOrdering.Domain.Entities;
using FoodOrdering.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodOrdering.Application.Features.Products.Queries.GetAllProducts
{
    public class GetAllProductsHandler : IRequestHandler<GetAllProductsQuery, ApiResponse<PagedResult<ReturnProductDto>>>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public GetAllProductsHandler(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<ApiResponse<PagedResult<ReturnProductDto>>> Handle(
            GetAllProductsQuery request,
            CancellationToken cancellationToken)
        {
            IQueryable<Product> query = _uow.Products.Query()
                .Include(p => p.Category)
                .Include(p => p.Reviews.Where(r => r.IsApproved))
                .AsQueryable();

            IFilterRule<Product>[] rules =
            {
                
                new BooleanFilterRule<Product>(
                    p => p.IsAvailable,
                    request.AvailableOnly),

                new EqualityFilterRule<Product, Guid>(
                    p => p.CategoryId,
                    request.CategoryId),
                 new EqualityFilterRule<Product, double>(
                    p => p.AverageRating,
                    request.Rating),

                new RangeFilterRule<Product, decimal>(
                    p => p.Price,
                    request.MinPrice,
                    request.MaxPrice),

                new KeywordSearchFilterRule<Product>(
                    request.SearchTerm,
                    word => p => p.NameAr.ToLower().Contains(word) ||
                                 p.NameEn.ToLower().Contains(word) ||
                                 p.DescriptionAr.ToLower().Contains(word) ||
                                 p.DescriptionEn.ToLower().Contains(word)),
            };

            foreach (var rule in rules.Where(r => r.IsApplicable()))
                query = rule.Apply(query);

            query = request.SortOrder?.ToLower() == "desc"
                ? query.OrderByDescending(p => p.Price)
                : query.OrderBy(p => p.Price);

            var paged = await PaginationHelper.ToPagedResultAsync(
                query,
                request.Page,
                request.PageSize,
                entities => _mapper.Map<IEnumerable<ReturnProductDto>>(entities),
                q => (IOrderedQueryable<Product>)q, // already sorted above
                cancellationToken);

            return ApiResponse<PagedResult<ReturnProductDto>>.Ok(paged);
        }
    }
}