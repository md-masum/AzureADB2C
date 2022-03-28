using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AzureADB2CApi.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly GraphClient _graphClient;

        public AuthController(GraphClient graphClient)
        {
            _graphClient = graphClient;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUser()
        {
            return Ok(await _graphClient.ListUsers());
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserById(string userId)
        {
            return Ok(await _graphClient.GetUser(userId));
        }

        [HttpGet("user/{userId}/role/{roleId}")]
        public async Task<IActionResult> UpdateUserRole(string userId, Roles roleId)
        {
            return Ok(await _graphClient.AddUserRole(userId, roleId));
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody]UserModel user)
        {
            return Ok(await _graphClient.CreateUserWithCustomAttribute(user));
        }
        
        [HttpPut("{userId}")]
        public async Task<IActionResult> UpdateUser(string userId, UserUpdateModel user)
        {
            return Ok(await _graphClient.UpdateUser(userId, user));
        }
        
        [HttpDelete("{userId}")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            await _graphClient.DeleteUserById(userId);
            return Ok();
        }
    }
}
