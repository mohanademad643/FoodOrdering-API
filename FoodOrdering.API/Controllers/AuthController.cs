//using FoodOrdering.Application.Features.Auth.Login;
//using FoodOrdering.Application.Features.Auth.RefreshTokens.Commands;
//using FoodOrdering.Application.Features.Auth.Register.Commands;
//using FoodOrdering.Application.Features.Auth.RevokeToken.Commands;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;

//namespace FoodOrdering.API.Controllers
//{
//    public class AuthController : BaseController
//    {
//        [HttpPost("register")]
//        public async Task<IActionResult> Register([FromBody] RegisterCommand command)
//        {
//            var result = await Mediator.Send(command);
//            return StatusCode(result.StatusCode, result);
//        }

//        [HttpPost("login")]
//        public async Task<IActionResult> Login([FromBody] LoginCommand command)
//        {
//            var result = await Mediator.Send(command);

//            if (result.Success && result.Data != null)
//            {
//                AppendAccessTokenCookie(result.Data.AccessToken, result.Data.AccessTokenExpiry);
//                AppendRefreshTokenCookie(result.Data.RefreshToken);
//            }

//            var safeResult = result.Success ? new
//            {
//                result.Success,
//                result.Message,
//                result.StatusCode,
//                Data = new
//                {
//                    result.Data!.AccessToken,
//                    result.Data.AccessTokenExpiry,
//                    result.Data.User
//                }
//            } : (object)result;

//            return StatusCode(result.StatusCode, safeResult);
//        }

//        [HttpPost("refresh-token")]
//        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest? body)
//        {
//            var accessToken = body?.AccessToken ?? string.Empty;
//            var refreshToken = body?.RefreshToken ?? Request.Cookies["refreshToken"] ?? string.Empty;

//            var result = await Mediator.Send(new RefreshTokenCommand(accessToken, refreshToken));

//            if (result.Success && result.Data != null)
//            {
//                AppendAccessTokenCookie(result.Data.AccessToken, result.Data.AccessTokenExpiry);
//                AppendRefreshTokenCookie(result.Data.RefreshToken);
//            }

//            return StatusCode(result.StatusCode, result);
//        }

//        [HttpPost("revoke-token")]
//        [Authorize]
//        public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequest? body)
//        {
//            var tokenToRevoke = body?.RefreshToken ?? Request.Cookies["refreshToken"] ?? string.Empty;

//            DeleteAuthCookies();

//            var result = await Mediator.Send(new RevokeTokenCommand(tokenToRevoke));
//            return StatusCode(result.StatusCode, result);
//        }

//        [HttpPost("logout")]
//        [Authorize]
//        public async Task<IActionResult> Logout()
//        {
//            var refreshToken = Request.Cookies["refreshToken"] ?? string.Empty;
//            DeleteAuthCookies();

//            if (!string.IsNullOrEmpty(refreshToken))
//                await Mediator.Send(new RevokeTokenCommand(refreshToken));

//            return Ok(new { Success = true, Message = "Logged out successfully." });
//        }

//        [HttpGet("me")]
//        [Authorize]
//        public IActionResult Me()
//        {
//            return Ok(new
//            {
//                UserId = CurrentUserId,
//                Email = CurrentUserEmail,
//                IsAdmin,
//                Language = CurrentLanguage
//            });
//        }

//        private void AppendAccessTokenCookie(string token, DateTime expiry)
//        {
//            Response.Cookies.Append("accessToken", token, new CookieOptions
//            {
//                HttpOnly = true,
//                Secure = true,
//                SameSite = SameSiteMode.Strict,
//                Expires = new DateTimeOffset(expiry)
//            });
//        }

//        private void AppendRefreshTokenCookie(string token)
//        {
//            Response.Cookies.Append("refreshToken", token, new CookieOptions
//            {
//                HttpOnly = true,
//                Secure = true,
//                SameSite = SameSiteMode.Strict,
//                Expires = DateTimeOffset.UtcNow.AddDays(7)
//            });
//        }

//        private void DeleteAuthCookies()
//        {
//            Response.Cookies.Delete("accessToken");
//            Response.Cookies.Delete("refreshToken");
//        }
//    }

//    public record RefreshTokenRequest(string AccessToken, string? RefreshToken);
//    public record RevokeTokenRequest(string? RefreshToken);
//}
using FoodOrdering.Application.Features.Auth.Login;
using FoodOrdering.Application.Features.Auth.RefreshTokens.Commands;
using FoodOrdering.Application.Features.Auth.Register.Commands;
using FoodOrdering.Application.Features.Auth.RevokeToken.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodOrdering.API.Controllers
{
    public class AuthController : BaseController
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterCommand command)
        {
            var result = await Mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginCommand command)
        {
            var result = await Mediator.Send(command);

            if (!result.Success || result.Data is null)
                return StatusCode(result.StatusCode, result);

            // FIX: set cookies only when running over HTTPS (i.e. on the host).
            // On some shared hosts the request comes in over HTTP internally,
            // which caused Secure cookies to be silently dropped → tokens never
            // reached the client → every call after login returned 401.
            var isHttps = Request.IsHttps;

            AppendAccessTokenCookie(result.Data.AccessToken, result.Data.AccessTokenExpiry, isHttps);
            AppendRefreshTokenCookie(result.Data.RefreshToken, isHttps);

            // FIX: the old anonymous-type branch used result.Data! with a null-
            // forgiving operator, but result.Data was already null-checked above.
            // More importantly, if result.Success were true but Data were somehow
            // null the original code would throw a NullReferenceException → 500.
            return StatusCode(result.StatusCode, new
            {
                result.Success,
                result.Message,
                result.StatusCode,
                Data = new
                {
                    result.Data.AccessToken,
                    result.Data.AccessTokenExpiry,
                    result.Data.User
                }
            });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest? body)
        {
            var accessToken = body?.AccessToken ?? string.Empty;
            var refreshToken = body?.RefreshToken ?? Request.Cookies["refreshToken"] ?? string.Empty;

            var result = await Mediator.Send(new RefreshTokenCommand(accessToken, refreshToken));

            if (result.Success && result.Data is not null)
            {
                var isHttps = Request.IsHttps;
                AppendAccessTokenCookie(result.Data.AccessToken, result.Data.AccessTokenExpiry, isHttps);
                AppendRefreshTokenCookie(result.Data.RefreshToken, isHttps);
            }

            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("revoke-token")]
        [Authorize]
        public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequest? body)
        {
            var tokenToRevoke = body?.RefreshToken ?? Request.Cookies["refreshToken"] ?? string.Empty;
            DeleteAuthCookies();
            var result = await Mediator.Send(new RevokeTokenCommand(tokenToRevoke));
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refreshToken"] ?? string.Empty;
            DeleteAuthCookies();

            if (!string.IsNullOrEmpty(refreshToken))
                await Mediator.Send(new RevokeTokenCommand(refreshToken));

            return Ok(new { Success = true, Message = "Logged out successfully." });
        }

        [HttpGet("me")]
        [Authorize]
        public IActionResult Me() => Ok(new
        {
            UserId = CurrentUserId,
            Email = CurrentUserEmail,
            IsAdmin,
            Language = CurrentLanguage
        });

        // ─────────────────────────────────────────────────────────────────────
        // Cookie helpers
        // ─────────────────────────────────────────────────────────────────────

        private void AppendAccessTokenCookie(string token, DateTime expiry, bool secure)
        {
            Response.Cookies.Append("accessToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = secure,          // FIX: only Secure on HTTPS
                SameSite = SameSiteMode.Strict,
                Expires = new DateTimeOffset(expiry)
            });
        }

        private void AppendRefreshTokenCookie(string token, bool secure)
        {
            Response.Cookies.Append("refreshToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = secure,          // FIX: only Secure on HTTPS
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });
        }

        private void DeleteAuthCookies()
        {
            Response.Cookies.Delete("accessToken");
            Response.Cookies.Delete("refreshToken");
        }
    }

    public record RefreshTokenRequest(string AccessToken, string? RefreshToken);
    public record RevokeTokenRequest(string? RefreshToken);
}