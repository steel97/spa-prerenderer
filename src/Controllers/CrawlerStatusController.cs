using Microsoft.AspNetCore.Mvc;
using SpaPrerenderer.Services;

namespace SpaPrerenderer.Controllers;

[Controller]
public class CrawlerStatusController : ControllerBase
{
    private readonly StorageSingletonService _storageSingletonService;

    public CrawlerStatusController(StorageSingletonService storageSingletonService) => _storageSingletonService = storageSingletonService;


    [HttpGet("/spa-prerenderer/status", Order = 0)]
    public StorageSingletonService Index() => _storageSingletonService;
}