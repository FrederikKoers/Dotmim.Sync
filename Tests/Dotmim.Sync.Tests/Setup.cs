using Dotmim.Sync.Data;
using Dotmim.Sync.Filter;
using Dotmim.Sync.Tests.Core;
using Dotmim.Sync.Tests.MySql;
using Dotmim.Sync.Tests.SqlServer;
using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.InteropServices;
using System.Text;

namespace Dotmim.Sync.Tests
{
    /// <summary>
    /// Setup class is all you need to setup connection string, tables and client enabled for your provider tests
    /// </summary>
    public class Setup
    {
        /// <summary>
        /// Configure a provider fixture
        /// </summary>
        internal static void OnConfiguring<T>(ProviderFixture<T> providerFixture) where T : CoreProvider
        {

            // App Veyor setup
            if (IsOnAppVeyor)
            {
                SetupSql(providerFixture);
                SetupMySql(providerFixture);
                return;
            }
            var sqlTables = new string[]
            {
               "SalesLT.ProductCategory", "SalesLT.ProductModel", "SalesLT.Product", "Employee", "Customer", "Address", "CustomerAddress", "EmployeeAddress",
               "SalesLT.SalesOrderHeader", "SalesLT.SalesOrderDetail", "dbo.Sql", "Posts", "Tags", "PostTag"
            };


            // 1) Add database name
            providerFixture.AddDatabaseName(ProviderType.Sql, "SqlAdventureWorks");

            // 2) Add tables
            providerFixture.AddTables(ProviderType.Sql, sqlTables);


            // 3) Add filters

            // virtual filter
            var region = new DmColumn<String>("region")
            {
                AllowDBNull = true,
                DbType = DbType.StringFixedLength,
                MaxLength = 12
            };
            FilterClause2 regionSqlFilter = new FilterClause2(region);
            regionSqlFilter.Filter("SalesLT.Product");

            var pEmpId = new DmColumn<Int32>("empId");
            pEmpId.AllowDBNull = true;
            pEmpId.DbType = DbType.Int16;

            FilterClause2 customerFilter = new FilterClause2(pEmpId);
            customerFilter.Filter("Customer")
                          .On("EmployeeId");

            FilterClause2 custAddFilter = new FilterClause2(pEmpId);
            custAddFilter.Filter("CustomerAddress")
                         .Join("Customer")
                         .On("EmployeeId");

            FilterClause2 addressFilter1 = new FilterClause2(pEmpId);
            addressFilter1.Filter("Address")
                         .Join("CustomerAddress")
                         .Join("Customer")
                         .On("EmployeeId");

            FilterClause2 addressFilter2 = new FilterClause2(pEmpId);
            addressFilter2.Filter("Address")
                         .Join("EmployeeAddress")
                         .On("EmployeeId");

            FilterClause2 employeeFilter = new FilterClause2(pEmpId);
            employeeFilter.Filter("Employee").On("EmployeeId");

            FilterClause2 employeeAddressFilter = new FilterClause2(pEmpId);
            employeeAddressFilter.Filter("EmployeeAddress").On("EmployeeId");


            FilterClause2 sohFilter = new FilterClause2(pEmpId);
            sohFilter.Filter("SalesLT.SalesOrderHeader")
                          .Join("Customer")
                          .On("EmployeeId");

            FilterClause2 sodFilter = new FilterClause2(pEmpId);
            // then we add the filter table and its column
            sodFilter.Filter("SalesLT.SalesOrderDetail")
                          .Join("SalesLT.SalesOrderHeader")
                          .Join("Customer")
                          .On("EmployeeId");


            // virtual filter on product
            providerFixture.AddFilter(ProviderType.Sql, regionSqlFilter);
            // employees                           
            providerFixture.AddFilter(ProviderType.Sql, employeeFilter);
            providerFixture.AddFilter(ProviderType.Sql, employeeAddressFilter);
            providerFixture.AddFilter(ProviderType.Sql, addressFilter2);
            // customers                           
            providerFixture.AddFilter(ProviderType.Sql, customerFilter);
            providerFixture.AddFilter(ProviderType.Sql, custAddFilter);
            providerFixture.AddFilter(ProviderType.Sql, addressFilter1);
            // sales orders                        
            providerFixture.AddFilter(ProviderType.Sql, sohFilter);
            providerFixture.AddFilter(ProviderType.Sql, sodFilter);

            // add the parameter for the client
            providerFixture.AddFilterParameter(ProviderType.Sql, new SyncParameter("empId", 1));


            // 3) Add runs
            providerFixture.AddRun((ProviderType.Sql, NetworkType.Tcp),
                    ProviderType.Sql |
                    ProviderType.MySql |
                    ProviderType.Sqlite);

            providerFixture.AddRun((ProviderType.Sql, NetworkType.Http),
                    ProviderType.Sql |
                    ProviderType.MySql |
                    ProviderType.Sqlite);


        }

        /// <summary>
        /// Returns the database server to be used in the untittests - note that this is the connection to appveyor SQL Server 2016 instance!
        /// see: https://www.appveyor.com/docs/services-databases/#mysql
        /// </summary>
        internal static String GetSqlDatabaseConnectionString(string dbName)
        {
            // check if we are running localy on windows or linux
            bool isWindowsRuntime = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            if (IsOnAppVeyor)
                return $@"Server=(local)\SQL2016;Database={dbName};UID=sa;PWD=Password12!";
            else if (isWindowsRuntime)
                return $@"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog={dbName};Integrated Security=true;";
            else
                return $@"Data Source=localhost; Database={dbName}; User=sa; Password=QWE123qwe";
        }

        /// <summary>
        /// Returns the database server to be used in the untittests - note that this is the connection to appveyor MySQL 5.7 x64 instance!
        /// see: https://www.appveyor.com/docs/services-databases/#mysql
        /// </summary>
        internal static string GetMySqlDatabaseConnectionString(string dbName)
        {
            if (IsOnAppVeyor)
                return $@"Server=127.0.0.1; Port=3306; Database={dbName}; Uid=root; Pwd=Password12!";
            else
                return $@"Server=127.0.0.1; Port=3306; Database={dbName}; Uid=root; Pwd=azerty31$;";
        }

        /// <summary>
        /// Gets if the tests are running on AppVeyor
        /// </summary>
        internal static bool IsOnAppVeyor
        {
            get
            {
                // check if we are running on appveyor or not
                string isOnAppVeyor = Environment.GetEnvironmentVariable("APPVEYOR");
                return !String.IsNullOrEmpty(isOnAppVeyor) && isOnAppVeyor.ToLowerInvariant() == "true";
            }
        }

        internal static void SetupSql<T>(ProviderFixture<T> providerFixture) where T : CoreProvider
        {
            var sqlTables = new string[]
            {
                "SalesLT.ProductCategory", "SalesLT.ProductModel", "SalesLT.Product", "Employee", "Customer", "Address", "CustomerAddress", "EmployeeAddress",
                "SalesLT.SalesOrderHeader", "SalesLT.SalesOrderDetail", "dbo.Sql", "Posts", "Tags", "PostTag"
            };


            // 1) Add database name
            providerFixture.AddDatabaseName(ProviderType.Sql, "SqlAdventureWorks");

            // 2) Add tables
            providerFixture.AddTables(ProviderType.Sql, sqlTables);


            // 3) Add filters

            // virtual filter
            var region = new DmColumn<String>("region")
            {
                AllowDBNull = true,
                DbType = DbType.StringFixedLength,
                MaxLength = 12,
                DefaultValue = "EUROPE"
            };
            FilterClause2 regionSqlFilter = new FilterClause2(region);
            regionSqlFilter.Filter("SalesLT.Product");

            var pEmpId = new DmColumn<Int32>("empId");
            pEmpId.AllowDBNull = true;
            pEmpId.DbType = DbType.Int16;

            FilterClause2 customerFilter = new FilterClause2(pEmpId);
            customerFilter.Filter("Customer")
                          .On("EmployeeId");

            FilterClause2 custAddFilter = new FilterClause2(pEmpId);
            custAddFilter.Filter("CustomerAddress")
                         .Join("Customer")
                         .On("EmployeeId");

            FilterClause2 addressFilter1 = new FilterClause2(pEmpId);
            addressFilter1.Filter("Address")
                         .Join("CustomerAddress")
                         .Join("Customer")
                         .On("EmployeeId");

            FilterClause2 addressFilter2 = new FilterClause2(pEmpId);
            addressFilter2.Filter("Address")
                         .Join("EmployeeAddress")
                         .On("EmployeeId");

            FilterClause2 employeeFilter = new FilterClause2(pEmpId);
            employeeFilter.Filter("Employee").On("EmployeeId");

            FilterClause2 employeeAddressFilter = new FilterClause2(pEmpId);
            employeeAddressFilter.Filter("EmployeeAddress").On("EmployeeId");


            FilterClause2 sohFilter = new FilterClause2(pEmpId);
            sohFilter.Filter("SalesLT.SalesOrderHeader")
                          .Join("Customer")
                          .On("EmployeeId");

            FilterClause2 sodFilter = new FilterClause2(pEmpId);
            // then we add the filter table and its column
            sodFilter.Filter("SalesLT.SalesOrderDetail")
                          .Join("SalesLT.SalesOrderHeader")
                          .Join("Customer")
                          .On("EmployeeId");


            // virtual filter on product
            providerFixture.AddFilter(ProviderType.Sql, regionSqlFilter);
            // employees                           
            providerFixture.AddFilter(ProviderType.Sql, employeeFilter);
            providerFixture.AddFilter(ProviderType.Sql, employeeAddressFilter);
            providerFixture.AddFilter(ProviderType.Sql, addressFilter2);
            // customers                           
            providerFixture.AddFilter(ProviderType.Sql, customerFilter);
            providerFixture.AddFilter(ProviderType.Sql, custAddFilter);
            providerFixture.AddFilter(ProviderType.Sql, addressFilter1);
            // sales orders                        
            providerFixture.AddFilter(ProviderType.Sql, sohFilter);
            providerFixture.AddFilter(ProviderType.Sql, sodFilter);

            // add the parameter for the client
            providerFixture.AddFilterParameter(ProviderType.Sql, new SyncParameter("empId", 1));


            // 3) Add runs
            providerFixture.AddRun((ProviderType.Sql, NetworkType.Tcp),
                    ProviderType.Sql |
                    ProviderType.MySql |
                    ProviderType.Sqlite);

            providerFixture.AddRun((ProviderType.Sql, NetworkType.Http),
                    ProviderType.Sql |
                    ProviderType.MySql |
                    ProviderType.Sqlite);


        }


        internal static void SetupMySql<T>(ProviderFixture<T> providerFixture) where T : CoreProvider
        {

            var mySqlTables = new string[]
            {
                "productcategory", "productmodel", "product", "employee", "customer", "address","customeraddress", "employeeaddress",
                "salesorderheader", "salesorderdetail", "sql", "posts", "tags", "posttag"
            };

            // 1) Add database name
            providerFixture.AddDatabaseName(ProviderType.MySql, "mysqladventureworks");

            // 2) Add tables
            providerFixture.AddTables(ProviderType.MySql, mySqlTables);


            // 3) Add filters

            // virtual filter
            var region = new DmColumn<String>("region")
            {
                AllowDBNull = true,
                DbType = DbType.StringFixedLength,
                MaxLength = 12,
                DefaultValue = "EUROPE"
            };
            FilterClause2 regionMySqlFilter = new FilterClause2(region);
            regionMySqlFilter.Filter("product");


            var pEmpId = new DmColumn<Int32>("empId");
            pEmpId.AllowDBNull = true;
            pEmpId.DbType = DbType.Int16;

            FilterClause2 customerFilter = new FilterClause2(pEmpId);
            customerFilter.Filter("Customer")
                          .On("EmployeeId");

            FilterClause2 custAddFilter = new FilterClause2(pEmpId);
            custAddFilter.Filter("CustomerAddress")
                         .Join("Customer")
                         .On("EmployeeId");

            FilterClause2 addressFilter1 = new FilterClause2(pEmpId);
            addressFilter1.Filter("Address")
                         .Join("CustomerAddress")
                         .Join("Customer")
                         .On("EmployeeId");

            FilterClause2 addressFilter2 = new FilterClause2(pEmpId);
            addressFilter2.Filter("Address")
                         .Join("EmployeeAddress")
                         .On("EmployeeId");

            FilterClause2 employeeFilter = new FilterClause2(pEmpId);
            employeeFilter.Filter("Employee").On("EmployeeId");

            FilterClause2 employeeAddressFilter = new FilterClause2(pEmpId);
            employeeAddressFilter.Filter("EmployeeAddress").On("EmployeeId");


            FilterClause2 sohFilter = new FilterClause2(pEmpId);
            sohFilter.Filter("SalesOrderHeader")
                          .Join("Customer")
                          .On("EmployeeId");

            FilterClause2 sodFilter = new FilterClause2(pEmpId);
            sodFilter.Filter("SalesOrderDetail")
                          .Join("SalesOrderHeader")
                          .Join("Customer")
                          .On("EmployeeId");


            // virtual filter on product
            providerFixture.AddFilter(ProviderType.MySql, regionMySqlFilter);
            // employees                           
            providerFixture.AddFilter(ProviderType.MySql, employeeFilter);
            providerFixture.AddFilter(ProviderType.MySql, employeeAddressFilter);
            providerFixture.AddFilter(ProviderType.MySql, addressFilter2);
            // customers                           
            providerFixture.AddFilter(ProviderType.MySql, customerFilter);
            providerFixture.AddFilter(ProviderType.MySql, custAddFilter);
            providerFixture.AddFilter(ProviderType.MySql, addressFilter1);
            // sales orders                        
            providerFixture.AddFilter(ProviderType.MySql, sohFilter);
            providerFixture.AddFilter(ProviderType.MySql, sodFilter);

            // add the parameter for the client
            providerFixture.AddFilterParameter(ProviderType.MySql, new SyncParameter("empId", 1));

            // My SQL (disable http to go faster on app veyor)
            providerFixture.AddRun((ProviderType.MySql, NetworkType.Tcp),
                    ProviderType.Sql |
                    ProviderType.MySql |
                    ProviderType.Sqlite);

            providerFixture.AddRun((ProviderType.MySql, NetworkType.Http),
                    ProviderType.Sql |
                    ProviderType.MySql |
                    ProviderType.Sqlite);
        }

    }
}
