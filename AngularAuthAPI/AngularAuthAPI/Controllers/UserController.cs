using AngularAuthAPI.Context;
using AngularAuthAPI.Helpers;
using AngularAuthAPI.Models;
using AngularAuthAPI.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace AngularAuthAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Check authen from login form
        /// </summary>
        /// <param name="userObj"></param>
        /// <returns></returns>
        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] User userObj)
        {
            // Check user object from request body
            if(userObj == null)
            {
                return BadRequest();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == userObj.UserName);

            // Check username exist
            if(user == null)
            {
                return NotFound(new { Message = "Username or password is incorrect!" });
            }

            // Check password
            if(!PasswordHasher.VerifyPassword(userObj.Password, user.Password))
            {
                return BadRequest(new { Message = "Username or password is incorrect!" });
            }

            // Generate token
            user.Token = CreateJwtToken(user);

            var newAccessToken = user.Token;
            var newRefreshToken = CreateRefreshToken();
            user.RefreshToken = newRefreshToken;

            await _context.SaveChangesAsync();

            return Ok(new TokenApiDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            });
        }

        /// <summary>
        /// Register user
        /// </summary>
        /// <param name="userObj"></param>
        /// <returns></returns>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User userObj)
        {
            if(userObj == null)
            {
                return BadRequest();
            }

            // Check username
            if(await CheckUsernameExistAsync(userObj.UserName))
            {
                return BadRequest(new
                {
                    Message = "Username already exist!"
                });
            }

            // Check email
            if (await CheckEmailExistAsync(userObj.Email))
            {
                return BadRequest(new
                {
                    Message = "Email already exist!"
                });
            }

            // Check password
            var pass = CheckPasswordStrength(userObj.Password);
            if (!string.IsNullOrEmpty(pass))
            {
                return BadRequest(new
                {
                    Message = pass
                });
            }

            var email = CheckEmailValid(userObj.Email);
            if (!string.IsNullOrEmpty(email))
            {
                return BadRequest(new
                {
                    Message = email
                });
            }

            // Hash password
            userObj.Password = PasswordHasher.HashPassword(userObj.Password);
            userObj.Role = "User";
            userObj.Token = "";

            await _context.Users.AddAsync(userObj);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "User Registered!"
            });
        }

        /// <summary>
        /// Get all users
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet]
        public async Task<ActionResult<User>> GetAllUsers()
        {
            var users = await _context.Users.ToListAsync();

            return Ok(users);
        }

        /// <summary>
        /// Check username has existed
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        private async Task<bool> CheckUsernameExistAsync(string username)
        {
            return await _context.Users.AnyAsync(u => u.UserName == username);
        }

        /// <summary>
        /// Check email has existed
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        private async Task<bool> CheckEmailExistAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        /// <summary>
        /// Check password is correct
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        private string CheckPasswordStrength(string password)
        {
            var sb = new StringBuilder();

            if(password.Length < 8)
            {
                sb.Append($"Minimum password length should be greater or equal than 8 {Environment.NewLine}");
            }

            if(!Regex.IsMatch(password, "[a-zA-z0-9]"))
            {
                sb.Append($"Password should be Alphanumeric {Environment.NewLine}");
            }

            if (!Regex.IsMatch(password, "[~,!,@,#,$,%,^,&,*,(,)]"))
            {
                sb.Append($"Password should contain special chars {Environment.NewLine}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Check email is valid
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        private string CheckEmailValid(string email)
        {
            var sb = new StringBuilder();

            if(!Regex.IsMatch(email, @"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$"))
            {
                sb.Append($"Email is not valid!");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Create jwt token
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        private string CreateJwtToken(User user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes("this is a secret key, don't show it"); // secret key
            var identity = new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}")
            });
            var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = identity,
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = credentials,
            };

            var token = jwtTokenHandler.CreateToken(tokenDescriptor);

            return jwtTokenHandler.WriteToken(token);
        }

        private string CreateRefreshToken()
        {
            var tokenBytes = RandomNumberGenerator.GetBytes(64);
            var refreshToken = Convert.ToBase64String(tokenBytes);
            var tokenInUser = _context.Users.Any(t => t.RefreshToken == refreshToken);

            if(tokenInUser)
            {
                return CreateRefreshToken();
            }

            return refreshToken;
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var key = Encoding.ASCII.GetBytes("this is a secret key, don't show it");
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateLifetime = false
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken;
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;

            if(jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("This is invalid token");
            }

            return principal;
        }
    }
}
