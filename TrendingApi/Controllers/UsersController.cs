using Microsoft.AspNetCore.Mvc;
using TrendingApi.Models;

namespace TrendingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserService _userService;

        public UsersController(UserService userService)
        {
            _userService = userService;
        }

        // Dohvati sve korisnike iz Redis-a
        [HttpGet("all")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                return Ok(new { success = true, data = users });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // Dohvati specifičnog korisnika
        [HttpGet("{username}")]
        public async Task<IActionResult> GetUser(string username)
        {
            try
            {
                var user = await _userService.GetUserAsync(username);
                if (user == null)
                    return NotFound(new { success = false, message = "Korisnik nije pronađen" });

                return Ok(new { success = true, data = user });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // Kreiraj ili ažuriraj korisnika
        [HttpPost]
        public async Task<IActionResult> CreateOrUpdateUser([FromBody] User user)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(user.Username))
                    return BadRequest(new { success = false, message = "Korisničko ime je obavezno" });

                await _userService.CreateOrUpdateUserAsync(user);
                return Ok(new { success = true, message = "Korisnik uspešno sačuvan", data = user });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
