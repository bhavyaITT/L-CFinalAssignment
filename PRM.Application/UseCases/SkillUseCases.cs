using PRM.Application.DTOs.Employee;
using PRM.Application.Interfaces.Repository;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.UseCases
{
    public class GetSkillUseCase(IUnitOfWork unitOfWork)
    {
        public async Task<Result<List<SkillResponse>>> ExecuteAsync(int employeeId, CancellationToken ct = default)
        {
            if (!await unitOfWork.Employees.AnyAsync(e => e.Id == employeeId && e.User.IsActive, ct))
                return Result<List<SkillResponse>>.Failure($"Active employee with ID {employeeId} not found.");

            var skills = await unitOfWork.EmployeeSkills.FindAsync(
                s => s.EmployeeId == employeeId, ct);

            var skillResponses = skills
                .Select(s => new SkillResponse(
                    s.Id,
                    s.SkillName,
                    s.Category.ToString(),
                    s.Proficiency.ToString()))
                .ToList();

            return Result<List<SkillResponse>>.Success(skillResponses);
        }
    }

    public class AddSkillUseCase(IUnitOfWork unitOfWork)
    {
        public async Task<Result<SkillResponse>> ExecuteAsync(int employeeId, AddSkillRequest request, CancellationToken ct = default)
        {
            if (!await unitOfWork.Employees.AnyAsync(e => e.Id == employeeId && e.User.IsActive, ct))
                return Result<SkillResponse>.Failure($"Active employee with ID {employeeId} not found.");

            if (!Enum.TryParse<SkillCategory>(request.Category, ignoreCase: true, out var category))
                return Result<SkillResponse>.Failure($"Invalid category '{request.Category}'.");

            if (!Enum.TryParse<ProficiencyLevel>(request.Proficiency, ignoreCase: true, out var proficiency))
                return Result<SkillResponse>.Failure($"Invalid proficiency '{request.Proficiency}'.");

            // Prevent duplicate skill names for the same employee
            if (await unitOfWork.EmployeeSkills.AnyAsync(
                s => s.EmployeeId == employeeId && s.SkillName.ToLower() == request.SkillName.ToLower(), ct))
                return Result<SkillResponse>.Failure($"Skill '{request.SkillName}' already exists for this employee.");

            var skill = new EmployeeSkill
            {
                EmployeeId = employeeId,
                SkillName = request.SkillName,
                Category = category,
                Proficiency = proficiency
            };

            await unitOfWork.EmployeeSkills.AddAsync(skill, ct);
            await unitOfWork.SaveChangesAsync(ct);

            return Result<SkillResponse>.Success(new SkillResponse(
                skill.Id, skill.SkillName, skill.Category.ToString(), skill.Proficiency.ToString()
            ));
        }
    }

    public class UpdateSkillProficiencyUseCase(IUnitOfWork unitOfWork)
    {
        public async Task<Result> ExecuteAsync(int skillId, UpdateSkillProficiencyRequest request, CancellationToken ct = default)
        {
            if (!Enum.TryParse<ProficiencyLevel>(request.Proficiency, ignoreCase: true, out var proficiency))
                return Result.Failure($"Invalid proficiency '{request.Proficiency}'.");

            var skill = await unitOfWork.EmployeeSkills.GetByIdAsync(skillId, ct);
            if (skill is null)
                return Result.Failure($"Skill with ID {skillId} not found.");

            skill.Proficiency = proficiency;
            unitOfWork.EmployeeSkills.Update(skill);
            await unitOfWork.SaveChangesAsync(ct);

            return Result.Success();
        }
    }

    public class RemoveSkillUseCase(IUnitOfWork unitOfWork)
    {
        public async Task<Result> ExecuteAsync(int skillId, CancellationToken ct = default)
        {
            var skill = await unitOfWork.EmployeeSkills.GetByIdAsync(skillId, ct);
            if (skill is null)
                return Result.Failure($"Skill with ID {skillId} not found.");

            unitOfWork.EmployeeSkills.Remove(skill);
            await unitOfWork.SaveChangesAsync(ct);

            return Result.Success();
        }
    }
}
