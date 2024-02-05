using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace AuthorizationServer
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                {
                    options.LoginPath = "/account/login";
                });
            
            services.AddDbContext<DbContext>(options =>
            {
                // Configure the context to use an in-memory store.
                options.UseInMemoryDatabase(nameof(DbContext));

                // Register the entity sets needed by OpenIddict.
                options.UseOpenIddict();
            });

            services.AddOpenIddict()

                // Register the OpenIddict core components.
                .AddCore(options =>
                {
                    // Configure OpenIddict to use the EF Core stores/models.
                    options.UseEntityFrameworkCore()
                        .UseDbContext<DbContext>();
                })

                // Register the OpenIddict server components.
                .AddServer(options =>
                {
                    options
                        .AllowClientCredentialsFlow()
                        .AllowAuthorizationCodeFlow()
                            .RequireProofKeyForCodeExchange()
                        .AllowRefreshTokenFlow()
                        .AllowImplicitFlow()
                        ;

                    options
                        .SetTokenEndpointUris("/connect/token")
                        .SetAuthorizationEndpointUris("/connect/authorize")
                        .SetUserinfoEndpointUris("/connect/userinfo");

                    // Encryption and signing of tokens
                    options
                        .AddEphemeralEncryptionKey()
//                         .AddEncryptionKey(new JsonWebKey(@"{
//     ""kty"": ""RSA"",
//     ""n"": ""sH8_-uavYxkWoEXm0QHDrZbfWByo0pEQdpy-EEdiQU_LVxlS4Et-ArUVq28hf1PRgGxRGEMzVXddyUgrrYuPV_17okqZZshfJnjqUpcN5d-mkyIs3XO-DLqI2UIoNXtEP5zlWvJTkqzUUlXg9y3QIHM_-1j8G3KeJKxIhezuLIUMJLSfJv3CgKF6CHPCT0JLPbOEStDCzzqQwIulDhU3Ts6N4CPttOoG8w9FS0Z6fJjYWeeztAtstBggXw4_Hgq7_-TaxV8tct5rWighV50Z5SJA1xi7w4GlfvV4EpwixUfSOZzAN_RAzFoiq6MgBCl-rtb7mCAxuSfkD5xSoMe0rw"",
//     ""e"": ""AQAB"",
//     ""alg"": ""RS256"",
//     ""use"": ""sig""
// }"))
                        .AddEphemeralSigningKey()
                        .DisableAccessTokenEncryption();

                    // Register scopes (permissions)
                    options
                        .RegisterScopes("api")
                        // .RegisterClaims("some claim")
                        ;

                    // Register the ASP.NET Core host and configure the ASP.NET Core-specific options.
                    options
                        .UseAspNetCore()
                        .EnableTokenEndpointPassthrough()
                        .EnableAuthorizationEndpointPassthrough()
                        .EnableUserinfoEndpointPassthrough();            
                });
            
            services.AddHostedService<TestData>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();

            app.UseRouting();
            
            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });
        }
    }
}