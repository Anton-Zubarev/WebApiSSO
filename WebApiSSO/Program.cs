using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.WsFederation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Tokens.Saml;
using Microsoft.IdentityModel.Tokens.Saml2;
using WebApiSSO;

IdentityModelEventSource.ShowPII = true;

var certProvider = new SamlCertificateProvider();

var builder = WebApplication.CreateBuilder(args);
//переоределить праметры, на проде - wsFed, на других - windows
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false);

builder.Services.AddAuthentication(sharedOptions =>
{
    sharedOptions.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    sharedOptions.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    sharedOptions.DefaultChallengeScheme = WsFederationDefaults.AuthenticationScheme;
})
.AddCookie()
.AddWsFederation(options =>
{
    options.MetadataAddress = "https://sso.carcade.com/FederationMetadata/2007-06/FederationMetadata.xml";
    /*
    // FederationMetadata.xml скачивается с задержкой, из-за этого в недрах WsFederation что-то не срабатывает
    // попробовал этот метод - тоже не сработало
    // Тогда взял заранее скачанный FederationMetadata.xml и использовал данные в TokenValidationParameters
    options.ConfigurationManager = new ConfigurationManager<WsFederationConfiguration>(
        "https://sso.carcade.com/FederationMetadata/2007-06/FederationMetadata.xml",
        new WsFederationConfigurationRetriever(),
        new FileDocumentRetriever()
    );*/

    options.Wtrealm = builder.Configuration["AppUrl"]; // конечная точка, должна быть записана в ADFS.  Реализация в пакете swFederation, переделывать не надо
    if (!string.IsNullOrEmpty(builder.Configuration["WsfedCallback"])) options.CallbackPath = builder.Configuration["wsfed_callback"];

    options.BackchannelHttpHandler = new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true, 
        UseDefaultCredentials = true,
        AutomaticDecompression= System.Net.DecompressionMethods.All,
        CheckCertificateRevocationList = false
    };
    options.BackchannelTimeout = TimeSpan.FromSeconds(40);

    //options.TokenHandlers.Add(new SamlSecurityTokenHandler());
    options.SecurityTokenHandlers.Clear();
    options.SecurityTokenHandlers.Add(new Saml2SecurityTokenHandler()); 
    options.SecurityTokenHandlers.Add(new SamlSecurityTokenHandler());

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = certProvider.ValidIssuer,
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidAudience = options.Wtrealm,
        IssuerSigningKey = new X509SecurityKey(certProvider.Certificate)
    };
});

if (builder.Configuration["AuthMethod"] == "wsfed")
{
    builder.Services.AddAuthorization(options =>
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    });
}
else
{
    builder.Services.AddAuthorization();
}


var app = builder.Build();

//порядок важен
app.UseAuthentication(); 
app.UseAuthorization();

//app.UseHttpsRedirection(); //не надо

app.UseDefaultFiles();
app.UseStaticFiles();

/*
// не надо - UseStaticFiles лучше работает, он понимает js/css
app.MapGet("/{id}/{tp?}/{cmr?}", async context =>
{
    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync("wwwroot/index.html");
})
 .WithMetadata(new { Constraints = new { id = @"^[^\.]+$", tp = @"^[^\.]+$", cmr = @"^[^\.]+$" } });*/

app.MapFallbackToFile("index.html");
app.Run();

