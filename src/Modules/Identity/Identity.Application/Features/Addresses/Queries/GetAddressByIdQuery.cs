using FluentValidation;
using Identity.Application.DTOs.Response;
using Identity.Application.Interfaces;
using MediatR;
using Shared.Application.Interfaces;
using Shared.Application.Models;

namespace Identity.Application.Features.Addresses.Queries;

public record GetAddressByIdQuery(Guid AddressId) : IRequest<Result<AddressDto>>;

public class GetAddressByIdQueryValidator : AbstractValidator<GetAddressByIdQuery>
{
    public GetAddressByIdQueryValidator()
    {
        RuleFor(x => x.AddressId)
            .NotEmpty().WithMessage("ID địa chỉ không được để trống.");
    }
}

public class GetAddressByIdQueryHandler : IRequestHandler<GetAddressByIdQuery, Result<AddressDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;

    public GetAddressByIdQueryHandler(
        IUserRepository userRepository,
        ICurrentUser currentUser)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<AddressDto>> Handle(GetAddressByIdQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return Result<AddressDto>.Failure("Bạn chưa đăng nhập.");

        // Lọc thẳng theo cả AddressId lẫn UserId ngay trong SQL - không cần tải cả User Aggregate.
        var address = await _userRepository.GetAddressByIdForUserAsync(_currentUser.UserId, request.AddressId, cancellationToken);

        if (address == null)
        {
            return Result<AddressDto>.Failure("Địa chỉ không tồn tại hoặc bạn không có quyền truy cập.");
        }

        var dto = new AddressDto(
            Id: address.Id,
            RecipientName: address.RecipientName,
            Phone: address.RecipientPhone.Value,
            FullAddress: address.FullAddress,
            AddressDetail: address.AddressDetail,
            Province: address.Province,
            District: address.District ,
            Commune: address.Commune,
            Latitude: address.Location?.Latitude,
            Longitude: address.Location?.Longitude,
            IsDefault: address.IsDefault
        );

        return Result<AddressDto>.Success(dto);
    }
}