using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using LoginApi.Models;
using BCrypt.Net;
using LoginApi.Helper;  // Add this line

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { message = "Invalid model state" });


        if (await _context.Users.AnyAsync(u => u.Email == model.Email))
        {
            return BadRequest(new { message = "User already exists." });

        }

        var user = new User
        {
            FirstName = model.FirstName,

            LastName = model.LastName,

            Email = model.Email,
            //PasswordHash = CryptoHelper.EncryptString(model.Password)
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { message = "User registered successfully" });

    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == model.Email);

                
        //as per my thoud this should handel the old password and new passwords also :)
        //if (!BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash) || model.Password != CryptoHelper.DecryptString(user.PasswordHash))
        //{
        //    return Unauthorized(new { message = "Invalid email or password" });
        //}

        if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid email or password" });
        }

        var token = GenerateJwtToken(user);
        //return Ok(new { token });
        return Ok(new
        {
            token,
            userDetails = new
            {
                user.FirstName,
                user.LastName,
                user.Email
            }
        });
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(30),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
