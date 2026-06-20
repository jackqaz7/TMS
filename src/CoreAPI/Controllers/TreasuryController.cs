using System;
using Microsoft.AspNetCore.Mvc;
using CoreAPI.Models;
using CoreAPI.Filters;
using Microsoft.AspNetCore.Authorization;

namespace CoreAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TreasuryController : ControllerBase
    {
        [Route("Status")]
        [MethodFilter("GET")]
        public IActionResult GetStatus()
        {
            // OK is helper method from controllerbase class 
            // new {} is an anonymous object to return service status
            return Ok(new { Service = "Treasury", Status = "Running", Timestamp = DateTime.UtcNow });
        }

        [Route("Process")]
        [MethodFilter("POST")]
        public IActionResult ProcessTrade([FromBody] TransactionDto transaction)
        {
            return Ok(new { service = "Treasury", transaction.Id });
        }
    }
}
