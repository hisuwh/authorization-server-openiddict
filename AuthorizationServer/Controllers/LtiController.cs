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
        
        var url = new RequestUrl("https://aquilalearning.moodlecloud.com/enrol/lti/login.php?id=c1ca1fccd4468561260cb4bbf18493ca52ff93d3b9727c36051627ce2d77").Create(new
        {
            iss = "https://c86c-2a00-23c8-7589-3201-5802-edc5-495e-55ed.ngrok-free.app/",
            login_hint = "1234",
            target_link_uri = "https://aquilalearning.moodlecloud.com/enrol/lti/launch.php",
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