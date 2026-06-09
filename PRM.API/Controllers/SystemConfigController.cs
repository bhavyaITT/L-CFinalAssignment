using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PRM.Application.DTOs;
using PRM.Application.UseCases.SystemConfig;

namespace PRM.API.Controllers
{
    [ApiController]
    [Route("api/system-config")]
    [Authorize(Roles = "Admin")]
    public class SystemConfigController(
     GetSystemConfigUseCase getConfig,
     UpdateSystemConfigUseCase updateConfig) : ControllerBase
    {
        /// <summary>GET /api/system-config</summary>
        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken ct)
        {
            var result = await getConfig.ExecuteAsync(ct);

            if (!result.IsSuccess)
                return NotFound(new { message = result.Error });

            return Ok(result.Data);
        }

        /// <summary>PATCH /api/system-config — Update one or more fields</summary>
        [HttpPatch]
        public async Task<IActionResult> Update([FromBody] UpdateSystemConfigRequest request, CancellationToken ct)
        {
            var result = await updateConfig.ExecuteAsync(request, ct);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.Error });

            return Ok(result.Data);
        }
    }
}
