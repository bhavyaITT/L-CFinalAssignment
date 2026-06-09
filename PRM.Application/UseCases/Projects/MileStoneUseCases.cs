using PRM.Application.DTOs.Project;
using PRM.Application.Interfaces.Repository;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.UseCases.Projects
{
    public class AddMilestoneUseCase(IUnitOfWork unitOfWork)
    {
        public async Task<Result<MilestoneResponse>> ExecuteAsync(int projectId, AddMilestoneRequest request, CancellationToken ct = default)
        {
            if (!await unitOfWork.Projects.AnyAsync(p => p.Id == projectId, ct))
                return Result<MilestoneResponse>.Failure($"Project with ID {projectId} not found.");

            var milestone = new Milestone
            {
                ProjectId = projectId,
                Title = request.Title,
                DueDate = request.DueDate,
                Status = MilestoneStatus.NotStarted
            };

            await unitOfWork.Milestones.AddAsync(milestone, ct);
            await unitOfWork.SaveChangesAsync(ct);

            return Result<MilestoneResponse>.Success(new MilestoneResponse(
                milestone.Id, milestone.Title, milestone.DueDate, milestone.Status.ToString()
            ));
        }
    }

    public class UpdateMilestoneStatusUseCase(IUnitOfWork unitOfWork)
    {
        public async Task<Result> ExecuteAsync(int milestoneId, UpdateMilestoneStatusRequest request, CancellationToken ct = default)
        {
            if (!Enum.TryParse<MilestoneStatus>(request.Status, ignoreCase: true, out var status))
                return Result.Failure($"Invalid status '{request.Status}'. Valid: NotStarted, InProgress, Done.");

            var milestone = await unitOfWork.Milestones.GetByIdAsync(milestoneId, ct);
            if (milestone is null)
                return Result.Failure($"Milestone with ID {milestoneId} not found.");

            milestone.Status = status;
            unitOfWork.Milestones.Update(milestone);
            await unitOfWork.SaveChangesAsync(ct);

            return Result.Success();
        }
    }
}
