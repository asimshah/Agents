using Fastnet.Blazor.Core;
using Fastnet.Core.Shared;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fastnet.Agents.Client.Services
{
    public class AuthenticationService : AuthenticationServiceBase
    {
        public AuthenticationService(ILogger<AuthenticationService> logger, HttpClient client,
            AuthenticationStateProvider authStateProvider, LocalStorage localStorage) : base(logger, client, authStateProvider, localStorage)
        {

        }

        public override async Task<WebApiResult<string>> LoginAsync<LoginModel>(LoginModel userForLogin)
        {
            var result = await PostAsync<LoginModel, string>("accounts/login", userForLogin);
            if (result.Success)
            {
                var token = result.Data;
                await SetTokenAsync(token, userForLogin.Email);
            }
            return result;
        }
        public override Task LogoutAsync()
        {
            return base.LogoutAsync();
        }
        public override async Task<WebApiResult<string>> RefreshTokenAsync()
        {
            var result = await GetAsync<string>("accounts/refresh");
            if (result.Success)
            {
                await RefreshTokenAsync(result.Data);
            }
            return result;
        }
        public override async Task<WebApiResult<UserAccountDTO>> RegisterUserAsync<UserRegistration>(UserRegistration userForRegistration)
        {
            var result = await PostAsync<UserRegistration, UserAccountDTO>("accounts/register", userForRegistration);
            return result;
        }
        public async Task<int> GetUserCount()
        {
            var war = await GetAsync<int>("accounts/get/user/count");
            if (war.Success)
            {
                return war.Data;
            }
            return 0;
        }
    }
}
