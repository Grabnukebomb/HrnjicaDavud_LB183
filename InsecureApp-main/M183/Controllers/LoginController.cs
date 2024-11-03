using M183.Controllers.Dto;
using M183.Controllers.Helper;
using M183.Data;
using M183.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace M183.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class LoginController : ControllerBase
  {
    private readonly IConfiguration _configuration;
    private readonly NewsAppContext _context;

    public LoginController(NewsAppContext context, IConfiguration configuration)
    {
      _configuration = configuration;
      _context = context;
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
        return BadRequest();
      }

      User? user = _context.Users
        .Where(aU => aU.Username == request.Username && aU.Password == MD5Helper.ComputeMD5Hash(request.Password))
        .FirstOrDefault();

      if (user == null)
      {
        return Unauthorized("login failed");
      }
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

      return new JwtSecurityTokenHandler().WriteToken(token);
    }
  }
}
