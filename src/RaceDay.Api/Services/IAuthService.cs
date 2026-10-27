using RaceDay.Api.DTOs.Auth;

namespace RaceDay.Api.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterUserDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
}
