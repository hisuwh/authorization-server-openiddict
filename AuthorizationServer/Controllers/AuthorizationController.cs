using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LtiAdvantage.Lti;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace AuthorizationServer.Controllers
{
    public class AuthorizationController : Controller
    {
        [HttpGet("~/connect/authorize")]
        [HttpPost("~/connect/authorize")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Authorize()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
                          throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");
            
            // Retrieve the user principal stored in the authentication cookie.
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // If the user principal can't be extracted, redirect the user to the login page.
            if (!result.Succeeded)
            {
                return Challenge(
                    authenticationSchemes: CookieAuthenticationDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties
                    {
                        RedirectUri = Request.PathBase + Request.Path + QueryString.Create(
                            Request.HasFormContentType ? Request.Form.ToList() : Request.Query.ToList())
                    });
            }

            // Create a new claims principal
            var claims = new List<Claim>
            {
                // 'subject' claim which is required
                new Claim(OpenIddictConstants.Claims.Subject, result.Principal.Identity.Name),
                new Claim(OpenIddictConstants.Claims.Issuer, $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}"),
                // new Claim(OpenIddictConstants.Claims.Audience, "saltire.lti.app"),
                new Claim("some claim", "some value").SetDestinations(OpenIddictConstants.Destinations.AccessToken),
                new Claim(OpenIddictConstants.Claims.Email, "some@email").SetDestinations(OpenIddictConstants.Destinations.IdentityToken)
            };

            var ltiMessageHint = request.GetParameter("lti_message_hint");

            if (ltiMessageHint.HasValue)
            {
                var message = JToken.Parse(ltiMessageHint.Value.ToString());
                var id = message.Value<int>("id");
                var course = message.Value<int?>("courseId");
                var messageType = message.Value<string>("messageType");

                var resourceRequest = new LtiResourceLinkRequest
                {
                    Version = "1.3.0",
                    DeploymentId = "cLWwj9cbmkSrCNsckEFBmA",
                    FamilyName = "Griffin",
                    GivenName = "Stewie",
                    LaunchPresentation = new LaunchPresentationClaimValueType
                    {
                        DocumentTarget = DocumentTarget.Window,
                        Locale = CultureInfo.CurrentUICulture.Name,
                        ReturnUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}"
                    },
                    Lis = new LisClaimValueType
                    {
                        PersonSourcedId = "test-person-sis-id",
                        CourseSectionSourcedId = "test-course-sis-id"
                    },
                    Lti11 = new LtiMigrationClaimValueType
                    {
                        UserId = "user-1234",
                    },
                    Platform = new PlatformClaimValueType
                    {
                        ContactEmail = "test@example.com",
                        Description = "Test LTI imp",
                        Guid = Guid.NewGuid().ToString(),
                        Name = "LTI Test",
                        ProductFamilyCode = "abcde",
                        Url = "https://c86c-2a00-23c8-7589-3201-5802-edc5-495e-55ed.ngrok-free.app/",
                        Version = "0.0.1"
                    },
                    ResourceLink = new ResourceLinkClaimValueType
                    {
                        Id = $"{id}",
                        Title = "Resource title",
                        Description = "Resource description"
                    },
                    Roles = new Role[]
                    {
                        Role.SystemUser,
                        Role.ContextLearner
                    },
                    TargetLinkUri = "https://saltire.lti.app/tool",
                    Context = new ContextClaimValueType
                    {
                        Id = $"{course}",
                        Title = "Test course",
                        Type = new []
                        {
                            ContextType.CourseSection
                        }
                    }
                };

                foreach (var resourceClaim in resourceRequest.Claims)
                {
                    resourceClaim.SetDestinations(OpenIddictConstants.Destinations.IdentityToken);
                    claims.Add(resourceClaim);
                }
            }

            var claimsIdentity = new ClaimsIdentity(claims, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            // Set requested scopes (this is not done automatically)
            claimsPrincipal.SetScopes(request.GetScopes());
            // claimsPrincipal.SetResources("saltire.lti.app");
            

            // Signing in with the OpenIddict authentiction scheme trigger OpenIddict to issue a code (which can be exchanged for an access token)
            return SignIn(claimsPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
        
        [HttpPost("~/connect/token")]
        public async Task<IActionResult> Exchange()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
                          throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            ClaimsPrincipal claimsPrincipal;

            if (request.IsClientCredentialsGrantType())
            {
                // Note: the client credentials are automatically validated by OpenIddict:
                // if client_id or client_secret are invalid, this action won't be invoked.

                var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

                // Subject (sub) is a required field, we use the client id as the subject identifier here.
                identity.AddClaim(OpenIddictConstants.Claims.Subject, request.ClientId ?? throw new InvalidOperationException());

                // Add some claim, don't forget to add destination otherwise it won't be added to the access token.
                identity.AddClaim("some-claim", "some-value", OpenIddictConstants.Destinations.AccessToken);

                claimsPrincipal = new ClaimsPrincipal(identity);

                claimsPrincipal.SetScopes(request.GetScopes());
            }

            else if (request.IsAuthorizationCodeGrantType())
            {
                // Retrieve the claims principal stored in the authorization code
                claimsPrincipal = (await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal;
            }
            
            else if (request.IsRefreshTokenGrantType())
            {
                // Retrieve the claims principal stored in the refresh token.
                claimsPrincipal = (await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal;
            }

            else
            {
                throw new InvalidOperationException("The specified grant type is not supported.");
            }

            // Returning a SignInResult will ask OpenIddict to issue the appropriate access/identity tokens.
            return SignIn(claimsPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
        
        [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
        [HttpGet("~/connect/userinfo")]
        public async Task<IActionResult> Userinfo()
        {
            var claimsPrincipal = (await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal;

            return Ok(new
            {
                Name = claimsPrincipal.GetClaim(OpenIddictConstants.Claims.Subject),
                Occupation = "Developer",
                Age = 43
            });
        }
    }
}