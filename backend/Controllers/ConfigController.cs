using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigController : ControllerBase
{
    private readonly IOptions<FeatureOptions> _featureOptions;

    public ConfigController(IOptions<FeatureOptions> featureOptions)
    {
        _featureOptions = featureOptions;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { aiEnabled = _featureOptions.Value.AIEnabled });
    }
}
