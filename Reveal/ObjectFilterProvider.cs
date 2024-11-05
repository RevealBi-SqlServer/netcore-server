using Reveal.Sdk;
using Reveal.Sdk.Data;
using Reveal.Sdk.Data.Microsoft.SqlServer;

namespace RevealSdk.Server.Reveal
{
    // ****
    // https://help.revealbi.io/web/user-context/#using-the-user-context-in-the-objectfilterprovider
    // ObjectFilter Provider is optional.
    // The Filter functions allow you to control the data sources dialog  on the client.
    // ****


    // ****
    // NOTE:  This is ignored of it is not set in the Builder in Program.cs --> //.AddObjectFilter<ObjectFilterProvider>()
    // ****
    public class ObjectFilterProvider : IRVObjectFilter
    {
        public Task<bool> Filter(IRVUserContext userContext, RVDashboardDataSource dataSource)
        {
            throw new NotImplementedException();
        }

        public Task<bool> Filter(IRVUserContext userContext, RVDataSourceItem dataSourceItem)
        {
            // ****
            // In the scenario I am using the Roles that are set up in the UserContext Provider to check the 
            // Role property to restrict what is displayed in the Data Sources.
            // If the logged in user is an Admin role, they see all the Tables, Views, Sprocs, if not, 
            // they will only see the 'All Orders' and 'Invoices' tables.
            // ****
            if (userContext?.Properties != null && dataSourceItem is RVSqlServerDataSourceItem dataSQLItem)
            {
                if (userContext.Properties.TryGetValue("Role", out var roleObj) &&
                    roleObj?.ToString()?.ToLower() == "user")
                {
                    var allowedItems = new HashSet<string> { "All Orders", "Invoices" };

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
