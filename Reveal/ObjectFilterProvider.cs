// The OPTIONAL `ObjectFilterProvider`class implements the `IRVObjectFilter` interface  
// to filter which data sources and data source items (such as tables, views, or 
// stored procedures) are accessible to users within the Reveal BI environment. 
// It allows you to control the display and access of these database objects 
// based on user-specific criteria, such as user roles.
//
// Purpose:
// The `ObjectFilterProvider` serves as an optional filter mechanism, letting you 
// restrict access to certain databases or specific database objects in Reveal BI. 
// This is useful for enforcing security and data access policies, especially in 
// multi-tenant applications or scenarios with role-based data access.
//
// Provider Setup:
// This provider must be registered in the DI container in `Program.cs` by calling 
// `.AddObjectFilter<ObjectFilterProvider>()`. If not registered, Reveal will not 
// apply any custom filtering logic to data source or data source items.
//
// Key Methods:
// - `Filter(IRVUserContext userContext, RVDashboardDataSource dataSource)`:
//   This method checks if a specific data source is allowed for the user based on 
//   a predefined list of allowed databases. It can leverage `userContext` to restrict 
//   data access by user or tenant, ensuring only authorized data sources are accessible.
//
// - `Filter(IRVUserContext userContext, RVDataSourceItem dataSourceItem)`:
//   This method filters individual database objects, such as tables and stored procedures, 
//   based on user roles. In this example, users with a "User" role are restricted to 
//   viewing only the `All Orders` and `All Invoices` tables, while users with an 
//   "Admin" role have unrestricted access. Role information is retrieved from `userContext`.
//
// Configuration and Security:
// - `_config`: An instance of `IConfiguration`, injected via constructor dependency injection. 
//   It provides access to app settings and secrets, which can include a list of authorized databases 
//   or other sensitive information. 
// - `allowedList`: A predefined list of databases that are permitted for access, populated from 
//   configuration settings.
// - `allowedItems`: A hardcoded list of allowed database items, restricting access based on the 
//   user role. In production, consider dynamically retrieving this list from a configuration file 
//   or database.
//
// Usage Notes:
// - Both `Filter` methods are optional and can be omitted if unrestricted access to all databases 
//   and database items is acceptable.
// - You may adapt these methods to implement more complex filtering rules, such as time-based 
//   access restrictions or department-based filtering.
//
// Reference Links:
// - Reveal BI User Context and Object Filter Documentation: https://help.revealbi.io/web/user-context/#using-the-user-context-in-the-objectfilterprovider
// - App Secrets Management in .NET: https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-8.0&tabs=windows

using Reveal.Sdk;
using Reveal.Sdk.Data;
using Reveal.Sdk.Data.Microsoft.SqlServer;

namespace RevealSdk.Server.Reveal
{
    public class ObjectFilterProvider : IRVObjectFilter
    {
        // ***
        // For AppSettings / Secrets retrieval
        // https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-8.0&tabs=windows
        // ***
        private readonly IConfiguration _config;

        // Constructor that accepts IConfiguration as a dependency
        public ObjectFilterProvider(IConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }
        // ***

        public Task<bool> Filter(IRVUserContext userContext, RVDashboardDataSource dataSource) // this is a filter that goes through all databases on the server
        {
            // ****
            //
            // The Filter is Optional, you do not need to include this in your implementation.
            // This method is meant to ensure there is no rogue request for a database that is not allowed.
            //
            // For example, you can use the UserContext to set a property for a user or tenant, and then check this
            // property in the Filter to ensure that the user is only accessing the data they are allowed to access.
            //
            // ****
            var allowedList = new List<string>() { _config["""SqlServer:Database"""] }; //here we indicate a list of databases with which we want to work

            if (dataSource != null)
            {
                if (dataSource is RVSqlServerDataSource dataSQL) // we consult if it is a SQL DB and cast the generic data source to SQL to be able to access its attributes
                {
                    if (allowedList.Contains(dataSQL.Database)) return Task.FromResult(true);
                }
            }
            return Task.FromResult(false);
        }

        public Task<bool> Filter(IRVUserContext userContext, RVDataSourceItem dataSourceItem)
        {
            // ****
            // The Filter is Optional, you do not need to include this in your implementation.
            // This method is meant to ensure there is no rogue request for a database objects (Tables, Functions,
            // Stored Procedures) that is not allowed.
            //
            // In the example I am using the Roles that are set up in the UserContext Provider.  I use this property
            // to check the Role of the current users to restrict what is displayed in the Data Sources.
            // If the logged in user is an Admin role, they see all the Tables, Views, Sprocs, if not, 
            // they will only see the 'All Orders' and 'Invoices' tables.
            // ****
            if (userContext?.Properties != null && dataSourceItem is RVSqlServerDataSourceItem dataSQLItem)
            {
                if (userContext.Properties.TryGetValue("Role", out var roleObj) &&
                    roleObj?.ToString()?.ToLower() == "user")
                {
                    // ****
                    // Hardcoding these, however, you can pull these in from JSON, configuration or a database
                    // ****
                    var allowedItems = new HashSet<string> { "All Orders", "All Invoices"};

                    if ((dataSQLItem.Table != null && !allowedItems.Contains(dataSQLItem.Table)) ||
                        (dataSQLItem.Procedure != null && !allowedItems.Contains(dataSQLItem.Procedure)))
                    {
                        return Task.FromResult(false);
                    }
                }
            }
            return Task.FromResult(true);
        }
    }
}
