using ECommerce.Identity.Application.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Win32;
using System.Collections.Generic;
using static ECommerce.Identity.Application.DTOs.AuthDtos;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ECommerce.Identity.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IValidator<RegisterRequest> _registerValidator;

        public AuthController(IAuthService authService,
                              IValidator<RegisterRequest> registerValidator)
        {
            _authService = authService;
            _registerValidator = registerValidator;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var validation = await _registerValidator.ValidateAsync(request);
            if (!validation.IsValid)
                return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

            var (response, error) = await _authService.RegisterAsync(request);
            if (error is not null) return Conflict(new { message = error });

            return Ok(response);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var (response, error) = await _authService.LoginAsync(request);
            if (error is not null)
                return Unauthorized(new { message = error });

            return Ok(response);
        }

        [HttpPost("register/seller")]
        public async Task<IActionResult> RegisterSeller([FromBody] RegisterRequest request)
        {
            var validation = await _registerValidator.ValidateAsync(request);
            if (!validation.IsValid)
                return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

            var (response, error) = await _authService.RegisterAsSellerAsync(request);
            if (error is not null) return Conflict(new { message = error });

            return Ok(response);
        }


        [HttpPatch("users/{userId}/role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUserRole(
        Guid userId,
        [FromBody] UpdateRoleRequest request)
        {
            var (success, error) = await _authService.UpdateUserRoleAsync(userId, request.Role);
            if (error is not null) return BadRequest(new { message = error });

            return Ok(new { message = $"Role updated to {request.Role} successfully." });
        }

        public record UpdateRoleRequest(string Role);


        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            var (response, error) = await _authService.RefreshTokenAsync(request.RefreshToken);
            if (error is not null)
                return Unauthorized(new { message = error });

            return Ok(response);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
        {
            await _authService.LogoutAsync(request.RefreshToken);
            return NoContent();
        }

        [HttpGet("me")]
        [Authorize]
        public IActionResult Me()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            return Ok(new { userId, email, role });
        }
    }
}
