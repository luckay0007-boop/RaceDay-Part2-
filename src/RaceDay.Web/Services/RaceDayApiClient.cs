using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;

namespace RaceDay.Web.Services;

/// <summary>
/// Typed HTTP client that communicates exclusively with the Part 2 RaceDay REST API.
/// No database access or domain business logic resides in the MVC application.
/// </summary>
public class RaceDayApiClient : IRaceDayApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly LocalApiCompanion _companion;
    private readonly ILogger<RaceDayApiClient> _logger;

    public RaceDayApiClient(
        HttpClient http,
        IHttpContextAccessor httpContextAccessor,
        LocalApiCompanion companion,
        ILogger<RaceDayApiClient> logger)
    {
        _http = http;
        _httpContextAccessor = httpContextAccessor;
        _companion = companion;
        _logger = logger;
    }

    public Task<ApiResult<AuthResponseModel>> LoginAsync(LoginViewModel model, CancellationToken ct = default) =>
        SendJsonAsync<AuthResponseModel>(HttpMethod.Post, "/api/Auth/login", new
        {
            model.Email,
            model.Password
        }, ct);

    public Task<ApiResult<AuthResponseModel>> RegisterAsync(RegisterViewModel model, CancellationToken ct = default) =>
        SendJsonAsync<AuthResponseModel>(HttpMethod.Post, "/api/Auth/register", new
        {
            model.FirstName,
            model.LastName,
            model.Email,
            model.Password,
            model.Role,
            model.DateOfBirth,
            model.Gender,
            model.PhoneNumber,
            model.City,
            model.Province
        }, ct);

    public Task<ApiResult<List<EventModel>>> GetEventsAsync(
        string? search = null,
        string? type = null,
        string? city = null,
        DateOnly? from = null,
        DateOnly? to = null,
        bool upcomingOnly = false,
        bool publishedOnly = true,
        CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?>();
        if (!string.IsNullOrWhiteSpace(search)) query["search"] = search.Trim();
        if (!string.IsNullOrWhiteSpace(type)) query["type"] = type.Trim();
        if (!string.IsNullOrWhiteSpace(city)) query["city"] = city.Trim();
        if (from.HasValue) query["from"] = from.Value.ToString("yyyy-MM-dd");
        if (to.HasValue) query["to"] = to.Value.ToString("yyyy-MM-dd");
        if (upcomingOnly) query["upcomingOnly"] = "true";
        if (publishedOnly) query["publishedOnly"] = "true";

        var url = QueryHelpers.AddQueryString("/api/Events", query);
        return GetAsync<List<EventModel>>(url, ct);
    }

    public Task<ApiResult<List<EventModel>>> GetMyOrganiserEventsAsync(CancellationToken ct = default) =>
        GetAsync<List<EventModel>>("/api/Events/mine", ct);

    public Task<ApiResult<EventModel>> GetEventByIdAsync(int eventId, CancellationToken ct = default) =>
        GetAsync<EventModel>($"/api/Events/{eventId}", ct);

    public Task<ApiResult<EventModel>> CreateEventAsync(EventFormViewModel model, CancellationToken ct = default) =>
        SendJsonAsync<EventModel>(HttpMethod.Post, "/api/Events", new
        {
            model.Name,
            model.Description,
            model.EventDate,
            model.StartTime,
            model.Location,
            model.City,
            model.Province,
            model.Distance,
            model.EventType
        }, ct);

    public Task<ApiResult<EventModel>> UpdateEventAsync(int eventId, EventFormViewModel model, CancellationToken ct = default) =>
        SendJsonAsync<EventModel>(HttpMethod.Put, $"/api/Events/{eventId}", new
        {
            model.Name,
            model.Description,
            model.EventDate,
            model.StartTime,
            model.Location,
            model.City,
            model.Province,
            model.Distance,
            model.EventType,
            model.Status
        }, ct);

    public Task<ApiResult<EventModel>> UploadEventBannerAsync(int eventId, IFormFile bannerFile, CancellationToken ct = default) =>
        UploadFileAsync<EventModel>($"/api/Events/{eventId}/banner", bannerFile, ct);

    public Task<ApiResult<bool>> DeleteEventAsync(int eventId, CancellationToken ct = default) =>
        DeleteAsync($"/api/Events/{eventId}", ct);

    public Task<ApiResult<List<CategoryModel>>> GetCategoriesByEventAsync(int eventId, CancellationToken ct = default) =>
        GetAsync<List<CategoryModel>>($"/api/Categories/event/{eventId}", ct);

    public Task<ApiResult<CategoryModel>> CreateCategoryAsync(CreateCategoryInputModel model, CancellationToken ct = default) =>
        SendJsonAsync<CategoryModel>(HttpMethod.Post, "/api/Categories", new
        {
            model.EventId,
            model.Name,
            model.Distance,
            model.MaxParticipants,
            model.EntryFee
        }, ct);

    public Task<ApiResult<bool>> DeleteCategoryAsync(int categoryId, CancellationToken ct = default) =>
        DeleteAsync($"/api/Categories/{categoryId}", ct);

    public Task<ApiResult<EnrolmentModel>> CreateEnrolmentAsync(int categoryId, CancellationToken ct = default) =>
        SendJsonAsync<EnrolmentModel>(HttpMethod.Post, "/api/Enrolments", new { CategoryId = categoryId }, ct);

    public Task<ApiResult<List<EnrolmentModel>>> GetMyEnrolmentsAsync(CancellationToken ct = default) =>
        GetAsync<List<EnrolmentModel>>("/api/Enrolments/my", ct);

    public Task<ApiResult<List<EnrolmentModel>>> GetEnrolmentsByEventAsync(int eventId, CancellationToken ct = default) =>
        GetAsync<List<EnrolmentModel>>($"/api/Enrolments/event/{eventId}", ct);

    public Task<ApiResult<EnrolmentModel>> UpdateEnrolmentStatusAsync(int enrolmentId, string status, CancellationToken ct = default) =>
        SendJsonAsync<EnrolmentModel>(HttpMethod.Put, $"/api/Enrolments/{enrolmentId}/status", new { Status = status }, ct);

    public Task<ApiResult<ResultModel>> SubmitResultAsync(
        int enrolmentId,
        TimeOnly? finishTime,
        int? position,
        string status,
        CancellationToken ct = default) =>
        SendJsonAsync<ResultModel>(HttpMethod.Post, "/api/Results", new
        {
            EnrolmentId = enrolmentId,
            FinishTime = finishTime,
            Position = position,
            Status = status
        }, ct);

    public Task<ApiResult<List<ResultModel>>> GetResultsByEventAsync(int eventId, CancellationToken ct = default) =>
        GetAsync<List<ResultModel>>($"/api/Results/event/{eventId}", ct);

    public Task<ApiResult<List<ResultModel>>> GetMyResultsAsync(CancellationToken ct = default) =>
        GetAsync<List<ResultModel>>("/api/Results/my", ct);

    public Task<ApiResult<UserProfileModel>> GetMyProfileAsync(CancellationToken ct = default) =>
        GetAsync<UserProfileModel>("/api/Profile/me", ct);

    public Task<ApiResult<UserProfileModel>> UpdateMyProfileAsync(ProfileViewModel model, CancellationToken ct = default) =>
        SendJsonAsync<UserProfileModel>(HttpMethod.Put, "/api/Profile/me", new
        {
            model.FirstName,
            model.LastName,
            model.DateOfBirth,
            model.Gender,
            model.PhoneNumber,
            model.City,
            model.Province
        }, ct);

    public Task<ApiResult<UserProfileModel>> UploadProfilePictureAsync(IFormFile pictureFile, CancellationToken ct = default) =>
        UploadFileAsync<UserProfileModel>("/api/Profile/me/picture", pictureFile, ct);

    private async Task<ApiResult<T>> GetAsync<T>(string relativeUrl, CancellationToken ct)
    {
        return await ExecuteWithRetryAsync<T>(() => CreateAuthorizedRequest(HttpMethod.Get, relativeUrl), ct);
    }

    private async Task<ApiResult<T>> SendJsonAsync<T>(HttpMethod method, string relativeUrl, object payload, CancellationToken ct)
    {
        return await ExecuteWithRetryAsync<T>(() =>
        {
            var req = CreateAuthorizedRequest(method, relativeUrl);
            req.Content = JsonContent.Create(payload);
            return req;
        }, ct);
    }

    private async Task<ApiResult<T>> UploadFileAsync<T>(string relativeUrl, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return ApiResult<T>.Fail("Please select a valid image file to upload.");

        await _companion.EnsureApiRunningAsync(ct);

        using var req = CreateAuthorizedRequest(HttpMethod.Post, relativeUrl);
        using var content = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        using var streamContent = new StreamContent(stream);

        if (!string.IsNullOrWhiteSpace(file.ContentType))
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

        var safeFileName = Path.GetFileName(file.FileName);
        content.Add(streamContent, "file", safeFileName);
        req.Content = content;

        return await ExecuteAsync<T>(req, ct);
    }

    private async Task<ApiResult<bool>> DeleteAsync(string relativeUrl, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var req = CreateAuthorizedRequest(HttpMethod.Delete, relativeUrl);
            try
            {
                using var res = await _http.SendAsync(req, ct);
                if (res.IsSuccessStatusCode)
                    return ApiResult<bool>.Ok(true, (int)res.StatusCode);

                var err = await ExtractErrorMessageAsync(res, ct);
                return ApiResult<bool>.Fail(err, (int)res.StatusCode);
            }
            catch (HttpRequestException ex) when (attempt == 0)
            {
                if (!await _companion.EnsureApiRunningAsync(ct))
                {
                    _logger.LogWarning(ex, "API DELETE request failed for {Url}", relativeUrl);
                    return ApiResult<bool>.Fail("Unable to reach the RaceDay API server. Please ensure the API is running.", 503);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "API DELETE request failed for {Url}", relativeUrl);
                return ApiResult<bool>.Fail("Unable to reach the RaceDay API server. Please ensure the API is running.", 503);
            }
        }

        return ApiResult<bool>.Fail("Unable to reach the RaceDay API server. Please ensure the API is running.", 503);
    }

    private HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string relativeUrl)
    {
        var req = new HttpRequestMessage(method, relativeUrl);
        var token = _httpContextAccessor.HttpContext?.Session.GetJwtToken();
        if (!string.IsNullOrWhiteSpace(token))
        {
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return req;
    }

    private async Task<ApiResult<T>> ExecuteWithRetryAsync<T>(Func<HttpRequestMessage> requestFactory, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var request = requestFactory();
            try
            {
                using var response = await _http.SendAsync(request, ct);
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
                    return data is not null
                        ? ApiResult<T>.Ok(data, (int)response.StatusCode)
                        : ApiResult<T>.Fail("Received an empty response from the server.", (int)response.StatusCode);
                }

                var error = await ExtractErrorMessageAsync(response, ct);
                return ApiResult<T>.Fail(error, (int)response.StatusCode);
            }
            catch (HttpRequestException ex) when (attempt == 0)
            {
                if (!await _companion.EnsureApiRunningAsync(ct))
                {
                    _logger.LogWarning(ex, "API request failed for {Method} {Uri}", request.Method, request.RequestUri);
                    return ApiResult<T>.Fail("Unable to connect to the RaceDay API service. Please verify the API is running and try again.", 503);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "API request failed for {Method} {Uri}", request.Method, request.RequestUri);
                return ApiResult<T>.Fail("Unable to connect to the RaceDay API service. Please verify the API is running and try again.", 503);
            }
        }

        return ApiResult<T>.Fail("Unable to connect to the RaceDay API service. Please verify the API is running and try again.", 503);
    }

    private async Task<ApiResult<T>> ExecuteAsync<T>(HttpRequestMessage request, CancellationToken ct)
    {
        try
        {
            using var response = await _http.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
                return data is not null
                    ? ApiResult<T>.Ok(data, (int)response.StatusCode)
                    : ApiResult<T>.Fail("Received an empty response from the server.", (int)response.StatusCode);
            }

            var error = await ExtractErrorMessageAsync(response, ct);
            return ApiResult<T>.Fail(error, (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API request failed for {Method} {Uri}", request.Method, request.RequestUri);
            return ApiResult<T>.Fail("Unable to connect to the RaceDay API service. Please verify the API is running and try again.", 503);
        }
    }

    private static async Task<string> ExtractErrorMessageAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!string.IsNullOrWhiteSpace(body))
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                if (root.TryGetProperty("detail", out var detailProp) && detailProp.ValueKind == JsonValueKind.String)
                {
                    var detail = detailProp.GetString();
                    if (!string.IsNullOrWhiteSpace(detail)) return detail;
                }

                if (root.TryGetProperty("errors", out var errorsProp) && errorsProp.ValueKind == JsonValueKind.Object)
                {
                    var messages = new List<string>();
                    foreach (var field in errorsProp.EnumerateObject())
                    {
                        if (field.Value.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in field.Value.EnumerateArray())
                            {
                                if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                                    messages.Add(item.GetString()!);
                            }
                        }
                    }
                    if (messages.Count > 0)
                        return string.Join(" ", messages);
                }

                if (root.TryGetProperty("title", out var titleProp) && titleProp.ValueKind == JsonValueKind.String)
                {
                    var title = titleProp.GetString();
                    if (!string.IsNullOrWhiteSpace(title)) return title;
                }
            }
        }
        catch
        {
            // Ignore malformed JSON error payload
        }

        return (int)response.StatusCode switch
        {
            400 => "Please check the information provided and try again.",
            401 => "Your session has expired or credentials were invalid. Please log in again.",
            403 => "You do not have permission to perform this action.",
            404 => "The requested item could not be found.",
            _ => "Something went wrong while processing your request. Please try again."
        };
    }
}

