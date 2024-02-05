using System.Threading.Tasks;
using IdentityModel.Client;
using LtiAdvantage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using OpenIddict.Server;

namespace AuthorizationServer.Controllers;

public class LtiController : Controller
{
    private readonly IOptionsMonitor<OpenIddictServerOptions> _oidcOptions;

    public LtiController(
        IOptionsMonitor<OpenIddictServerOptions> oidcOptions
        )
    {
        _oidcOptions = oidcOptions;
    }
    
    public async Task<IActionResult> Launch()
    {
        
        var url = new RequestUrl("https://saltire.lti.app/tool").Create(new
        {
            iss = "https://c86c-2a00-23c8-7589-3201-5802-edc5-495e-55ed.ngrok-free.app/",
            login_hint = "1234",
            target_link_uri = "https://saltire.lti.app/tool",
            lti_message_hint = JsonConvert.SerializeObject(new
            {
                id = "1234",
                messageType = "basic-lti-launch-request",
                courseId = "6789"
            })
        });

        return Redirect(url);
    }
}