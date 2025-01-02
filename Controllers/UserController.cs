using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Models.LoginCredentials;
using Models.User;
using Service.UserService;
using Services.JwtService;

namespace Controllers.UserController;

[Route("/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IJwtService _jwtService;
    // inject User Service and JwtService
    public UserController(IUserService userService, IJwtService jwtService)
    {
        _userService = userService;
        _jwtService = jwtService;
    }

    // Get all users in the system - only accessible by admin
    [Authorize(Policy = "AdminOnly")]
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        try
        {
            var users = await _userService.GetAllUsers();
            if (users == null)
            {
                return NotFound("No users exist");
            }
            return Ok(users);
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, new { Message = "Users cannot be retrieved", Details = ex.Message });
        }
    }

    // this should be admin only so that other users cannot access any user detail by mistake
    [Authorize]
    [HttpGet("{UserId}")]
    public async Task<IActionResult> GetUserById(string UserId)
    {
        try
        {
            var user = await _userService.GetUserById(UserId);
            if (user == null)
            {
                return NotFound("No user found with User Id: " + UserId);
            }
            return Ok(user);
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, new { Message = "User with Id: " + UserId + " cannot be retrieved", Details = ex.Message });
        }
    }

    // get users based on user filter criteria defined in the User Service class
    [Authorize(Policy = "AdminOnly")]
    [HttpGet("filter")]
    public async Task<IActionResult> GetUsersByFilter([FromQuery] UserFilterCriteria filterCriteria)
    {
        try
        {
            var users = await _userService.GetUsersByFilter(filterCriteria);
            return Ok(users);
        }
        catch (System.Exception ex)
        {

            return BadRequest(new { Message = "Users cannot be retrieved", Details = ex.Message });
        }
    }

    // signup user and add details to database
    [HttpPost("signup")]
    public async Task<IActionResult> Signup([FromBody] User user)
    {
        try
        {
            await _userService.AddUser(user);
            return Ok("User added with User Id: " + user.UserId + " and Username: " + user.UserName);
        }
        catch (System.Exception ex)
        {
            return BadRequest(new { Message = "User cannot be created", Details = ex.Message });
        }
    }

    // login has two authentication layers - verifiying username and password
    // second - assigning a JWT token for use
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCredentials loginCredentials)
    {
        // authenticate the user
        try
        {
            var userExists = await _userService.AuthenticateUser(loginCredentials);
            if (userExists)
            {
                // extract user role
                string userRole = await _userService.GetUserRoleFromUsername(loginCredentials.username!);
                // create token for user
                var token = _jwtService.generateJwtToken(loginCredentials.username!, userRole);
                return Ok(new { Message = "User authenticated successfully", Token = token });
            }

            return Unauthorized("Invalid username or password");
        }
        catch (System.Exception ex)
        {

            return StatusCode(500, new { Message = "User authentication failed.", Details = ex.Message });
        }
    }

    // This is allowed for customer as well so that they can modify their details
    [Authorize(Policy = "CustomerOrAdmin")]
    [HttpPatch]
    public async Task<IActionResult> ModifyUser([FromBody] User user)
    {
        try
        {
            await _userService.ModifyUser(user);
            return StatusCode(201, "User with User Id: " + user.UserId + " updated successfully.");
        }
        catch (System.Exception ex)
        {
            return BadRequest(new { Message = "User update failed.", Details = ex.Message });
        }
    }

    // admin only operation
    [Authorize(Policy = "AdminOnly")]
    [HttpDelete("{UserId}")]
    public async Task<IActionResult> DeleteUser(string UserId)
    {
        try
        {
            await _userService.DeleteUser(UserId);
            return Ok("User deleted with User Id: " + UserId);
        }
        catch (System.Exception ex)
        {
            return BadRequest(new { Message = "User cannot be deleted", Details = ex.Message });
        }
    }
}