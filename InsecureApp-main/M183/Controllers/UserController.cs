using M183.Controllers.Dto;
using M183.Controllers.Helper;
using M183.Data;
using Microsoft.AspNetCore.Mvc;

namespace M183.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class UserController : ControllerBase
  {
    private readonly NewsAppContext _context;
    private readonly ILogger<UserController> _logger;

    public UserController(NewsAppContext context, ILogger<UserController> logger)
    {
      _context = context;
      _logger = logger;
    }

    /// <summary>
    /// update password
    /// </summary>
    /// <response code="200">Password updated successfully</response>
    /// <response code="400">Bad request</response>
    /// <response code="404">User not found</response>
    [HttpPatch("password-update")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public ActionResult PasswordUpdate(PasswordUpdateDto request)
    {
      if (request == null)
      {
        _logger.LogWarning($"Request empty", DateTime.UtcNow);
        return BadRequest();
      }

      var user = _context.Users.Find(request.UserId);
      if (user == null)
      {
        _logger.LogWarning($"User not found", DateTime.UtcNow);
        return NotFound(string.Format("User {0} not found", request.UserId));
      }
      user.IsAdmin = request.IsAdmin;
      user.Password = MD5Helper.ComputeMD5Hash(request.NewPassword);

      _context.Users.Update(user);
      _context.SaveChanges();

      _logger.LogWarning($"User passwort changed {request.UserId}", DateTime.UtcNow);
      return Ok();
    }
  }
}
