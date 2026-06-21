//using AutoMapper;
//using FoodOrdering.Application.Common.Models;
//using FoodOrdering.Application.Features.Contacts.DTOs;
//using FoodOrdering.Domain.Interfaces;
//using MediatR;
//using Microsoft.EntityFrameworkCore;

//namespace FoodOrdering.Application.Features.Contacts.Queries.GetAllContacts
//{
//    public class GetAllContactsQueryHandler
//       : IRequestHandler<GetAllContactsQuery, ApiResponse<PagedResult<ContactDto>>>
//    {
//        private readonly IUnitOfWork _uow;
//        private readonly IMapper _mapper;

//        public GetAllContactsQueryHandler(IUnitOfWork uow, IMapper mapper)
//        {
//            _uow = uow;
//            _mapper = mapper;
//        }

//        public async Task<ApiResponse<PagedResult<ContactDto>>> Handle(
//            GetAllContactsQuery request,
//            CancellationToken cancellationToken)
//        {
//            var query = _uow.Contacts.Query().AsQueryable();

//            if (request.IsRead.HasValue)
//                query = query.Where(c => c.IsRead == request.IsRead.Value);

//            if (request.IsReplied.HasValue)
//                query = query.Where(c => c.IsReplied == request.IsReplied.Value);
//            if (request.StartDate.HasValue)
//            {
//                var start = request.StartDate.Value.ToDateTime(TimeOnly.MinValue);
//                query = query.Where(r => r.CreatedAt >= start);
//            }
//            if (!string.IsNullOrWhiteSpace(request.MessageContent))
//            {
//                var productWords = request.MessageContent
//                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
//                    .Select(w => w.ToLower());
//                query = query.Where(r => productWords.All(word =>
//                    r.Subject.ToLower().Contains(word) ||
//                    r.Message.ToLower().Contains(word)));
//            }

//            if (!string.IsNullOrWhiteSpace(request.FullNameSearchTerm))
//            {
//                var nameWords = request.FullNameSearchTerm
//                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
//                    .Select(w => w.ToLower());

//                query = query.Where(r => nameWords.All(word =>
//                    r.FullName.ToLower().Contains(word)));
//            }
//            if (!string.IsNullOrWhiteSpace(request.Email))
//            {
//                var nameWords = request.Email
//                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
//                    .Select(w => w.ToLower());

//                query = query.Where(r => nameWords.All(word =>
//                    r.Email.ToLower().Contains(word)));
//            }
//            if (request.EndDate.HasValue)
//            {
//                var end = request.EndDate.Value.ToDateTime(TimeOnly.MinValue).AddDays(1);
//                query = query.Where(r => r.CreatedAt < end);
//            }
//            var totalCount = await query.CountAsync(cancellationToken);

//            var contacts = await query
//                .OrderByDescending(c => c.CreatedAt)
//                .Skip((request.Page - 1) * request.PageSize)
//                .Take(request.PageSize)
//                .ToListAsync(cancellationToken);

//            return ApiResponse<PagedResult<ContactDto>>.Ok(new PagedResult<ContactDto>
//            {
//                Items = _mapper.Map<IEnumerable<ContactDto>>(contacts),
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
using FoodOrdering.Application.Features.Contacts.DTOs;
using FoodOrdering.Domain.Entities;
using FoodOrdering.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodOrdering.Application.Features.Contacts.Queries.GetAllContacts
{
    public class GetAllContactsQueryHandler
        : IRequestHandler<GetAllContactsQuery, ApiResponse<PagedResult<ContactDto>>>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public GetAllContactsQueryHandler(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<ApiResponse<PagedResult<ContactDto>>> Handle(
            GetAllContactsQuery request,
            CancellationToken cancellationToken)
        {
            var query = _uow.Contacts.Query().AsQueryable();

            IFilterRule<Contact>[] rules =
            {
                new BooleanFilterRule<Contact>(
                    c => c.IsRead,
                    request.IsRead),

                new BooleanFilterRule<Contact>(
                    c => c.IsReplied,
                    request.IsReplied),

                new DateRangeFilterRule<Contact>(
                    c => c.CreatedAt,
                    request.StartDate,
                    request.EndDate),

                new KeywordSearchFilterRule<Contact>(
                    request.MessageContent,
                    word => c => c.Subject.ToLower().Contains(word) ||
                                 c.Message.ToLower().Contains(word)),

                new KeywordSearchFilterRule<Contact>(
                    request.FullNameSearchTerm,
                    word => c => c.FullName.ToLower().Contains(word)),

                new KeywordSearchFilterRule<Contact>(
                    request.Email,
                    word => c => c.Email.ToLower().Contains(word)),
            };

            foreach (var rule in rules.Where(r => r.IsApplicable()))
                query = rule.Apply(query);

            var paged = await PaginationHelper.ToPagedResultAsync(
                query,
                request.Page,
                request.PageSize,
                entities => _mapper.Map<IEnumerable<ContactDto>>(entities),
                q => q.OrderByDescending(c => c.CreatedAt),
                cancellationToken);

            return ApiResponse<PagedResult<ContactDto>>.Ok(paged);
        }
    }
}