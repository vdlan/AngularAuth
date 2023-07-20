using AngularAuthAPI.Context;
using AngularAuthAPI.Helpers;
using AngularAuthAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

            return Ok(new
            {
                Message = "Login Success!"
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

        private async Task<bool> CheckUsernameExistAsync(string username)
        {
            return await _context.Users.AnyAsync(u => u.UserName == username);
        }

        private async Task<bool> CheckEmailExistAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

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

        private string CheckEmailValid(string email)
        {
            var sb = new StringBuilder();

            if(!Regex.IsMatch(email, @"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$"))
            {
                sb.Append($"Email is not valid!");
            }

            return sb.ToString();
        }
    }
}
