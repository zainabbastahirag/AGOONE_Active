using AgOne.Shared.Auth.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace AgOne.Shared.Auth.Components;

public partial class AgOneAuthGuard : ComponentBase
{
    [Inject] private IAgOneSsoService SsoService { get; set; } = default!;

    /// <summary>
    /// Content to display when the user is authenticated.
    /// Provides the AuthenticationState context.
    /// </summary>
    [Parameter] public RenderFragment<AuthenticationState>? Authenticated { get; set; }

    /// <summary>
    /// Content to display while authentication is being verified.
    /// </summary>
    [Parameter] public RenderFragment? Loading { get; set; }

    /// <summary>
    /// Content to display when the user is not authenticated.
    /// If not provided, user is automatically redirected to login.
    /// </summary>
    [Parameter] public RenderFragment? NotAuthenticated { get; set; }

    /// <summary>
    /// Default child content (used when Authenticated template is not provided).
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    private bool _attemptingSilentLogin;

    protected override async Task OnInitializedAsync()
    {
        if (!await SsoService.IsAuthenticatedAsync())
        {
            _attemptingSilentLogin = true;
            StateHasChanged();

            // Attempt silent login using existing Entra ID session
            var success = await SsoService.TrySilentLoginAsync();

            _attemptingSilentLogin = false;

            if (!success && NotAuthenticated == null)
            {
                // No custom not-authenticated content, redirect to login
                SsoService.Login();
            }

            StateHasChanged();
        }
    }
}
