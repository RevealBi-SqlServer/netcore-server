// The `AuthenticationProvider` class implements the `IRVAuthenticationProvider` interface 
// to provide custom authentication for Reveal BI data sources. The purpose of this provider 
// is to supply credentials that Reveal BI uses to authenticate to databases, enabling secure 
// access to data sources in Reveal BI dashboards.
//
// Purpose:
// The `AuthenticationProvider` retrieves credentials for a specified data source, using configuration 
// settings, secret management, or other means to supply credentials securely. This is essential for 
// connecting to external data sources (like SQL Server) and is invoked by setting the authentication 
// provider in the application's dependency injection (DI) container.
//
// Authentication Setup:
// This provider must be registered in the DI container in `Program.cs` with the following call:
// `.AddAuthenticationProvider<AuthenticationProvider>()`, allowing Reveal BI to automatically use 
// this authentication provider when a data source requires credentials.
//
// Key Components and Methods:
// - `_config`: An instance of `IConfiguration`, injected via constructor dependency injection. This 
//   provides access to app settings, secrets, and configuration values for retrieving database credentials 
//   (such as from a secure vault or environment variables).
//
// - `ResolveCredentialsAsync(IRVUserContext userContext, RVDashboardDataSource dataSource)`:
//   This method asynchronously resolves credentials for a specific data source. It accepts a `userContext` 
//   parameter (for user-specific credentials) and `dataSource`, which represents the requested data source.
//   It returns an `IRVDataSourceCredential`, used to authenticate Reveal BI to that data source.
//
// Usage Notes:
// - The method checks if the data source is of type `RVSqlServerDataSource` and provides SQL Server 
//   credentials in the form of `RVUsernamePasswordDataSourceCredential`, using username and password 
//   values retrieved from `_config`. The credentials in this demo are currently stored in UserSecrets, 
//   in production ensure they are securely stored and retrieved (e.g., from app secrets or a key vault).
//
// Reference Links:
// - Reveal BI Authentication Documentation: https://help.revealbi.io/web/authentication/ 
// - App Secrets Management in .NET: https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-8.0&tabs=windows


using Reveal.Sdk;
using Reveal.Sdk.Data;
using Reveal.Sdk.Data.Microsoft.SqlServer;

namespace RevealSdk.Server.Reveal
{
    public class AuthenticationProvider : IRVAuthenticationProvider
    {
        // ***
        // For AppSettings / Secrets retrieval
        // https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-8.0&tabs=windows
        // ***
        private readonly IConfiguration _config;

        // Constructor that accepts IConfiguration as a dependency
        public AuthenticationProvider(IConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }
        // ***

        public Task<IRVDataSourceCredential> ResolveCredentialsAsync(IRVUserContext userContext,
            RVDashboardDataSource dataSource)
        {
            // Create a userCredential object
            IRVDataSourceCredential? userCredential = null;

            // Check that the incoming request is for the expected data source type
            // or any of the information in your UserContext to determine what credentials to set if necessary
            if (dataSource is RVSqlServerDataSource)
            {
                // for SQL Server, add a username, password and optional domain
                // note these are just properties, you can set them from configuration, a key vault, a look up to 
                // database, etc.  They are pulled from UserSecrets for this example.
                userCredential = new RVUsernamePasswordDataSourceCredential(_config["SqlServer:Username"], _config["SqlServer:Password"]);
            }
            return Task.FromResult(userCredential);
        }
    }
}
