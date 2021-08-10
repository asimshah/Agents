using Fastnet.Agents.Server.Models;
using Fastnet.Core.Shared;
using Fastnet.Core.UserAccounts;
using Fastnet.Core.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Agents.Server.Controllers
{
    [Route("accounts")]
    [ApiController]
    public class AccountsController : AccountsControllerBase
    {
        private readonly UserManager userManager;
        private readonly AgentsDb db;
        public AccountsController(UserManager um, AgentsDb db)
        {
            this.db = db;
            this.userManager = um;
        }
        [HttpPost("register")]
        public async Task<IActionResult> RegisterUser([FromBody] RegistrationModel newUserModel)
        {
            var result = await base.RegisterNewUserAsync(newUserModel);
            if (result.IsSuccess())
            {
                Debug.Assert(result is OkObjectResult);
                UserAccountDTO user = (result as OkObjectResult).Value as UserAccountDTO;
                // do more with user but remember this user has already been created in dbo.UserAccounts by UserManager
                await db.SaveChangesAsync();
            }
            return result;
        }
        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync([FromBody] LoginModel loginUserModel)
        {
            //ModelState.AddModelError("Email", "Email is not valid");
            //ModelState.AddModelError("Password", "Password is not valid");
            return await base.LoginUserAsync(loginUserModel);
        }
        [Authorize]
        [HttpGet("refresh")]
        public async Task<string> Refresh()
        {
            return await base.RefreshCurrentUserTokenAsync();

        }
        [HttpGet("get/user/count")]
        public IActionResult GetUserCount()
        {
            return Ok(userManager.GetUserCount());
        }
    }
}
