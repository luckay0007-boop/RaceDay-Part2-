using RaceDay.Api.DTOs.Results;

namespace RaceDay.Api.Services;

public interface IResultService
{
    Task<ResultResponseDto> SubmitResultAsync(int organiserId, SubmitResultDto dto);
    Task<List<ResultResponseDto>> GetResultsByEventAsync(int eventId);
    Task<List<ResultResponseDto>> GetResultsByParticipantAsync(int participantId);
}
