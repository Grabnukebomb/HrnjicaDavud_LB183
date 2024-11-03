using Google.Authenticator;
using M183.Controllers.Dto;
using M183.Controllers.Helper;
using M183.Data;
using M183.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace M183.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class LoginController : ControllerBase
  {
    private readonly IConfiguration _configuration;
    private readonly NewsAppContext _context;
    private readonly ILogger<LoginController> _logger;

    public LoginController(NewsAppContext context, IConfiguration configuration, ILogger<LoginController> logger)
    {
      _configuration = configuration;
      _context = context;
      _logger = logger;
    }

    /// <summary>
    /// Login a user using password and username
    /// </summary>
    /// <response code="200">Login successfull</response>
    /// <response code="400">Bad request</response>
    /// <response code="401">Login failed</response>
    [HttpPost]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public ActionResult<User> Login(LoginDto request)
    {
      if (request == null || request.Username.IsNullOrEmpty() || request.Password.IsNullOrEmpty())
      {
        _logger.LogWarning($"Validation failed. User input invalid data.", DateTime.UtcNow);
        return BadRequest();
      }

      User? user = _context.Users
        .Where(aU => aU.Username == request.Username && aU.Password == MD5Helper.ComputeMD5Hash(request.Password))
        .FirstOrDefault();

      if (user == null)
      {
        _logger.LogWarning($"User was unauthorized to log in, Credentials wrong.", DateTime.UtcNow);
        return Unauthorized("login failed");
      }

      if (user.SecretKey2FA != null)
      {
        _logger.LogInformation($"User uses 2FA {user.Username}", DateTime.UtcNow);
        string secretKey = user.SecretKey2FA;
        string userUniqueKey = user.Username + secretKey;
        TwoFactorAuthenticator authenticator = new TwoFactorAuthenticator();
        bool isAuthenticated = authenticator.ValidateTwoFactorPIN(userUniqueKey, request.UserKey);
        if (!isAuthenticated)
        {
          _logger.LogWarning($"False 2FA key {user.Username}", DateTime.UtcNow);
          return Unauthorized("login failed");
        }
      }

      _logger.LogInformation($"User logged in {user.Username}", DateTime.UtcNow);
      return Ok(CreateJwt(user));
    }

    public string CreateJwt(User user)
    {
      string issuer = _configuration.GetSection("Jwt:Issuer").Value;
      string audience = _configuration.GetSection("Jwt:Audience").Value;

      string userRole = user.IsAdmin ? "admin" : "user";

      List<Claim> claims = new List<Claim>
      {
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
        new Claim(JwtRegisteredClaimNames.NameId, user.Id.ToString()),
        new Claim(ClaimTypes.Role, userRole)
      };


      string base64Key = _configuration.GetSection("Jwt:Key").Value!;
      SymmetricSecurityKey securityKey = new SymmetricSecurityKey(Convert.FromBase64String(base64Key));

      SigningCredentials credentials = new SigningCredentials(
              securityKey,
              SecurityAlgorithms.HmacSha512Signature);

      JwtSecurityToken token = new JwtSecurityToken(
        issuer: issuer,
        audience: audience,
        claims: claims,
        notBefore: DateTime.Now,
        expires: DateTime.Now.AddDays(1),
        signingCredentials: credentials
        );

      _logger.LogInformation($"JWT issued to {user.Username}", DateTime.UtcNow);
      return new JwtSecurityTokenHandler().WriteToken(token);
    }
  }
}
