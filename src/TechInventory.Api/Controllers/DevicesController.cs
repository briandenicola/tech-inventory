using Microsoft.AspNetCore.Mvc;

namespace TechInventory.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class DevicesController : ControllerBase
{
    private readonly ILogger<DevicesController> _logger;

    public DevicesController(ILogger<DevicesController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get all devices
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<object>> GetDevices()
    {
        // TODO(spec-001): wire to MediatR query
        _logger.LogInformation("Devices endpoint hit (stub)");
        return Ok(Array.Empty<object>());
    }
}
