using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Wangkanai.Detection.Services;

namespace SpaPrerenderer.Controllers
{
    [Controller]
    public class SPAController : ControllerBase
    {
        private readonly IDetectionService _detectionService;
        public SPAController(IDetectionService detectionService)
        {
            _detectionService = detectionService;
        }

        [HttpGet("{*url}", Order = int.MaxValue)]
        public ActionResult Index(string url)
        {
            // set 404 response code if match config
            return Ok(Request.QueryString.Value);
            // return file from cache if caching used
            if (_detectionService.Crawler.IsCrawler)
            {
                return Ok("Bot detected");
            }
            return Ok(url);
            return File("./wwwroot/index.html", "text/html", false);
        }
    }
}