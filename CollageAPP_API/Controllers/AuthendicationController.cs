using CollageAPP_API.Data;
using CollageAPP_API.Data.ViewModels;
using CollageAPP_API.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CollageAPP_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AuthendicationController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly TokenValidationParameters _tokenValidationParameters;
        public AuthendicationController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager,
            AppDbContext context, IConfiguration configuration, TokenValidationParameters tokenValidationParameters)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _configuration = configuration;
            _tokenValidationParameters = tokenValidationParameters;
        }

        [HttpPost("register-user")]
        [AllowAnonymous]

        public async Task<IActionResult> Register([FromBody] RegisterVM registerVM)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Please, provide all the required fields");
            }
            var userExits = await _userManager.FindByEmailAsync(registerVM.EmailAddress);
            if (userExits != null)
            {
                return BadRequest($"user{registerVM.EmailAddress} already exists ");

            }
            ApplicationUser newUser = new ApplicationUser()
            {
                FirstName = registerVM.FirstName,
                LastName = registerVM.LastName,
                Email = registerVM.EmailAddress,
                UserName = registerVM.UserName,
                SecurityStamp = Guid.NewGuid().ToString()

            };
            var result = await _userManager.CreateAsync(newUser, registerVM.Password);
            if (result.Succeeded) return Ok("User Created");

            return BadRequest ("User cannot be created");
        }
        [HttpPost ("Login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginVM loginVM)

        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Please, provide all the required fields");
            }
            var userExits = await _userManager.FindByEmailAsync(loginVM.EmailAddress);
            if (userExits != null && await _userManager.CheckPasswordAsync(userExits,loginVM.Password))
              {
                var tokenvalue = await GeneratejwtTokenAsync(userExits , null);
                return Ok(tokenvalue);
            }
            return Unauthorized();
        }

        [HttpPost("Refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] TokenRequestVM tokenRequestVM)

        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Please, provide all the required fields");
            }
            var result = await VerifyAndGenerateTokenAsync(tokenRequestVM);
                return Ok(result);
        }

        private async Task<AuthResultVM> VerifyAndGenerateTokenAsync(TokenRequestVM tokenRequestVM)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var storedToken = await _context.RefreshTokens.FirstOrDefaultAsync(x => x.Token ==
            tokenRequestVM.RefreshToken);
            var dbUser = await _userManager.FindByIdAsync(storedToken.UserID);

            try
            {
                var tokenCheckResult = jwtTokenHandler.ValidateToken(tokenRequestVM.Token,
                    _tokenValidationParameters,
                    out var validatedToken);
                return await GeneratejwtTokenAsync(dbUser, storedToken);
            }
            catch(SecurityTokenExpiredException)
            {
                if(storedToken.DateEcpire >= DateTime.UtcNow)
                {
                    return await GeneratejwtTokenAsync(dbUser, storedToken);
                }
                else
                {
                  return  await  GeneratejwtTokenAsync(dbUser, null);
                }
            }
        }

        private  async Task<AuthResultVM> GeneratejwtTokenAsync(ApplicationUser user, RefreshToken rToken)
        {
            var authclaims = new List<Claim>()
           {
               new Claim(ClaimTypes.Name, user.UserName),
               new Claim(ClaimTypes.NameIdentifier, user.Id),
               new Claim(JwtRegisteredClaimNames.Email, user.Email),
               new Claim(JwtRegisteredClaimNames.Sub, user.Email),
               new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
               //new Claim(JwtRegisteredClaimNames.Iss, _configuration["JWT: Issuer"]),
               //new Claim(JwtRegisteredClaimNames.Aud, _configuration["JWT: Audience"])

           };
            var jwtKey = _configuration.GetSection("Jwt:Key").Get<string>();
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:Issuer"],
                audience: _configuration["JWT: Audience"],
                expires: DateTime.UtcNow.AddMinutes(1),
                claims: authclaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256));

            var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);
            if(rToken != null)
            {
                var rTokenResponse = new AuthResultVM()
                {
                    Token = jwtToken,
                    RefreshToken = rToken.Token,
                    ExpiresAt = token.ValidTo
                };
                return rTokenResponse;
            }
            var refreshToken =new RefreshToken()
            {
                JwtId = token.Id,
                IsRevoked = false,
                UserID = user.Id,
                DateAdded = DateTime.UtcNow,
                DateEcpire = DateTime.UtcNow.AddMonths(6),
                Token = Guid.NewGuid().ToString() + "-" + Guid.NewGuid().ToString()
            };
            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();

            var response = new AuthResultVM()
            {
                Token = jwtToken,
                RefreshToken = refreshToken.Token,
                ExpiresAt = token.ValidTo
            };
            return response;

        }
    }
}
