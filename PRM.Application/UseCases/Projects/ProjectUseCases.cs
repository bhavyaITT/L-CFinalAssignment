using PRM.Application.DTOs.Project;
using PRM.Application.Interfaces.Repository;
using PRM.Application.Interfaces.Service;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.UseCases.Projects
{
    public class CreateProjectUseCase(IUnitOfWork unitOfWork)
    {
        public async Task<Result<ProjectSummaryResponse>> ExecuteAsync(CreateProjectRequest request, CancellationToken ct = default)
        {
            if (!Enum.TryParse<ProjectStatus>(request.Status, ignoreCase: true, out var status))
                return Result<ProjectSummaryResponse>.Failure($"Invalid status '{request.Status}'. Valid: Planned, Active, OnHold.");

            if (request.EndDate <= request.StartDate)
                return Result<ProjectSummaryResponse>.Failure("End date must be after start date.");

            var managerUser = await unitOfWork.Users.GetByIdAsync(request.ManagerId, ct);
            if (managerUser is null || !managerUser.IsActive)
                return Result<ProjectSummaryResponse>.Failure($"Active manager with ID {request.ManagerId} not found.");

            if (managerUser.Role != UserRole.Manager)
                return Result<ProjectSummaryResponse>.Failure("The assigned manager must have the Manager role.");

            if (!await unitOfWork.Employees.AnyAsync(e => e.Id == request.ManagerId, ct))
                return Result<ProjectSummaryResponse>.Failure($"Employee profile for manager ID {request.ManagerId} not found.");

            var project = new Project
            {
                Name = request.Name,
                Description = request.Description,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Status = status,
                Health = ProjectHealth.OnTrack,
                ManagerId = request.ManagerId
            };

            await unitOfWork.Projects.AddAsync(project, ct);
            await unitOfWork.SaveChangesAsync(ct);

            return Result<ProjectSummaryResponse>.Success(new ProjectSummaryResponse(
                project.Id, project.Name, project.Description, project.StartDate, project.EndDate,
                project.Status.ToString(), project.Health.ToString(), project.ManagerId, managerUser.FullName
            ));
        }
    }

    public class GetAllProjectsUseCase(IQueryService queryService)
    {
        public async Task<Result<ProjectsListResponse>> ExecuteAsync(CancellationToken ct = default)
        {
            var projects = (await queryService.GetAllProjectsWithManagerAsync(ct)).ToList();
            return Result<ProjectsListResponse>.Success(new ProjectsListResponse(projects, projects.Count));
        }
    }

    public class GetProjectDetailUseCase(IQueryService queryService)
    {
        public async Task<Result<ProjectDetailResponse>> ExecuteAsync(int projectId, CancellationToken ct = default)
        {
            var project = await queryService.GetProjectWithMilestonesAsync(projectId, ct);

            if (project is null)
                return Result<ProjectDetailResponse>.Failure($"Project with ID {projectId} not found.");

            return Result<ProjectDetailResponse>.Success(project);
        }
    }

    public class UpdateProjectUseCase(IUnitOfWork unitOfWork)
    {
        public async Task<Result<ProjectSummaryResponse>> ExecuteAsync(int projectId, UpdateProjectRequest request, CancellationToken ct = default)
        {
            if (!Enum.TryParse<ProjectStatus>(request.Status, ignoreCase: true, out var status))
                return Result<ProjectSummaryResponse>.Failure($"Invalid status '{request.Status}'.");

            if (request.EndDate <= request.StartDate)
                return Result<ProjectSummaryResponse>.Failure("End date must be after start date.");

            var project = await unitOfWork.Projects.GetByIdAsync(projectId, ct);
            if (project is null)
                return Result<ProjectSummaryResponse>.Failure($"Project with ID {projectId} not found.");

            var manager = await unitOfWork.Users.GetByIdAsync(request.ManagerId, ct);
            if (manager is null || !manager.IsActive)
                return Result<ProjectSummaryResponse>.Failure($"Active employee with ID {request.ManagerId} not found.");

            project.Name = request.Name;
            project.Description = request.Description;
            project.StartDate = request.StartDate;
            project.EndDate = request.EndDate;
            project.Status = status;
            project.ManagerId = request.ManagerId;

            unitOfWork.Projects.Update(project);
            await unitOfWork.SaveChangesAsync(ct);

            return Result<ProjectSummaryResponse>.Success(new ProjectSummaryResponse(
                project.Id, project.Name, project.Description,
                project.StartDate, project.EndDate,
                project.Status.ToString(), project.Health.ToString(),
                project.ManagerId, manager.FullName
            ));
        }
    }
}
