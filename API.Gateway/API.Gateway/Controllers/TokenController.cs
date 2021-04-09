using Gateway.Core.Models;
using Gateway.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Gateway.Controllers
{
    [Authorize]
    [Route("[controller]")]
    [ApiController]
    public class TokenController : ControllerBase
    {
        private readonly IJwtAuthenticationManager jwtAuthenticationManager;
        public TokenController(IJwtAuthenticationManager jwtAuthenticationManager)
        {
            this.jwtAuthenticationManager = jwtAuthenticationManager;
        }

        // Authentication
        [AllowAnonymous]
        [HttpPost("authenticate")]
        public IActionResult Authenticate([FromBody] UserInfo userInfo)
        {
            var token = jwtAuthenticationManager.Authenticate
                (userInfo.Username, userInfo.Password);
            if (token == null)
                return Unauthorized();
            return Ok(token);
        }
    }
}
