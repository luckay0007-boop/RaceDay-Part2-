using RaceDay.Api.DTOs.Results;

namespace RaceDay.Api.Services;

public interface IResultService
{
    Task<ResultResponseDto> SubmitResultAsync(SubmitResultDto dto);
    Task<List<ResultResponseDto>> GetResultsByEventAsync(int eventId);
}
