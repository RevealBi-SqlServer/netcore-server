using Reveal.Sdk;
using Reveal.Sdk.Data;
using Reveal.Sdk.Data.Microsoft.SqlServer;

namespace RevealSdk.Server.Reveal
{
    // ****
    // https://help.revealbi.io/web/authentication/ 
    // The Authentication Provider is required to set the credentials used
    // in the DataSourceProvider changeDataSourceAsync to authenticate to your database
    // ****


    // ****
    // NOTE:  This must beset in the Builder in Program.cs --> .AddAuthenticationProvider<AuthenticationProvider>()
    // ****
    public class AuthenticationProvider : IRVAuthenticationProvider
    {
        public Task<IRVDataSourceCredential> ResolveCredentialsAsync(IRVUserContext userContext,
            RVDashboardDataSource dataSource)
        {
            // Create a userCredential object
            IRVDataSourceCredential? userCredential = null;

            // Check that the incoming request is for the expected data source type
            // You can check the data source type, or any of the information in your UserContext to
            // set credentials
            if (dataSource is RVSqlServerDataSource)
            {
                // for SQL Server, add a username, password and optional domain
                // note these are just properties, you can set them from configuration, a key vault, a look up to 
                // database, etc.  They are hardcoded here for demo purposes.
                userCredential = new RVUsernamePasswordDataSourceCredential("username", "password");
            }
            return Task.FromResult(userCredential);
        }
    }
}
