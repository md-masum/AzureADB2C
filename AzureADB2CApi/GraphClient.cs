using System.Text.Json;
using Azure.Identity;
using Microsoft.Graph;

namespace AzureADB2CApi
{
    //Configure B2C Tenant https://docs.microsoft.com/en-us/azure/active-directory-b2c/microsoft-graph-get-started?tabs=app-reg-ga
    //Configure App to call graph https://docs.microsoft.com/en-us/graph/sdks/choose-authentication-providers?tabs=CS#client-credentials-provider
    //Sample Code https://github.com/Azure-Samples/ms-identity-dotnetcore-b2c-account-management/blob/master/src/Services/UserService.cs
    public class GraphClient
    {
        private readonly B2CCredentials _credentials;
        private readonly ILogger<GraphClient> _logger;
        public virtual GraphServiceClient GraphServiceClient { get; }
        public GraphClient(B2CCredentials credentials, ILogger<GraphClient> logger)
        {
            _credentials = credentials;
            _logger = logger;
            var scopes = new[] { "https://graph.microsoft.com/.default" };

            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };
            
            var clientSecretCredential = new ClientSecretCredential(
                credentials.TenantId, credentials.ClientId, credentials.ClientSecret, options);

            GraphServiceClient = new GraphServiceClient(clientSecretCredential, scopes);
        }

        public async Task<object> GetAllUser()
        {
            string roleAttributeName = GetAttributeFullName("Role");
            var users = await GraphServiceClient.Users
                .Request()
                .Select($"id,givenName,surName,displayName,identities,{roleAttributeName}")
                .GetAsync();
            return users;
        }

        public async Task<object> GetUser(string userId)
        {
            string roleAttributeName = GetAttributeFullName("Role");
            var user = await GraphServiceClient.Users[userId]
                .Request()
                .Select($"id,givenName,surName,displayName,identities,{roleAttributeName}")
                .GetAsync();
            return user;
        }

        public async Task<object> UpdateUser(string userId, UserUpdateModel userToUpdate)
        {
            string roleAttributeName = GetAttributeFullName("Role");
            var extensionInstance = RoleExtensionInstance(userToUpdate.Role);

            var user = new User
            {
                GivenName = userToUpdate.FirstName,
                Surname = userToUpdate.LastName,
                DisplayName = userToUpdate.DisplayName,
                AdditionalData = extensionInstance
            };
            var updatedUser = await GraphServiceClient.Users[userId]
                .Request()
                .Select($"id,givenName,surName,displayName,identities,{roleAttributeName}")
                .UpdateAsync(user);

            return updatedUser;
        }

        public async Task<object> AddUserRole(string userId, Roles roles)
        {
            string roleAttributeName = GetAttributeFullName("Role");
            var extensionInstance = RoleExtensionInstance(roles);

            var user = new User
            {
                AdditionalData = extensionInstance
            };

            var updatedUser = await GraphServiceClient.Users[userId]
                .Request()
                .Select($"id,givenName,surName,displayName,identities,{roleAttributeName}")
                .UpdateAsync(user);

            return updatedUser;
        }

        public async Task<List<object>> ListUsers()
        {
            _logger.LogInformation("Getting list of users...");
            string roleAttributeName = GetAttributeFullName("Role");

            List<object> userList = new List<object>();

            try
            {
                // Get all users
                var users = await GraphServiceClient.Users
                    .Request()
                    .Select($"id,givenName,surName,displayName,identities,{roleAttributeName}")
                    .GetAsync();

                // Iterate over all the users in the directory
                var pageIterator = PageIterator<User>
                    .CreatePageIterator(
                        GraphServiceClient,
                        users,
                        // Callback executed for each user in the collection
                        (user) =>
                        {
                            _logger.LogInformation(user.DisplayName);
                            userList.Add(user);
                            return true;
                        },
                        // Used to configure subsequent page requests
                        (req) =>
                        {
                            _logger.LogInformation($"Reading next page of users...");
                            return req;
                        }
                    );

                await pageIterator.IterateAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return userList;
        }

        public async Task DeleteUserById(string userId)
        {
            _logger.LogInformation($"Looking for user with object ID '{userId}'...");
            try
            {
                // Delete user by object ID
                await GraphServiceClient.Users[userId]
                    .Request()
                    .DeleteAsync();

                _logger.LogInformation($"User with object ID '{userId}' successfully deleted.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
            }
        }

        public async Task SetPasswordByUserId(string userId, string password)
        {
            _logger.LogInformation($"Looking for user with object ID '{userId}'...");

            var user = new User
            {
                PasswordPolicies = "DisablePasswordExpiration,DisableStrongPassword",
                PasswordProfile = new PasswordProfile
                {
                    ForceChangePasswordNextSignIn = false,
                    Password = password,
                }
            };

            try
            {
                // Update user by object ID
                await GraphServiceClient.Users[userId]
                    .Request()
                    .UpdateAsync(user);

                Console.WriteLine($"User with object ID '{userId}' successfully updated.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
            }
        }

        public async Task<object> CreateUserWithCustomAttribute(UserModel createUserObj)
        {
            if (string.IsNullOrWhiteSpace(_credentials.B2CExtensionAppClientId))
            {
                throw new ArgumentException("B2C Extension App ClientId (ApplicationId) is missing in the appsettings.json. Get it from the App Registrations blade in the Azure portal. The app registration has the name 'b2c-extensions-app. Do not modify. Used by AADB2C for storing user data.'.", nameof(GraphClient));
            }

            // Create custom attribute from azure B2C portal
            // Declare the names of the custom attributes
            string roleAttributeName = GetAttributeFullName("Role");
            var extensionInstance = RoleExtensionInstance(createUserObj.Role);

            try
            {
                // Create user
                var result = await GraphServiceClient.Users
                .Request()
                .AddAsync(new User
                {
                    GivenName = createUserObj.FirstName,
                    Surname = createUserObj.LastName,
                    DisplayName = createUserObj.DisplayName,
                    Identities = new List<ObjectIdentity>
                    {
                        new ObjectIdentity()
                        {
                            SignInType = "emailAddress",
                            Issuer = _credentials.Domain,
                            IssuerAssignedId = createUserObj.Email
                        }
                    },
                    PasswordProfile = new PasswordProfile()
                    {
                        Password = createUserObj.Password
                    },
                    PasswordPolicies = "DisablePasswordExpiration",
                    AdditionalData = extensionInstance
                });

                string userId = result.Id;

                _logger.LogInformation($"Created the new user. Now get the created user with object ID '{userId}'...");

                // Get created user by object ID
                result = await GraphServiceClient.Users[userId]
                    .Request()
                    .Select($"id,givenName,surName,displayName,identities,{roleAttributeName}")
                    .GetAsync();

                if (result != null)
                {
                    _logger.LogInformation($"DisplayName: {result.DisplayName}");
                    _logger.LogInformation($"Role: {result.AdditionalData[roleAttributeName]}");
                    _logger.LogInformation(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
                }

                return result;
            }
            catch (ServiceException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    _logger.LogError($"Have you created the custom attributes in your tenant?");
                    _logger.LogError(ex.Message);
                }

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw;
            }
        }

        private string GetAttributeFullName(string attributeName)
        {
            // Get the complete name of the custom attribute (Azure AD extension)
            Helpers.B2CCustomAttributeHelper helper = new Helpers.B2CCustomAttributeHelper(_credentials.B2CExtensionAppClientId);
            _logger.LogInformation($"Create a user with the custom attributes '{attributeName}' (string)");
            return helper.GetCompleteAttributeName(attributeName);
        }

        private IDictionary<string, object> RoleExtensionInstance(Roles roles)
        {
            string roleAttributeName = GetAttributeFullName("Role");

            // Fill custom attributes
            IDictionary<string, object> extensionInstance = new Dictionary<string, object>();
            extensionInstance.Add(roleAttributeName, roles.ToString());
            return extensionInstance;
        }
    }
}
