using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using MyShopAPI.Core.AuthManager;
using MyShopAPI.Core.EntityDTO.CustomerDTO;
using MyShopAPI.Core.IRepository;
using MyShopAPI.Data.Entities;

namespace MyShopAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly IAuthManager _authManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;

        public AccountController(IMapper mapper, IAuthManager authManager, IUnitOfWork unitOfWork, IWebHostEnvironment env, IConfiguration configuration)
        {
            _mapper = mapper;
            _authManager = authManager;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _env = env;
        }

        [HttpPost("signup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SignUP([FromBody] SignUpDTO signUpDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = _mapper.Map<Customer>(signUpDTO);

            user.UserName = signUpDTO.Email;

            var result = await _authManager.CreateAsync(user, signUpDTO.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(error.Code, error.Description);
                }

                return BadRequest(ModelState);
            }

            try
            {
                await _authManager.GenerateEmailConfirmationTokenAsync(user, user.FirstName, _configuration["EmailConfirmation"]);
            }
            catch
            {

            }
            finally
            {
                await _authManager.AddToRolesAsync(user, signUpDTO.Roles);
            }

            return Ok("Registration Successful!. Check your email for validation.");
        }

        [HttpPost("confirm-email")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ConfirmUserEmail([FromQuery] string uid, [FromQuery] string token)
        {
            if (string.IsNullOrEmpty(uid) || string.IsNullOrEmpty(token))
            {
                return BadRequest();
            }

            var result = await _authManager.ConfirmEmailAsync(uid, token);

            if (result.Succeeded)
            {
                return Ok();
            }

            return BadRequest();
        }

        [HttpPost("resend-confirmation-email")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResendConfirmationEmail([FromQuery] string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest();
            }

            var user = await _authManager.GetUserByEmailAsync(email);

            if (user == null)
            {
                return BadRequest();
            }

            await _authManager.GenerateEmailConfirmationTokenAsync(user,user.FirstName, _configuration["AcctValidationEmail"]);
            return Ok("Check your email for validation.");
        }

        [HttpPost]
        [Route("login")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        public async Task<IActionResult> LoginUser([FromBody] LoginDTO userDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            var result = await _authManager.SignInUser(userDTO);

            if (!result.Succeeded)
            {
                return Unauthorized();
            }

            return Accepted(new { token = await _authManager.CreateToken()});
        }
    }
}
