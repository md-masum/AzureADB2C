using System.Security.Claims;
using AzureADB2CApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//     .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
//     {
//         options.Authority = "https://login.microsoftonline.com/4450c437-fa1b-4e4b-b7d3-d5c953c1ac98/v2.0";
//         options.Audience = "api://08446908-ff71-4c4b-a21a-a83fc5f87be0";
//         options.TokenValidationParameters.ValidIssuer =
//             "https://sts.windows.net/4450c437-fa1b-4e4b-b7d3-d5c953c1ac98/";
//     });
// builder.Services.Configure<B2CCredentials>(
//     builder.Configuration.GetSection("AzureAdB2C"));
builder.Services.AddSingleton(builder.Configuration.GetSection("AzureAdB2C").Get<B2CCredentials>());
builder.Services.AddSingleton<GraphClient>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAdB2C"));

builder.Services.Configure<JwtBearerOptions>(
    JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters.NameClaimType = "name";
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = opt =>
            {
                string role = opt.Principal.FindFirstValue("extension_Role");
                if (role is not null)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Role, role)
                    };

                    var appIdentity = new ClaimsIdentity(claims);
                    opt.Principal?.AddIdentity(appIdentity);
                }
                return Task.CompletedTask;
            }
        };
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(policy =>
    policy.WithOrigins("https://localhost:7245", "https://localhost:7253", "https://localhost:7056")
        .AllowAnyMethod()
        .WithHeaders(HeaderNames.ContentType, HeaderNames.Authorization, "x-custom-header")
        .AllowCredentials());
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
