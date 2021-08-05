using Fastnet.Agents.Client.Services;
using Fastnet.Blazor.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Agents.Client.Shared
{
    public partial class MainLayout
    {
        [CascadingParameter] private Task<AuthenticationState> authenticationStateTask { get; set; }
        private int userCount = -1;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;
        [Inject] private IAuthenticationService authenticationService { get; set; } = default!;
        protected override async Task OnInitializedAsync()
        {
            userCount = await ((AuthenticationService)authenticationService).GetUserCount();
            var user = (await authenticationStateTask).User;
        }
        private bool ShowRegistrationItem()
        {
            return userCount == 0;
        }
    }
}
