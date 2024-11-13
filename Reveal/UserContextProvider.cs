// The OPTIONAL `UserContextProvider` class implements the `IRVUserContextProvider` interface to supply 
// a custom `UserContext` in the Reveal BI environment. This class retrieves HTTP headers from 
// incoming requests to create a user context with relevant user information, such as user ID, 
// order ID, and role. This custom `UserContext` can be used by Reveal BI providers to enforce 
// role-based access control and context-specific data handling for each user session.
//
// Purpose:
// The primary purpose of `UserContextProvider` is to retrieve user-specific details from 
// HTTP headers and define a custom context (`RVUserContext`) that contains the authenticated 
// user’s identity and associated properties. This context is then used by Reveal BI providers 
// (such as `IRVDashboardProvider`, `IRVAuthenticationProvider`, and `IRVDataSourceProvider`) 
// to apply data access restrictions or provide context-based features like query parameters for
// custom queries or stored procedures.
//
// Provider Setup:
// This provider must be registered in the DI container in `Program.cs` with the following call: 
// `.AddUserContextProvider<UserContextProvider>()`. If not registered, Reveal will not use this 
// custom `UserContext` for server requests.
//
// Key Components:
// - `GetUserContext(HttpContext aspnetContext)`:
//   This method extracts custom headers from the HTTP request to determine user-specific 
//   values (`userId`, `orderId`) and validates the `userId` format. It also assigns a role 
//   based on the `userId`, setting a default role of "User" and promoting certain IDs to "Admin". 
//   These values are packaged into a dictionary of properties and returned in an `RVUserContext` 
//   object, allowing Reveal to use this context in data access control and custom queries.
//
// - `IsValidCustomerId(string customerId)`:
//   A helper method to validate the `customerId` format using a regular expression, ensuring 
//   the ID is a 5-character alphanumeric string.
//
// Usage Notes:
// - The `userId` is assigned a role (e.g., "Admin" or "User") and stored as part of the context. 
//   In production, consider dynamically loading roles or permissions based on an authentication 
//   system.
// - Example headers (`x-header-one`, `x-header-two`) are used to illustrate how custom values 
//   can be sent from the client and leveraged within Reveal BI for context-sensitive data handling.
//
// Reference Links:
// - Reveal BI User Context Documentation: https://help.revealbi.io/web/user-context/
// - App Secrets Management in .NET: https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-8.0&tabs=windows


using Reveal.Sdk;
using System.Text.RegularExpressions;

namespace RevealSdk.Server.Reveal
{
    public class UserContextProvider : IRVUserContextProvider
    {
        IRVUserContext IRVUserContextProvider.GetUserContext(HttpContext aspnetContext)
        {

            // ****
            // In this case, there are 3 headers sent in clear text to the server
            // Normally, you'd be accepting your token or other secrets that you'd use 
            // for the security context of your data requests,
            // or you would be passing query parameters for custom queries, etc.
            // Thsi configuration is 100% your own custom implementation, below is just an example.
            // ****

            var userId = aspnetContext.Request.Headers["x-header-one"];
            var orderId = aspnetContext.Request.Headers["x-header-two"];

            if (!IsValidCustomerId(userId))
                throw new ArgumentException("Invalid CustomerID format. CustomerID must be a 5-character alphanumeric string.");


            // ****
            // Set up Roles based on the incoming user id.  In a real app, this would be set up to match
            // your scenario and be dynamically loaded
            // ****
            string role = "User";
            if (userId == "AROUT" || userId == "BLONP")
            {
                role = "Admin";
            }

            // ****
            // Create an array of properties that can be used in other Reveal functions
            // ****
            var props = new Dictionary<string, object>() {
                    { "OrderId", orderId },
                    { "Role", role } };

            Console.WriteLine("UserContextProvider: " + userId + " " + orderId);

            return new RVUserContext(userId, props);
        }

        private static bool IsValidCustomerId(string customerId) => Regex.IsMatch(customerId, @"^[A-Za-z0-9]{5}$");
    }
}