using AutoMapper;
using FoodOrdering.Application.Common.Models;
using FoodOrdering.Application.Features.Contacts.DTOs;
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

            if (request.IsRead.HasValue)
                query = query.Where(c => c.IsRead == request.IsRead.Value);

            if (request.IsReplied.HasValue)
                query = query.Where(c => c.IsReplied == request.IsReplied.Value);

            var totalCount = await query.CountAsync(cancellationToken);

            var contacts = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            return ApiResponse<PagedResult<ContactDto>>.Ok(new PagedResult<ContactDto>
            {
                Items = _mapper.Map<IEnumerable<ContactDto>>(contacts),
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            });
        }
    }
}
