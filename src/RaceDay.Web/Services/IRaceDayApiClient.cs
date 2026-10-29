using RaceDay.Web.Models;

namespace RaceDay.Web.Services;

public interface IRaceDayApiClient
{
    // Authentication
    Task<ApiResult<AuthResponseModel>> LoginAsync(LoginViewModel model, CancellationToken ct = default);
    Task<ApiResult<AuthResponseModel>> RegisterAsync(RegisterViewModel model, CancellationToken ct = default);

    // Events
    Task<ApiResult<List<EventModel>>> GetEventsAsync(
        string? search = null,
        string? type = null,
        string? city = null,
        DateOnly? from = null,
        DateOnly? to = null,
        bool upcomingOnly = false,
        bool publishedOnly = true,
        CancellationToken ct = default);
    Task<ApiResult<List<EventModel>>> GetMyOrganiserEventsAsync(CancellationToken ct = default);
    Task<ApiResult<EventModel>> GetEventByIdAsync(int eventId, CancellationToken ct = default);
    Task<ApiResult<EventModel>> CreateEventAsync(EventFormViewModel model, CancellationToken ct = default);
    Task<ApiResult<EventModel>> UpdateEventAsync(int eventId, EventFormViewModel model, CancellationToken ct = default);
    Task<ApiResult<EventModel>> UploadEventBannerAsync(int eventId, IFormFile bannerFile, CancellationToken ct = default);
    Task<ApiResult<bool>> DeleteEventAsync(int eventId, CancellationToken ct = default);

    // Categories
    Task<ApiResult<List<CategoryModel>>> GetCategoriesByEventAsync(int eventId, CancellationToken ct = default);
    Task<ApiResult<CategoryModel>> CreateCategoryAsync(CreateCategoryInputModel model, CancellationToken ct = default);
    Task<ApiResult<bool>> DeleteCategoryAsync(int categoryId, CancellationToken ct = default);

    // Enrolments
    Task<ApiResult<EnrolmentModel>> CreateEnrolmentAsync(int categoryId, CancellationToken ct = default);
    Task<ApiResult<List<EnrolmentModel>>> GetMyEnrolmentsAsync(CancellationToken ct = default);
    Task<ApiResult<List<EnrolmentModel>>> GetEnrolmentsByEventAsync(int eventId, CancellationToken ct = default);
    Task<ApiResult<EnrolmentModel>> UpdateEnrolmentStatusAsync(int enrolmentId, string status, CancellationToken ct = default);

    // Results
    Task<ApiResult<ResultModel>> SubmitResultAsync(int enrolmentId, TimeOnly? finishTime, int? position, string status, CancellationToken ct = default);
    Task<ApiResult<List<ResultModel>>> GetResultsByEventAsync(int eventId, CancellationToken ct = default);
    Task<ApiResult<List<ResultModel>>> GetMyResultsAsync(CancellationToken ct = default);

    // Profile
    Task<ApiResult<UserProfileModel>> GetMyProfileAsync(CancellationToken ct = default);
    Task<ApiResult<UserProfileModel>> UpdateMyProfileAsync(ProfileViewModel model, CancellationToken ct = default);
    Task<ApiResult<UserProfileModel>> UploadProfilePictureAsync(IFormFile pictureFile, CancellationToken ct = default);
}

