using Asp.netDemoShared;
using identity_web_api_core.Models;
using identity_web_api_core.Pages;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace identity_web_api_core.Services
{
    public interface IUserService
    {

        Task<UserManagerResponse> RegisterUserAsync(RegisterViewModel model);

        Task<UserManagerResponse> LoginUserAsync(LoginViewModel Model);

        Task<UserManagerResponse> ConfirmEmailAsync(string UserId, string token);

        Task<UserManagerResponse> ForgetPasswordAsync(string email);

        Task<UserManagerResponse> ResetPasswordAsync(ResetPasswordViewModel model);
    }

    public class UserService : IUserService
    {
        private UserManager<IdentityUser> _userManager;

        private IConfiguration _configuration;
        private IMailService _mailService;
        public UserService(UserManager<IdentityUser> userManager, IConfiguration configuration, IMailService mailService)
        {
            _userManager = userManager;
            _configuration = configuration;
            _mailService = mailService;
        }
      public async  Task<UserManagerResponse> RegisterUserAsync(RegisterViewModel model)
        {
            if (model == null)
            {
                throw new NotImplementedException("model is null");
            }

            if(model.Password != model.ConfirmPassword)
            {
                return new UserManagerResponse
                {
                    Message = "password do not match  ",
                    IsSuccess = false,
                };
            }

            var identityUser = new IdentityUser
            {

                Email = model.Email,
                UserName = model.Email,
            };

            var result = await _userManager.CreateAsync(identityUser, model.Password);

            if (result.Succeeded)
            {
                //TODO send confirmation Email
                var confirmEmailToken = await _userManager.GenerateEmailConfirmationTokenAsync(identityUser);
                var encodedEmailToken = Encoding.UTF8.GetBytes(confirmEmailToken);
                var validToken = WebEncoders.Base64UrlEncode(encodedEmailToken);

                string url = $"{_configuration["AppUrl"]}/api/auth/ConfirmEmailAsync?UserId={identityUser.Id}&token={validToken}";

                await _mailService.SendEmailAsync(identityUser.Email, "Email confirmation", $"<h1>Thanks for registering</h1>" +
                                                  $"please click this link to confitm your email <a href='{url}'>Confirm here</a>");
                return new UserManagerResponse
                {
                    IsSuccess = true,
                    Message = "User have been create successfuly",
                    
                };
            }
            return new UserManagerResponse
            {
                IsSuccess = false,
                Message = "User did not create",
                Errors = result.Errors.Select(e => e.Description)
            };
           
        }




        public async Task<UserManagerResponse> LoginUserAsync(LoginViewModel model)
        {

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return new UserManagerResponse
                {
                    Message ="there is no user with that email",
                    IsSuccess=false,
                };
            }

            var result = await _userManager.CheckPasswordAsync(user, model.Password);
           if(!result)
            {
                return new UserManagerResponse
                {
                    Message = "your password is incorrect",
                    IsSuccess = false,
                };
            }

            var claims = new[]
            {
            new Claim("Email", model.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id)
           };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["AuthSettings:key"]));
            var token = new JwtSecurityToken
                (
                issuer: _configuration["AuthSettings:Issuer"],
                audience: _configuration["AuthSettings:audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(30),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

            string tokenAsString = new JwtSecurityTokenHandler().WriteToken(token);

            return new UserManagerResponse
            {
                Message = tokenAsString,
                IsSuccess = true,
                ExpireDate= token.ValidTo
            };

        }

        public async Task<UserManagerResponse> ConfirmEmailAsync(string UserId, string token)
        {
            var user = await _userManager.FindByIdAsync(UserId);
            if(user == null)
            {
                return new UserManagerResponse
                {
                    IsSuccess = false,
                    Message = "user not found"
                };
            }    
            else
            {
                var decodedToken = WebEncoders.Base64UrlDecode(token);
                var nornaltoken = Encoding.UTF8.GetString(decodedToken);

                var result = await _userManager.ConfirmEmailAsync(user, nornaltoken);

                if (result.Succeeded)
                {

                    return new UserManagerResponse
                    {
                        Message = "Email confirmed successfuly",
                        IsSuccess = true
                    };
                }
                else
                {

                    return new UserManagerResponse
                    {

                        Message = "Email did not confirm",
                        IsSuccess = true,
                        Errors = result.Errors.Select(e => e.Description)
                    };
                }

            }
        }

        public async Task<UserManagerResponse> ForgetPasswordAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return new UserManagerResponse
                {
                    Message = "no user assiciated by this account",
                    IsSuccess = false,
                };

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var encodedToken = Encoding.UTF8.GetBytes(token);
            var ValidToken = WebEncoders.Base64UrlEncode(encodedToken);

            string url = $"{_configuration["AppUrl"]}/ResetPassword?email={email}&token={ValidToken}";

            await _mailService.SendEmailAsync(email, "Rest Password", "<h1> Follow instruction to reset the password</h1>" +
                $"<p>To reset the password please <a href='{url}'>Click Here</a></p>");

            return new UserManagerResponse
            {
                Message = "Reser password link have been sent to you on your email",
                IsSuccess = true 
        };

        }

        public async Task<UserManagerResponse> ResetPasswordAsync(ResetPasswordViewModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return new UserManagerResponse
                {
                    Message = "no user assiciated by this account",
                    IsSuccess = false,
                };

            if (model.NewPassword != model.ConfirmPassword)
                return new UserManagerResponse
                {
                    IsSuccess = false,
                    Message = "your password do not match"
                };

            var decodedToken = WebEncoders.Base64UrlDecode(model.token);
            var nornaltoken = Encoding.UTF8.GetString(decodedToken);

            var result = await _userManager.ResetPasswordAsync(user, nornaltoken, model.NewPassword);
          

            if (result.Succeeded)
                return new UserManagerResponse
                {
                    Message = "password have been reset successfuly",
                    IsSuccess = true,
                    
                };
            return new UserManagerResponse
            {
                Message = "Something went wrong",
                IsSuccess = false,
                Errors = result.Errors.Select(e => e.Description)
            };
                
        }
    }
}
