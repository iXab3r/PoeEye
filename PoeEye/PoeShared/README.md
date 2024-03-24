# Problem with System.Data.SqlClient in Net8

LinqKit depends on EntityFramework, which on itself depends on System.Data.SqlClient
https://github.com/dotnet/SqlClient/issues/1930

The best way to look into dependencies is, unfortunately, to look @ *.deps.json as the tools are kinda not helpful,
even NDepend