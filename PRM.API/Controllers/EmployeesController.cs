using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.DTOs.Employee;
using PRM.Application.UseCases;
using PRM.Application.UseCases.Employees;

namespace PRM.API.Controllers
{
    [ApiController]
    [Route("api/employees")]
    [Authorize(Roles = "Admin")]
    public class EmployeesController(
    CreateEmployeeUseCase createEmployee,
    GetAllEmployeesUseCase getAllEmployees,
    GetEmployeeDetailUseCase getEmployeeDetail,
    UpdateEmployeeUseCase updateEmployee,
    DeactivateEmployeeUseCase deactivateEmployee,
    GetSkillUseCase getSkill,
    AddSkillUseCase addSkill,
    UpdateSkillProficiencyUseCase updateSkillProficiency,
    RemoveSkillUseCase removeSkill,
    AssignManagerUseCase assignManager) : ControllerBase
    {
        ///// <summary>GET /api/employees?status=Bench&department=Backend</summary>
        //[HttpGet]
        //public async Task<IActionResult> GetAll(
        //    [FromQuery] string? status,
        //    [FromQuery] string? department,
        //    CancellationToken ct)
        //{
        //    var result = await getAllEmployees.ExecuteAsync(status, department, ct);
        //    return Ok(result.Data);
        //}

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var result = await getAllEmployees.ExecuteAsync(ct);
            return Ok(result.Data);
        }

        /// <summary>GET /api/employees/{id}</summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var result = await getEmployeeDetail.ExecuteAsync(id, ct);

            if (!result.IsSuccess)
                return NotFound(new { message = result.Error });

            return Ok(result.Data);
        }

        /// <summary>POST /api/employees</summary>
        //[HttpPost]
        //public async Task<IActionResult> Create([FromBody] CreateEmployeeRequest request, CancellationToken ct)
        //{
        //    var result = await createEmployee.ExecuteAsync(request, ct);

        //    if (!result.IsSuccess)
        //        return BadRequest(new { message = result.Error });

        //    return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
        //}

        /// <summary>PUT /api/employees/{id}</summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateEmployeeRequest request, CancellationToken ct)
        {
            var result = await updateEmployee.ExecuteAsync(id, request, ct);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.Error });

            return Ok(result.Data);
        }

        /// <summary>PUT /api/employees/{id}/deactivate</summary>
        [HttpPut("{id:int}/deactivate")]
        public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
        {
            var result = await deactivateEmployee.ExecuteAsync(id, ct);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.Error });

            return Ok(new { message = "Employee deactivated. All active allocations ended." });
        }

        // ── Skills ────────────────────────────────────────────────────────────────

        [HttpGet("{employeeId:int}/skills")]
        public async Task<IActionResult> GetSkill(int employeeId, CancellationToken ct)
        {
            var result = await getSkill.ExecuteAsync(employeeId, ct);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.Error });

            return Ok(result.Data);
        }

        /// <summary>POST /api/employees/{id}/skills</summary>
        [HttpPost("{id:int}/skills")]
        public async Task<IActionResult> AddSkill(int id, [FromBody] AddSkillRequest request, CancellationToken ct)
        {
            var result = await addSkill.ExecuteAsync(id, request, ct);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.Error });

            return Ok(result.Data);
        }

        /// <summary>PUT /api/employees/skills/{skillId}</summary>
        [HttpPut("skills/{skillId:int}")]
        public async Task<IActionResult> UpdateSkillProficiency(int skillId, [FromBody] UpdateSkillProficiencyRequest request, CancellationToken ct)
        {
            var result = await updateSkillProficiency.ExecuteAsync(skillId, request, ct);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.Error });

            return Ok(new { message = "Proficiency updated." });
        }

        /// <summary>DELETE /api/employees/skills/{skillId}</summary>
        [HttpDelete("skills/{skillId:int}")]
        public async Task<IActionResult> RemoveSkill(int skillId, CancellationToken ct)
        {
            var result = await removeSkill.ExecuteAsync(skillId, ct);

            if (!result.IsSuccess)
                return NotFound(new { message = result.Error });

            return Ok(new { message = "Skill removed." });
        }

        /// <summary>PUT /api/employees/{id}/manager</summary>
        [HttpPut("{id:int}/manager/{managerId:int}")]
        public async Task<IActionResult> AssignManager(int id, int managerId,  CancellationToken ct)
        {
            var result = await assignManager.ExecuteAsync(id, managerId, ct);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.Error });

            return Ok(new { message = "Manager assigned successfully." });
        }

    }
}
