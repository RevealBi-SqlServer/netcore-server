// The `DataSourceProvider` class implements the `IRVDataSourceProvider` interface 
// to configure and validate data source connections and custom queries for Reveal BI 
// based on user context. It provides essential setup for database connections, 
// custom query construction, and security validation, specifically for SQL Server 
// data sources in this example.
//
// Purpose:
// The main purpose of `DataSourceProvider` is to dynamically set connection properties 
// (like host and database) and customize SQL queries or stored procedures based on 
// incoming requests, such as table requests from the client side. This is essential 
// for scenarios that involve dynamic data sources, multi-tenancy, or secure, 
// role-based access to data.
//
// Provider Setup:
// This provider must be registered in the DI container in `Program.cs` with the 
// following call: `.AddDataSourceProvider<DataSourceProvider>()`. Without this 
// registration, Reveal BI will not use this custom data source configuration.
//
// Key Methods:
// - `ChangeDataSourceAsync(IRVUserContext userContext, RVDashboardDataSource dataSource)`:
//   This asynchronous method configures data source connection properties (e.g., 
//   setting the SQL Server host and database) based on user context and data source type.
//
// - `ChangeDataSourceItemAsync(IRVUserContext userContext, string dashboardId, RVDataSourceItem dataSourceItem)`:
//   This method sets or modifies SQL queries based on the data source item requested 
//   by the client. It includes support for specific procedures (e.g., `CustOrderHist`), 
//   ad-hoc queries (e.g., `CustomerOrders`), and role-based access. It validates 
//   parameters and query format to prevent SQL injection and unauthorized access.
//   - `allowedTables`: A list of table names allowed for access based on the user’s role 
//     or requested data, restricted to those containing `CustomerID`.
//   - `isAdmin`: Allows unrestricted access for users with an "Admin" role.
//
// Validation and Security Helpers:
// - These are 100% custom to this example, they are not part of the Reveal SDK
// - `IsValidCustomerId` and `IsValidOrderId`: Regular expressions validate that customer 
//   and order IDs are well-formed to prevent SQL injection attacks and enforce 
//   proper data formats.
// - `EscapeSqlInput`: Sanitizes SQL inputs by escaping single quotes in dynamic queries 
//   to prevent SQL injection.
// - `IsSelectOnly`: A helper function that parses SQL using `TSql150Parser` and 
//   `ReadOnlySelectVisitor` to ensure only read-only `SELECT` statements are allowed, 
//   helping prevent malicious SQL statements in custom queries.
//
// Usage Notes:
// - Sensitive data, such as credentials and server details, should be retrieved from 
//   secure configurations (e.g., `IConfiguration` and app secrets). In production, 
//   queries should be strictly validated to avoid any security vulnerabilities.
//
// Reference Links:
// - Reveal BI Data Sources Documentation: https://help.revealbi.io/web/datasources/
// - Reveal BI MS SQL Server Data Source Documentation: https://help.revealbi.io/web/adding-data-sources/ms-sql-server/
// - App Secrets Management in .NET: https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-8.0&tabs=windows


using Microsoft.SqlServer.TransactSql.ScriptDom;
using Reveal.Sdk;
using Reveal.Sdk.Data;
using Reveal.Sdk.Data.Microsoft.SqlServer;
using System.Text.RegularExpressions;

namespace RevealSdk.Server.Reveal
{
    internal class DataSourceProvider : IRVDataSourceProvider
    {

        // ***
        // For AppSettings / Secrets retrieval
        // https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-8.0&tabs=windows
        // ***
        private readonly IConfiguration _config;

        // Constructor that accepts IConfiguration as a dependency
        public DataSourceProvider(IConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }
        // ***


        public Task<RVDashboardDataSource> ChangeDataSourceAsync(IRVUserContext userContext, RVDashboardDataSource dataSource)
        {
            // *****
            // Check the request for the incoming data source
            // In a multi-tenant environment, you can use the user context properties to determine who is logged in
            // and what their connection information should be
            // you can also check the incoming dataSource type or id to set connection properties
            // *****

            if (dataSource is RVSqlServerDataSource SqlDs)
            {
                SqlDs.Host = _config["SqlServer:Host"];
                SqlDs.Database = _config["SqlServer:Database"];
            }
            return Task.FromResult(dataSource);
        }

        public Task<RVDataSourceItem>? ChangeDataSourceItemAsync(IRVUserContext userContext, string dashboardId, RVDataSourceItem dataSourceItem)
        {
            // ****
            // Every request for data passes thru changeDataSourceItem
            // You can set query properties based on the incoming requests
            // for example, you can check:
            // - dsi.id
            // - dsi.table
            // - dsi.procedure
            // - dsi.title
            // and take a specific action on the dsi as this request is processed
            // ****

            if (dataSourceItem is not RVSqlServerDataSourceItem sqlDsi) return Task.FromResult(dataSourceItem);

            // Ensure data source is updated
            ChangeDataSourceAsync(userContext, sqlDsi.DataSource);

            string customerId = userContext.UserId;
            string orderId = userContext.Properties["OrderId"]?.ToString();
            bool isAdmin = userContext.Properties["Role"]?.ToString() == "Admin";

            var allowedTables = TableInfo.GetAllowedTables()
                             .Where(t => string.Equals(t.COLUMN_NAME, "CustomerID", StringComparison.OrdinalIgnoreCase))
                             .Select(t => t.TABLE_NAME)
                             .ToList();

            switch (sqlDsi.Id)
            {
                // *****
                // Example of how to use a stored procedure with a parameter
                // *****
                case "CustOrderHist":
                case "CustOrdersOrders":
                    if (!IsValidCustomerId(customerId))
                        throw new ArgumentException("Invalid CustomerID format. CustomerID must be a 5-character alphanumeric string.");
                    sqlDsi.Procedure = sqlDsi.Id;
                    sqlDsi.ProcedureParameters = new Dictionary<string, object> { { "@CustomerID", customerId } };
                    break;

                // *****
                // Example of how to use a stored procedure 
                // *****
                case "TenMostExpensiveProducts":
                    sqlDsi.Procedure = "Ten Most Expensive Products";
                    break;

                // *****
                // Example of an ad-hoc-query
                // *****
                case "CustomerOrders":
                    if (!IsValidOrderId(orderId))
                        throw new ArgumentException("Invalid OrderId format. OrderId must be a 5-digit numeric value.");

                    orderId = EscapeSqlInput(orderId);
                    string customQuery = $"SELECT * FROM Orders WHERE OrderId = '{orderId}'";
                    if (!IsSelectOnly(customQuery)) 
                        throw new ArgumentException("Invalid SQL query.");
                    sqlDsi.CustomQuery = customQuery;

                    break;

                    // *****
                    // Example pulling in the list of allowed tables that have the customerId column name
                    // this ensures that _any_ time a request is made for customer specific data in allowed tables
                    // the customerId parameter is passed
                    // note that the Admin role is not restricted to a custom query, the Admin role will see all 
                    // customer data with no restriction
                    // the tables being checked are in the allowedtables.json
                    // *****
                 case var table when allowedTables.Contains(sqlDsi.Table):
                    if (isAdmin && dashboardId != "Customer Orders")
                        break;

                    if (!IsValidCustomerId(customerId))
                        throw new ArgumentException("Invalid CustomerID format. CustomerID must be a 5-character alphanumeric string.");

                    customerId = EscapeSqlInput(customerId);
                    string query = $"SELECT * FROM [{sqlDsi.Table}] WHERE customerId = '{customerId}'";
                    if (!IsSelectOnly(query))
                        throw new ArgumentException("Invalid SQL query.");

                    sqlDsi.CustomQuery = query;
                    break;

                default:
                    // ****
                    // If you do not want to allow any other tables,throw an exception
                    // ****
                    //throw new ArgumentException("Invalid Table");
                    //return null;
                    break;
            }

            return Task.FromResult(dataSourceItem);
        }

        // ****
        // Modify any of the code below to meet your specific needs
        // The code below is not part of the Reveal SDK, these are helpers to clean / validate parameters
        // specific to this sample code.  For example, ensuring the customerId & orderId are well formed, 
        // and ensuring that no invalid / illegal statements are passed in the header to the custom query
        // ****

        private static bool IsValidCustomerId(string customerId) => Regex.IsMatch(customerId, @"^[A-Za-z0-9]{5}$");
        private static bool IsValidOrderId(string orderId) => Regex.IsMatch(orderId, @"^\d{5}$");
        private string EscapeSqlInput(string input) => input.Replace("'", "''");

        public bool IsSelectOnly(string sql)
        {
            TSql150Parser parser = new TSql150Parser(true);
            IList<ParseError> errors;
            TSqlFragment fragment;

            using (TextReader reader = new StringReader(sql))
            {
                fragment = parser.Parse(reader, out errors);
            }

            if (errors.Count > 0)
            {
                foreach (var error in errors)
                {
                    Console.WriteLine($"Error: {error.Message}");
                }
                return false;
            }

            var visitor = new ReadOnlySelectVisitor();
            fragment.Accept(visitor);
            return visitor.IsReadOnly;
        }
    }
}