using Reveal.Sdk;
using Reveal.Sdk.Data;
using Reveal.Sdk.Data.Microsoft.SqlServer;

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
        public Task<RVDataSourceItem>? ChangeDataSourceItemAsync(IRVUserContext userContext, 
                string dashboardId, RVDataSourceItem dataSourceItem)
        {

            // ****
            // Every request for data passes thru changeDataSourceItem
            // You can set query properties based on the incoming requests
            // ****

            if (dataSourceItem is RVSqlServerDataSourceItem sqlDsi)
            {
                // Ensure data source is updated
                ChangeDataSourceAsync(userContext, sqlDsi.DataSource);


                // *****
                // Example of how to use a stored procedure with a paramter
                // *****
                if (sqlDsi.Id == "CustOrderHist")
                {
                    sqlDsi.Procedure = "CustOrderHist";
                    sqlDsi.ProcedureParameters = new Dictionary<string, object> { { "@CustomerID", userContext.UserId } };
                }

                // *****
                // Example of how to use a stored procedure with a parameter
                // *****
                else if (sqlDsi.Id == "CustOrdersOrders")
                {
                    sqlDsi.Procedure = "CustOrdersOrders";
                    sqlDsi.ProcedureParameters = new Dictionary<string, object> { { "@CustomerID", userContext.UserId } };
                }

                // *****
                // Example of how to use a stored procedure with no parameter
                // *****
                else if (sqlDsi.Id == "TenMostExpensiveProducts")
                {
                    sqlDsi.Procedure = "Ten Most Expensive Products";
                }

                // *****
                // Example of how to use an ad-hoc query with a parameter
                // *****
                else if (sqlDsi.Id == "CustomerOrders")
                {
                    sqlDsi.CustomQuery = "Select * from Orders Where OrderId = " + userContext.Properties["OrderId"];
                }

                // *****
                // Example of how check a request for a table / view and add a paramterized query
                // *****
                else if (sqlDsi.Table == "OrdersQry")
                {
                    sqlDsi.CustomQuery = "Select * from OrdersQry where customerId = '" + userContext.UserId + "'";
                }

                //else return null;
            }
            return Task.FromResult(dataSourceItem);
        }

        public Task<RVDashboardDataSource> ChangeDataSourceAsync(IRVUserContext userContext, RVDashboardDataSource dataSource)
        {
            // *****
            // Check the request for the incoming data source
            // In a multi-tenant environment, you can use the user context properties to determine who is logged in
            // and what their connection information should be
            // you can also check the incoming dataSource type or id to set connection properties
            // *****

            if (dataSource is RVSqlServerDataSource sqlServerDataSourceItem)
            {

                sqlServerDataSourceItem.Host = "serverName";
                sqlServerDataSourceItem.Database = "databaseName";
            }
            return Task.FromResult(dataSource);
        }
    }
}