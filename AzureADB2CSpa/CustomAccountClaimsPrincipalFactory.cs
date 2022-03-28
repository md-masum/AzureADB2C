using System.Security.Claims;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;

namespace AzureADB2CSpa
{
    public class CustomAccountClaimsPrincipalFactory : AccountClaimsPrincipalFactory<RemoteUserAccount>
    {
        public CustomAccountClaimsPrincipalFactory(IAccessTokenProviderAccessor accessor)
            : base(accessor) { }

        public override async ValueTask<ClaimsPrincipal> CreateUserAsync(RemoteUserAccount account, RemoteAuthenticationUserOptions options)
        {
            var user = await base.CreateUserAsync(account, options);

            if (user.Identity is {IsAuthenticated: false})
            {
                return user;
            }

            if (user.Identity is not ClaimsIdentity identity)
            {
                return user;
            }

            var roleClaims = identity.FindAll("extension_Role").ToArray();

            if (!roleClaims.Any())
            {
                return user;
            }

            foreach (var roleClaim in roleClaims)
            {
                try
                {
                    var roleNames = roleClaim.Value;
                    identity.AddClaim(new Claim(ClaimTypes.Role, roleNames));
                }
                catch
                {
                    // continue
                }
            }

            return user;
        }
    }
}
