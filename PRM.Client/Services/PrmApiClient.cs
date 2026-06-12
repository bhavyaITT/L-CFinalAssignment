using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PRM.Application.DTOs;
using PRM.Application.DTOs.Allocation;
using PRM.Application.DTOs.Auth;
using PRM.Application.DTOs.Employee;
using PRM.Application.DTOs.Manager;
using PRM.Application.DTOs.Project;
using PRM.Application.DTOs.Users;

namespace PRM.Client.Services;

public sealed class PrmApiClient(HttpClient http, UserSession session)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // ── Auth ────────────────────────────────────────────────────

    public Task<ApiResult<LoginResponse>> LoginAsync(string username, string password, CancellationToken ct = default) =>
        PostAnonymousAsync<LoginResponse>("api/auth/login", new LoginRequest(username, password), ct);

    public Task<ApiResult> ChangePasswordAsync(string newPassword, string confirmPassword, CancellationToken ct = default) =>
        PostVoidAsync("api/auth/change-password", new ChangePasswordRequest(newPassword, confirmPassword), ct);

    private async Task<ApiResult<T>> PostAnonymousAsync<T>(string url, object body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body, options: JsonOptions)
        };
        return await SendAsync<T>(request, ct);
    }

    private async Task<ApiResult> PostVoidAsync(string url, object body, CancellationToken ct)
    {
        using var request = CreateAuthorized(HttpMethod.Post, url);
        request.Content = JsonContent.Create(body, options: JsonOptions);
        return await SendVoidAsync(request, ct);
    }

    // ── Admin: Employees ────────────────────────────────────────

    public Task<ApiResult<EmployeesListResponse>> GetEmployeesAsync(CancellationToken ct = default) =>
        GetAsync<EmployeesListResponse>("api/employees", ct);

    public Task<ApiResult<EmployeeDetailResponse>> GetEmployeeAsync(int id, CancellationToken ct = default) =>
        GetAsync<EmployeeDetailResponse>($"api/employees/{id}", ct);

    public Task<ApiResult<EmployeeDetailResponse>> UpdateEmployeeAsync(int id, UpdateEmployeeRequest body, CancellationToken ct = default) =>
        PutAsync<EmployeeDetailResponse>($"api/employees/{id}", body, ct);

    public Task<ApiResult> DeactivateEmployeeAsync(int id, CancellationToken ct = default) =>
        PutVoidAsync($"api/employees/{id}/deactivate", new { }, ct);

    public Task<ApiResult<List<SkillResponse>>> GetEmployeeSkillsAsync(int employeeId, CancellationToken ct = default) =>
        GetAsync<List<SkillResponse>>($"api/employees/{employeeId}/skills", ct);

    public Task<ApiResult<SkillResponse>> AddSkillAsync(int employeeId, AddSkillRequest body, CancellationToken ct = default) =>
        PostAsync<SkillResponse>($"api/employees/{employeeId}/skills", body, ct);

    public Task<ApiResult> UpdateSkillProficiencyAsync(int skillId, UpdateSkillProficiencyRequest body, CancellationToken ct = default) =>
        PutVoidAsync($"api/employees/skills/{skillId}", body, ct);

    public Task<ApiResult> RemoveSkillAsync(int skillId, CancellationToken ct = default) =>
        DeleteVoidAsync($"api/employees/skills/{skillId}", ct);

    public Task<ApiResult> AssignManagerAsync(int employeeId, int managerId, CancellationToken ct = default) =>
        PutVoidAsync($"api/employees/{employeeId}/manager/{managerId}", new { }, ct);

    // ── Admin: Users ────────────────────────────────────────────

    public Task<ApiResult<UsersListResponse>> GetUsersAsync(CancellationToken ct = default) =>
        GetAsync<UsersListResponse>("api/users", ct);

    public Task<ApiResult<UserSummaryResponse>> CreateUserAsync(CreateUserRequest body, CancellationToken ct = default) =>
        PostAsync<UserSummaryResponse>("api/users", body, ct);

    public Task<ApiResult> ResetPasswordAsync(string username, ResetPasswordRequest body, CancellationToken ct = default) =>
        PutVoidAsync($"api/users/{Uri.EscapeDataString(username)}/reset-password", body, ct);

    public Task<ApiResult> DeactivateUserAsync(string username, CancellationToken ct = default) =>
        PutVoidAsync($"api/users/{Uri.EscapeDataString(username)}/deactivate", new { }, ct);

    public Task<ApiResult> ReactivateUserAsync(string username, CancellationToken ct = default) =>
        PutVoidAsync($"api/users/{Uri.EscapeDataString(username)}/reactivate", new { }, ct);

    // ── Admin: Projects ─────────────────────────────────────────

    public Task<ApiResult<ProjectsListResponse>> GetProjectsAsync(CancellationToken ct = default) =>
        GetAsync<ProjectsListResponse>("api/projects", ct);

    public Task<ApiResult<ProjectDetailResponse>> GetProjectAsync(int id, CancellationToken ct = default) =>
        GetAsync<ProjectDetailResponse>($"api/projects/{id}", ct);

    public Task<ApiResult<ProjectDetailResponse>> CreateProjectAsync(CreateProjectRequest body, CancellationToken ct = default) =>
        PostAsync<ProjectDetailResponse>("api/projects", body, ct);

    public Task<ApiResult<ProjectDetailResponse>> UpdateProjectAsync(int id, UpdateProjectRequest body, CancellationToken ct = default) =>
        PutAsync<ProjectDetailResponse>($"api/projects/{id}", body, ct);

    public Task<ApiResult<MilestoneResponse>> AddMilestoneAsync(int projectId, AddMilestoneRequest body, CancellationToken ct = default) =>
        PostAsync<MilestoneResponse>($"api/projects/{projectId}/milestones", body, ct);

    public Task<ApiResult> UpdateMilestoneStatusAsync(int milestoneId, UpdateMilestoneStatusRequest body, CancellationToken ct = default) =>
        PutVoidAsync($"api/projects/milestones/{milestoneId}", body, ct);

    // ── Admin: Allocations ──────────────────────────────────────

    public Task<ApiResult<AllocationsListResponse>> GetAllocationsAsync(int? employeeId = null, int? projectId = null, bool activeOnly = true, CancellationToken ct = default)
    {
        var query = new List<string> { $"activeOnly={activeOnly.ToString().ToLowerInvariant()}" };
        if (employeeId.HasValue) query.Add($"employeeId={employeeId.Value}");
        if (projectId.HasValue) query.Add($"projectId={projectId.Value}");
        return GetAsync<AllocationsListResponse>($"api/allocations?{string.Join("&", query)}", ct);
    }

    // ── Admin: System Config ────────────────────────────────────

    public Task<ApiResult<SystemConfigResponse>> GetSystemConfigAsync(CancellationToken ct = default) =>
        GetAsync<SystemConfigResponse>("api/system-config", ct);

    public Task<ApiResult<SystemConfigResponse>> UpdateSystemConfigAsync(UpdateSystemConfigRequest body, CancellationToken ct = default) =>
        PatchAsync<SystemConfigResponse>("api/system-config", body, ct);

    // ── Manager: Dashboard ──────────────────────────────────────

    public Task<ApiResult<ResourceDashboardResponse>> GetResourceDashboardAsync(CancellationToken ct = default) =>
        GetAsync<ResourceDashboardResponse>("api/manager/dashboard", ct);

    public Task<ApiResult<EmployeeDrillInResponse>> GetEmployeeDrillInAsync(int employeeId, CancellationToken ct = default) =>
        GetAsync<EmployeeDrillInResponse>($"api/manager/dashboard/employees/{employeeId}", ct);

    // ── Manager: Allocations ────────────────────────────────────

    public Task<ApiResult<AllocationValidationResponse>> ValidateAllocationAsync(CreateAllocationRequest body, CancellationToken ct = default) =>
        PostAsync<AllocationValidationResponse>("api/manager/allocations/validate", body, ct);

    public Task<ApiResult<CreateAllocationResponse>> CreateAllocationAsync(CreateAllocationRequest body, CancellationToken ct = default) =>
        PostAsync<CreateAllocationResponse>("api/manager/allocations", body, ct);

    public Task<ApiResult<List<AllocationOnProjectResponse>>> GetAllocationsOnProjectAsync(int projectId, CancellationToken ct = default) =>
        GetAsync<List<AllocationOnProjectResponse>>($"api/manager/allocations/projects/{projectId}", ct);

    public Task<ApiResult<EndAllocationResponse>> EndAllocationAsync(int allocationId, CancellationToken ct = default) =>
        DeleteAsync<EndAllocationResponse>($"api/manager/allocations/{allocationId}", ct);

    // ── Manager: Projects ───────────────────────────────────────

    public Task<ApiResult<List<ManagerProjectSummaryResponse>>> GetMyProjectsAsync(CancellationToken ct = default) =>
        GetAsync<List<ManagerProjectSummaryResponse>>("api/manager/projects", ct);

    public Task<ApiResult<ManagerProjectDetailResponse>> GetMyProjectDetailAsync(int id, CancellationToken ct = default) =>
        GetAsync<ManagerProjectDetailResponse>($"api/manager/projects/{id}", ct);

    // ── Manager: Timesheets ─────────────────────────────────────

    public Task<ApiResult<TeamTimesheetResponse>> GetTeamTimesheetsAsync(DateOnly? weekStart, CancellationToken ct = default)
    {
        var url = weekStart.HasValue
            ? $"api/manager/timesheets?weekStart={weekStart.Value:yyyy-MM-dd}"
            : "api/manager/timesheets";
        return GetAsync<TeamTimesheetResponse>(url, ct);
    }

    public Task<ApiResult<TeamTimesheetDetailResponse>> GetTeamTimesheetDetailAsync(int employeeId, DateOnly weekStart, CancellationToken ct = default) =>
        GetAsync<TeamTimesheetDetailResponse>($"api/manager/timesheets/employees/{employeeId}?weekStart={weekStart:yyyy-MM-dd}", ct);

    // ── Manager: AI ─────────────────────────────────────────────

    public Task<ApiResult<SkillMatchResponse>> SkillMatchAsync(SkillMatchRequest body, CancellationToken ct = default) =>
        PostAsync<SkillMatchResponse>("api/ai/skill-match", body, ct);

    public Task<ApiResult<TeamStaffingResponse>> TeamStaffingAsync(TeamStaffingRequest body, CancellationToken ct = default) =>
        PostAsync<TeamStaffingResponse>("api/ai/team-staffing", body, ct);

    public Task<ApiResult<RiskSummaryResponse>> RiskSummaryAsync(int projectId, CancellationToken ct = default) =>
        GetAsync<RiskSummaryResponse>($"api/ai/risk-summary/{projectId}", ct);

    // ── Employee ────────────────────────────────────────────────

    public Task<ApiResult<MissedTimesheetReminderResponse>> GetTimesheetReminderAsync(CancellationToken ct = default) =>
        GetAsync<MissedTimesheetReminderResponse>("api/employee/timesheet-reminder", ct);

    public Task<ApiResult<ActiveAllocationsForWeekResponse>> GetAllocationsForWeekAsync(DateOnly? weekStart, CancellationToken ct = default)
    {
        var url = weekStart.HasValue
            ? $"api/employee/timesheets/allocations?weekStart={weekStart.Value:yyyy-MM-dd}"
            : "api/employee/timesheets/allocations";
        return GetAsync<ActiveAllocationsForWeekResponse>(url, ct);
    }

    public Task<ApiResult<SubmitTimesheetResponse>> SubmitTimesheetAsync(SubmitTimesheetRequest body, CancellationToken ct = default) =>
        PostAsync<SubmitTimesheetResponse>("api/employee/timesheets", body, ct);

    public Task<ApiResult<MyTimesheetsResponse>> GetMyTimesheetsAsync(CancellationToken ct = default) =>
        GetAsync<MyTimesheetsResponse>("api/employee/timesheets", ct);

    public Task<ApiResult<TimesheetDetailResponse>> GetMyTimesheetDetailAsync(DateOnly weekStart, CancellationToken ct = default) =>
        GetAsync<TimesheetDetailResponse>($"api/employee/timesheets/{weekStart:yyyy-MM-dd}", ct);

    public Task<ApiResult<MyAllocationsResponse>> GetMyAllocationsAsync(CancellationToken ct = default) =>
        GetAsync<MyAllocationsResponse>("api/employee/allocations", ct);

    // ── HTTP helpers ────────────────────────────────────────────

    private HttpRequestMessage CreateAuthorized(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        if (session.IsAuthenticated)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
        return request;
    }

    private async Task<ApiResult<T>> GetAsync<T>(string url, CancellationToken ct)
    {
        using var request = CreateAuthorized(HttpMethod.Get, url);
        return await SendAsync<T>(request, ct);
    }

    private async Task<ApiResult<T>> PostAsync<T>(string url, object body, CancellationToken ct)
    {
        using var request = CreateAuthorized(HttpMethod.Post, url);
        request.Content = JsonContent.Create(body, options: JsonOptions);
        return await SendAsync<T>(request, ct);
    }

    private async Task<ApiResult<T>> PutAsync<T>(string url, object body, CancellationToken ct)
    {
        using var request = CreateAuthorized(HttpMethod.Put, url);
        request.Content = JsonContent.Create(body, options: JsonOptions);
        return await SendAsync<T>(request, ct);
    }

    private async Task<ApiResult<T>> PatchAsync<T>(string url, object body, CancellationToken ct)
    {
        using var request = CreateAuthorized(HttpMethod.Patch, url);
        request.Content = JsonContent.Create(body, options: JsonOptions);
        return await SendAsync<T>(request, ct);
    }

    private async Task<ApiResult<T>> DeleteAsync<T>(string url, CancellationToken ct)
    {
        using var request = CreateAuthorized(HttpMethod.Delete, url);
        return await SendAsync<T>(request, ct);
    }

    private async Task<ApiResult> PutVoidAsync(string url, object body, CancellationToken ct)
    {
        using var request = CreateAuthorized(HttpMethod.Put, url);
        request.Content = JsonContent.Create(body, options: JsonOptions);
        return await SendVoidAsync(request, ct);
    }

    private async Task<ApiResult> DeleteVoidAsync(string url, CancellationToken ct)
    {
        using var request = CreateAuthorized(HttpMethod.Delete, url);
        return await SendVoidAsync(request, ct);
    }

    private async Task<ApiResult<T>> SendAsync<T>(HttpRequestMessage request, CancellationToken ct)
    {
        try
        {
            var response = await http.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
                return ApiResult<T>.Ok(data!);
            }

            return ApiResult<T>.Fail(await ReadErrorAsync(response, ct));
        }
        catch (HttpRequestException ex)
        {
            return ApiResult<T>.Fail($"Cannot reach API server: {ex.Message}");
        }
    }

    private async Task<ApiResult> SendVoidAsync(HttpRequestMessage request, CancellationToken ct)
    {
        try
        {
            var response = await http.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
                return ApiResult.Ok();

            return ApiResult.Fail(await ReadErrorAsync(response, ct));
        }
        catch (HttpRequestException ex)
        {
            return ApiResult.Fail($"Cannot reach API server: {ex.Message}");
        }
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions, ct);
            if (!string.IsNullOrWhiteSpace(error?.Message))
                return error.Message;
        }
        catch
        {
            // fall through
        }

        return $"Request failed ({(int)response.StatusCode} {response.ReasonPhrase})";
    }

    private sealed record ErrorResponse(string? Message);
}

public class ApiResult
{
    public bool IsSuccess { get; init; }
    public string? Error { get; init; }

    public static ApiResult Ok() => new() { IsSuccess = true };
    public static ApiResult Fail(string error) => new() { IsSuccess = false, Error = error };
}

public sealed class ApiResult<T> : ApiResult
{
    public T? Data { get; init; }

    public static ApiResult<T> Ok(T data) => new() { IsSuccess = true, Data = data };
    public static new ApiResult<T> Fail(string error) => new() { IsSuccess = false, Error = error };
}
