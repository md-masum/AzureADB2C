using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using AzureADB2CSpa;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddHttpClient("AzureADB2CSpa.ServerAPI", client => 
        client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("ApiBaseUrl")))
    .AddHttpMessageHandler(sp =>
    {
        var handler = sp.GetService<AuthorizationMessageHandler>()!
            .ConfigureHandler(
                authorizedUrls: new[] { builder.Configuration.GetValue<string>("ApiBaseUrl") }, //<--- The URI used by the Server project.
                scopes: new[] { builder.Configuration.GetValue<string>("AzureAdB2C:Scope") });
        return handler;
    });

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>()
    .CreateClient("AzureADB2CSpa.ServerAPI"));

builder.Services.AddMsalAuthentication( options =>
{
    builder.Configuration.Bind("AzureAdB2C", options.ProviderOptions.Authentication);
}).AddAccountClaimsPrincipalFactory<CustomAccountClaimsPrincipalFactory>();

await builder.Build().RunAsync();
