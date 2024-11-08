using Reveal.Sdk;
using Reveal.Sdk.Data;
using Reveal.Sdk.Data.Microsoft.SqlServer;
using System.Text.RegularExpressions;

namespace RevealSdk.Server.Reveal
{
    // ****
    // https://help.revealbi.io/web/datasources/
    // https://help.revealbi.io/web/adding-data-sources/ms-sql-server/        
    // The DataSource Provider is required.  
    // Set you connection details in the ChangeDataSource, like Host & Database.  
    // If you are using data source items on the client, or you need to set specific queries based 
    // on incoming table requests, you will handle those requests in the ChangeDataSourceItem.
    // ****


    // ****
    // NOTE:  This must beset in the Builder in Program.cs --> .AddDataSourceProvider<DataSourceProvider>()
    // ****
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
            // ****

            if (dataSourceItem is not RVSqlServerDataSourceItem sqlDsi) return Task.FromResult(dataSourceItem);

            // Ensure data source is updated
            ChangeDataSourceAsync(userContext, sqlDsi.DataSource);

            string customerId = userContext.UserId;

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
                    string orderId = userContext.Properties["OrderId"]?.ToString();
                    if (!IsValidOrderId(orderId))
                        throw new ArgumentException("Invalid OrderId format. OrderId must be a 5-digit numeric value.");
                    
                    string customQuery = $"SELECT * FROM Orders WHERE OrderId = {orderId}";
                    if (!IsValidSql(customQuery))
                        throw new ArgumentException("Invalid SQL query.");
                    sqlDsi.CustomQuery = customQuery;
                    break;

                case var table when table == "OrdersQry":
                    if (!IsValidCustomerId(customerId))
                        throw new ArgumentException("Invalid CustomerID format. CustomerID must be a 5-character alphanumeric string.");
                    
                    string query = $"SELECT * FROM OrdersQry WHERE customerId = '{customerId}'";
                    if (!IsValidSql(query))
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

        private static readonly string[] AllowedKeywords = new[]
        {
            "SELECT", "WHERE", "JOIN", "ORDER", "BY", "GROUP", "HAVING",
            "LIMIT", "OFFSET", "FETCH", "DISTINCT", "IN", "LIKE", "BETWEEN"
        };

        private static bool IsValidSql(string query)
        {
            var words = query.Split(new[] { ' ', '\t', '\n', '\r', ',', ';', '(', ')', '*' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in words)
            {
                if (!AllowedKeywords.Contains(word.ToUpper()) && !IsSqlIdentifier(word) && !IsSqlOperator(word) && !IsSqlLiteral(word))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool IsSqlIdentifier(string word)
        {
            // Add logic to determine if a word is a valid SQL identifier (e.g., table name, column name)
            // For simplicity, we assume that valid identifiers are alphanumeric and start with a letter
            return Regex.IsMatch(word, @"^[A-Za-z][A-Za-z0-9_]*$");
        }

        private static bool IsSqlOperator(string word)
        {
            // Add common SQL operators that are allowed
            var operators = new[] { "=", "<", ">", "<=", ">=", "<>", "!=", "AND", "OR", "NOT", "LIKE" };
            return operators.Contains(word.ToUpper());
        }

        private static bool IsSqlLiteral(string word)
        {
            // Check if the word is a numeric literal
            return int.TryParse(word, out _);
        }

    }
}