using RaceDay.Api.DTOs.Enrolments;

namespace RaceDay.Api.Services;

public interface IEnrolmentService
{
    Task<EnrolmentResponseDto> CreateEnrolmentAsync(int participantId, CreateEnrolmentDto dto);
    Task<List<EnrolmentResponseDto>> GetEnrolmentsByUserAsync(int userId);
}
