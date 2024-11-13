// The `TableInfo` class represents metadata about a database table and its columns. 
// This class includes properties for the table schema, table name, and column name, 
// and provides functionality to retrieve a list of allowed tables based on JSON 
// configuration.
//
// Purpose:
// The main purpose of `TableInfo` is to store information about database tables 
// and allow retrieval of a list of permitted tables for use in data access control. 
// This is useful for security and authorization scenarios where only certain tables 
// and columns are permitted for a given user or role in the Reveal BI environment.
//
// Key Properties:
// - `TABLE_SCHEMA`: The schema of the table (e.g., "dbo" or "sales").
// - `TABLE_NAME`: The name of the table (e.g., "Customers" or "Orders").
// - `COLUMN_NAME`: The specific column name in the table, allowing finer-grained 
//   control over data access if necessary.
//
// Key Method:
// - `GetAllowedTables()`:
//   This static method reads a JSON file (`allowedTables.json`) that contains an 
//   array of `TableInfo` objects, deserializes it into a list of `TableInfo` instances, 
//   and returns this list. This allows the application to define allowed tables and columns 
//   in a JSON file, which can be updated without changing the code.
//
// JSON Configuration:
// - `allowedTables.json`: This JSON file should be structured to contain an array of 
//   objects with properties `TABLE_SCHEMA`, `TABLE_NAME`, and `COLUMN_NAME`. It serves 
//   as an external configuration to control table and column access for the application.
//
// Usage Notes:
// - This class is often used in conjunction with data source filtering providers, like 
//   `ObjectFilterProvider`, to enforce access restrictions on specific tables and columns.
// - Ensure the `allowedTables.json` file is correctly formatted and accessible at runtime, 
//   as the deserialization process depends on its structure.
//
// Example JSON Structure:
// ```json
// [
//   { "TABLE_SCHEMA": "dbo", "TABLE_NAME": "Customers", "COLUMN_NAME": "CustomerID" },
//   { "TABLE_SCHEMA": "sales", "TABLE_NAME": "Orders", "COLUMN_NAME": "OrderID" }
// ]
// ```
// Usage of `TableInfo` in `DataSourceProvider`:
//
// Within the `ChangeDataSourceItemAsync` method, the `TableInfo.GetAllowedTables()` 
// method is called to retrieve a list of tables that are authorized for access. 
// This allows the application to enforce table-level access control based on a 
// configuration file (`allowedTables.json`) containing permitted tables and columns.
//
// Example Usage:
// - The `allowedTables` list is populated with table names that contain the `CustomerID` 
//   column, indicating that only these tables are permitted for customer-specific data access.
//
// Code snippet:
// ```csharp
// var allowedTables = TableInfo.GetAllowedTables()
//                  .Where(t => string.Equals(t.COLUMN_NAME, "CustomerID", StringComparison.OrdinalIgnoreCase))
//                  .Select(t => t.TABLE_NAME)
//                  .ToList();
// ```
//
// This approach supports role-based access control by restricting non-admin users 
// to specific tables. The `allowedTables` list is used in a `switch` statement to 
// enforce that only the tables listed in `allowedTables.json` can be accessed by 
// non-admin users.
//
// Summary:
// - The `TableInfo` class enables flexible, configuration-driven access control, 
//   ensuring that only authorized tables with sensitive columns are accessible 
//   according to user roles and the current context.

using System.Text.Json;

namespace RevealSdk.Server.Reveal
{
    public class TableInfo
    {
        public string? TABLE_SCHEMA { get; set; }
        public string? TABLE_NAME { get; set; }
        public string? COLUMN_NAME { get; set; }

        public static List<TableInfo> GetAllowedTables()
        {
            var json = File.ReadAllText("allowedTables.json");
            return JsonSerializer.Deserialize<List<TableInfo>>(json);
        }
    }
}
