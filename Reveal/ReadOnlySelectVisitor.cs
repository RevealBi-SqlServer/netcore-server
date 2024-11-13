
// The `ReadOnlySelectVisitor` is a custom visitor that inherits from `TSqlFragmentVisitor` 
// in the Microsoft.SqlServer.TransactSql.ScriptDom namespace. It is used to determine if a given 
// SQL fragment (parsed SQL structure) contains only `SELECT` statements, indicating a read-only operation.
//
// ** Note this is optional and not part of the Reveal SDK.  It is used in this sample to show how to
// ** determine if a SQL statement is read-only as an additional check on an ad-hoc .CustomQuery in
// ** the ChangeDataSourceItem method in the DashboardProvider class.
//
// Purpose:
// The main purpose of `ReadOnlySelectVisitor` is to inspect SQL statements and determine if they 
// are read-only. Specifically, it traverses the SQL AST (Abstract Syntax Tree) and sets the `IsReadOnly` 
// property to `false` if it encounters any statement other than a `SELECT` statement.
//
// Key Properties and Methods:
// - `IsReadOnly`: A boolean property that starts as `true`, meaning the SQL is assumed to be read-only. 
//   If any non-`SELECT` statements are found, this property is set to `false`.
// - `Visit(TSqlStatement node)`: An overridden method from `TSqlFragmentVisitor` that is called for each 
//   `TSqlStatement` in the SQL fragment. If the statement is not a `SELECT` statement, `IsReadOnly` is 
//   set to `false`, indicating that the SQL contains write operations or modifications.
//
// Usage Context:
// This class can be used in scenarios where it is necessary to ensure SQL statements are read-only 
// (e.g., for security purposes or to enforce database access policies) by confirming only `SELECT` 
// statements are present in the SQL fragment being analyzed.


using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace RevealSdk.Server.Reveal
{
    class ReadOnlySelectVisitor : TSqlFragmentVisitor
    {
        public bool IsReadOnly { get; private set; } = true;

        public override void Visit(TSqlStatement node)
        {
            if (!(node is SelectStatement))
            {
                IsReadOnly = false;
            }
            base.Visit(node);
        }
    }
}
