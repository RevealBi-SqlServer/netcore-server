// The OPTIONAL `DashboardProvider` class implements the `IRVDashboardProvider` interface 
// to customize where Reveal BI loads and saves dashboards.
//
// By default, Reveal saves and loads dashboards from the `/dashboards` folder in
// the application's directory, but this provider enables flexibility to
// load and save dashboards from a custom location, such as a specific folder or a database.
//
// Purpose:
// The primary function of `DashboardProvider` is to customize 
// where dashboards are saved and loaded. It uses the `userContext` information to 
// allow conditional loading and saving, based on specific users or other contextual 
// properties. For example, it might load dashboards from a user-specific folder or 
// store dashboards in a different repository, like a database.
//
// Provider Setup:
// To use this custom dashboard provider, it must be registered in the DI container 
// in `Program.cs` by calling `.AddDashboardProvider<DashboardProvider>()`. If not 
// registered, Reveal defaults to loading and saving dashboards from the standard 
// `/dashboards` folder.
//
// Key Methods:
// - `GetDashboardAsync(IRVUserContext userContext, string dashboardId)`:
//   This asynchronous method retrieves a dashboard by ID from a specified folder, 
//   here set to `MyDashboards` under the current working directory. The `userContext` 
//   parameter can be used to conditionally load dashboards based on the requesting user.
//   The method constructs the file path for the requested dashboard ID and loads it from 
//   `MyDashboards`.
//
// - `SaveDashboardAsync(IRVUserContext userContext, string dashboardId, Dashboard dashboard)`:
//   This asynchronous method saves the specified `dashboard` to a file path under the 
//   `MyDashboards` folder with the given dashboard ID. The `userContext` parameter 
//   can be used to customize the save location based on the user's context. The `SaveToFileAsync` 
//   method of `Dashboard` is used to persist the dashboard to the specified path.
//
// Usage Notes:
// - This example code saves and loads dashboards only from the `MyDashboards` folder. 
//   *** In this sample, the DashboardProvider is not registered in the `Program.cs` file, so it is not used
//   In production, this could be extended to dynamically determine save locations 
//   or integrate with a database for centralized dashboard storage.
//
// Reference Link:
// - Reveal BI Dashboard Provider Documentation: https://help.revealbi.io/web/saving-dashboards/#example-implementing-save-with-irvdashboardprovider

using Reveal.Sdk;

namespace RevealSdk.Server.Reveal
{
    public class DashboardProvider : IRVDashboardProvider
    {
        public Task<Dashboard> GetDashboardAsync(IRVUserContext userContext, string dashboardId)
        {
            // Only load dashboards from the MyDashboards folder
            var filePath = Path.Combine(Environment.CurrentDirectory, $"MyDashboards/{dashboardId}.rdash");
            var dashboard = new Dashboard(filePath);
            return Task.FromResult(dashboard);
        }

        public async Task SaveDashboardAsync(IRVUserContext userContext, string dashboardId, Dashboard dashboard)
        {
            // Only save dashboards to the MyDashboards folder.
            var filePath = Path.Combine(Environment.CurrentDirectory, $"MyDashboards/{dashboardId}.rdash");
            await dashboard.SaveToFileAsync(filePath);
        }
    }
}
