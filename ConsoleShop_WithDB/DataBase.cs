using System.Data.Common;
using System.Configuration;
using System.Data.SQLite;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ConsoleShop_WithDB
{    
    internal static class DataBase
    {
        static DataBase ()
        {
            DbProviderFactories.RegisterFactory("SQLite", System.Data.SQLite.SQLiteFactory.Instance);
            DbProviderFactories.RegisterFactory("SqlServer", Microsoft.Data.SqlClient.SqlClientFactory.Instance);

            //Данные о поставщике СУБД и строка подключения - в конфиг.файле
            string provider = ConfigurationManager.AppSettings["provider"];
            _connectionString = ConfigurationManager.ConnectionStrings[provider].ConnectionString;

            Factory = DbProviderFactories.GetFactory(provider);
        }        
        private static readonly DbProviderFactory Factory;
        private static readonly string _connectionString;
        private static DbConnection Connection { get; set; }


        //Загрузка базы данных 
        internal static Dictionary<Product, int> LoadDB()
        {
            DataSet data = new DataSet();

            OpenConn();
            using (DbCommand command = Factory.CreateCommand())
            {                
                command.Connection = Connection;
                command.CommandText = "SELECT Products.Id, Products.Name, Products.Category, Products.Description, Products.Made, Products.Price," +
                    "NN_Storehouse.ProductCount + MSC_Storehouse.ProductCount as AllProductCount, " + 
                    "ISNULL(Discounts.Discount, 0) as Discount " + // если скидки нет установить вместо null 0
                    "from NN_Storehouse " + //первый склад
                    "JOIN MSC_Storehouse " + //второй склад
                    "on MSC_Storehouse.ProductId = NN_Storehouse.ProductId " +
                    "JOIN Products " + //товары
                    "on Products.Id = NN_Storehouse.ProductId " +
                    "LEFT JOIN Discounts " + // скидки
                    "on Discounts.ProductId = NN_Storehouse.ProductId ";

                command.CommandText = "SELECT Products.Id, Products.Name, Products.Category, Products.Description, Products.Made, Products.Price," +
                    "NN_Storehouse.ProductCount + MSC_Storehouse.ProductCount as AllProductCount, ";
                if (Factory is SQLiteFactory) command.CommandText += "IFNULL(Discounts.Discount, 0) as Discount "; // если скидки нет установить вместо null 0
                else if (Connection is SqlClientFactory) command.CommandText += "ISNULL(Discounts.Discount, 0) as Discount "; //различие в языке запроса SQL - ifnull isnull
                else command.CommandText += "Discounts.Discount ";
                command.CommandText +=  "from NN_Storehouse " + //первый склад
                      "JOIN MSC_Storehouse " + //второй склад
                     "on MSC_Storehouse.ProductId = NN_Storehouse.ProductId " +
                      "JOIN Products " + //товары
                      "on Products.Id = NN_Storehouse.ProductId " +
                      "LEFT JOIN Discounts " + // скидки
                      "on Discounts.ProductId = NN_Storehouse.ProductId ";

                DbDataAdapter adapter = Factory.CreateDataAdapter();
                adapter.SelectCommand = command;
                adapter.Fill(data);
            }
            CloseConn();

            //преобразование данных бд в дикшинери ProductsInShop
            Dictionary<Product, int> ProductsInShop = new Dictionary<Product, int>();
            foreach (DataRow row in data.Tables[0].Rows)
            {
                var cells = row.ItemArray;

                //проверка на нуль, и получение данных
                object idObj = cells[0] ?? 0;
                object nameObj = cells[1] ?? "Неопределено";
                object categoryObj = cells[2] ?? "Неопределено";
                object descriptionObj = cells[3] ?? "Неопределено";
                object madeObj = cells[4] ?? "Неопределено";
                object priceObj = cells[5] ?? 0;
                object countObj = cells[6] ?? 0;
                object discountObj = cells[7] ?? 0;

                //приведение в зависимости от типа хранимых данных
                ParseData(idObj, out int id, out string _);
                ParseData(nameObj, out int _, out string name);
                ParseData(categoryObj, out int _, out string category);
                ParseData(descriptionObj, out int _, out string description);
                ParseData(madeObj, out int _, out string made);
                ParseData(priceObj, out int price, out string _);
                ParseData(countObj, out int count, out string _);
                ParseData(discountObj, out int discount, out string _);

                Product product = new Product(id, name, category, description, made, price, discount);
                ProductsInShop.Add(product, count);
            }
            return ProductsInShop;            
        }

        //Покупка товара (уменьшение количества товара на складах в БД)
        internal static void SetBuyProductsDB(Dictionary<Product, int> order)
        {
            DataSet data = new DataSet();

            OpenConn();
            using (DbCommand command = Factory.CreateCommand())
            {
                command.Connection = Connection;
                //получение общих данных по количеству товара на разных складах
                command.CommandText = "SELECT NN_Storehouse.ProductId, Products.Name, " +
                    "NN_Storehouse.ProductCount as ProductCountNN, MSC_Storehouse.ProductCount as ProductCountMSC, " +
                    "NN_Storehouse.ProductCount + MSC_Storehouse.ProductCount as ProductCountAll " +
                    "FROM NN_Storehouse " +
                    "JOIN MSC_Storehouse " +
                    "on MSC_Storehouse.ProductId = NN_Storehouse.ProductId " +
                    "JOIN Products " +
                    "on Products.Id = NN_Storehouse.ProductId ";

                DbDataAdapter adapter = Factory.CreateDataAdapter();
                adapter.SelectCommand = command;
                adapter.Fill(data);
            }
            CloseConn();

            //фильтрация данных и получение листка productCountInStorehouses
            List<(Product, int, int)> productCountInStorehouses = new List<(Product, int, int)>();
            foreach (var product in order)
            {
                foreach (DataRow row in data.Tables[0].Rows)
                {
                    //получение данных
                    object idObj = row[0];

                    //приведение
                    ParseData(idObj, out int id, out string _);

                    if (id == product.Key.Id)
                    {
                        object countNNObj = row[2]; //количество товара на первом складе
                        object countMSCObj = row[3]; //количество товара на втором складе

                        //приведение
                        ParseData(countNNObj, out int countNN, out string _);
                        ParseData(countMSCObj, out int countMSC, out string _);

                        productCountInStorehouses.Add((product.Key, countNN, countMSC));
                        break;
                    }
                }
            }

            //Внесение корректировок в бд, уменьшение товара на складах 
            foreach (var product in order)
            {
                foreach (var prod in productCountInStorehouses)
                {
                    if (product.Key == prod.Item1)
                    {
                        //если на ближайшем складе достаточно товара
                        if (prod.Item2 >= product.Value)
                        {
                            OpenConn();
                            using (DbCommand command = Factory.CreateCommand())
                            {
                                command.Connection = Connection;
                                //уменьшение товара на ближайшем складе
                                command.CommandText = "UPDATE NN_Storehouse " +
                                    "SET ProductCount = ProductCount - @prodBuyCount " +
                                    "WHERE ProductId = @prodBuyId;";

                                DbParameter idParam = Factory.CreateParameter();
                                idParam.ParameterName = "@prodBuyId";
                                idParam.Value = product.Key.Id;
                                command.Parameters.Add(idParam);

                                DbParameter countParam = Factory.CreateParameter();
                                countParam.ParameterName = "@prodBuyCount";
                                countParam.Value = product.Value;
                                command.Parameters.Add(countParam);

                                command.ExecuteNonQuery();
                            }
                            CloseConn();
                        }
                        //если на ближайшем складе НЕдостаточно товара
                        else
                        {
                            OpenConn();
                            using (DbCommand command = Factory.CreateCommand())
                            {
                                command.Connection = Connection;
                                //уменьшение товара на обоих складе
                                command.CommandText = "UPDATE NN_Storehouse " +
                                    "SET ProductCount = ProductCount - @prodBuyCountNN " +
                                    "WHERE ProductId = @prodBuyId; " +
                                    "UPDATE MSC_Storehouse " +
                                    "SET ProductCount = ProductCount - @prodBuyCountMSC " +
                                    "WHERE ProductId = @prodBuyId;";

                                DbParameter idParam = Factory.CreateParameter();
                                idParam.ParameterName = "@prodBuyId";
                                idParam.Value = product.Key.Id;
                                command.Parameters.Add(idParam);

                                // с первого слада NN берется сколько есть
                                DbParameter countNNParam = Factory.CreateParameter();
                                countNNParam.ParameterName = "@prodBuyCountNN";
                                countNNParam.Value = prod.Item2;
                                command.Parameters.Add(countNNParam);

                                // со второго склада MSC добирается остаток
                                order[product.Key] -= prod.Item2;
                                DbParameter countMSCParam = Factory.CreateParameter();
                                countMSCParam.ParameterName = "@prodBuyCountMSC";
                                countMSCParam.Value = order[product.Key];
                                command.Parameters.Add(countMSCParam);

                                command.ExecuteNonQuery();
                            }
                            CloseConn();
                        }
                        break;
                    }
                }
            }
        }

        //формирование заказа в БД
        internal static void SetOrderDB(Order order)
        {
            OpenConn();
            using (DbCommand command = Factory.CreateCommand())
            {

                //через транзакцию, по каждому продукту в корзине 
                DbTransaction transaction = Connection.BeginTransaction();
                command.Connection = Connection;
                command.Transaction = transaction;
                
                try
                {
                    int numberProduct = 1;
                    foreach (var product in order.Purchase)
                    {
                        //создание заказа
                        command.CommandText = $"INSERT INTO Orders (DateOrder, ClientId, ProductId, CountProduct, Price) VALUES " +
                            $"(@dateOrder{numberProduct}, @clientId{numberProduct}, @productId{numberProduct}, " +
                            $"@countProduct{numberProduct}, @price{numberProduct})";

                        DbParameter dateParam = Factory.CreateParameter();
                        dateParam.ParameterName = $"@dateOrder{numberProduct}";
                        dateParam.Value = order.DateTimeOrder;
                        command.Parameters.Add(dateParam);

                        DbParameter clientIdParam = Factory.CreateParameter();
                        clientIdParam.ParameterName = $"@clientId{numberProduct}";
                        clientIdParam.Value = order.IdClient;
                        command.Parameters.Add(clientIdParam);

                        DbParameter productIdParam = Factory.CreateParameter();
                        productIdParam.ParameterName = $"@productId{numberProduct}";
                        productIdParam.Value = product.Key.Id;
                        command.Parameters.Add(productIdParam);

                        DbParameter countProductParam = Factory.CreateParameter();
                        countProductParam.ParameterName = $"@countProduct{numberProduct}";
                        countProductParam.Value = product.Value;
                        command.Parameters.Add(countProductParam);

                        DbParameter priceParam = Factory.CreateParameter();
                        priceParam.ParameterName = $"@price{numberProduct}";
                        priceParam.Value = product.Value * product.Key.Price;
                        command.Parameters.Add(priceParam);

                        command.ExecuteNonQuery();
                        numberProduct++;
                    }
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    transaction.Rollback();
                    Console.ReadKey();
                }
                finally
                {
                    CloseConn();
                }
            }  
        }

        //получение заказов из бд
        internal static List<Order> GetOrdersDB(int ClientId)
        {
            DataSet data = new DataSet();

            OpenConn();
            using (DbCommand command = Factory.CreateCommand())
            {
                command.Connection = Connection;
                command.CommandText = "SELECT * " +
                    "FROM Orders " +
                    "WHERE Orders.ClientId = @idClient";

                DbParameter idParam = Factory.CreateParameter();
                idParam.ParameterName = "@idClient";
                idParam.Value = ClientId;
                command.Parameters.Add(idParam);

                DbDataAdapter adapter = Factory.CreateDataAdapter();
                adapter.SelectCommand = command;
                adapter.Fill(data);
            }
            CloseConn();

            //преобразование данных бд в листок заказов
            List<Order> orders = new List<Order>();
            foreach (DataRow row in data.Tables[0].Rows)
            {
                var cells = row.ItemArray;

                //проверка на нуль, и получение данных
                object dateObj = cells[1] ?? "Неопределено";
                object idClientObj = cells[2] ?? 0;
                object idProductObj = cells[3] ?? 0;
                object countObj = cells[4] ?? 0;
                object priceObj = cells[5] ?? 0;

                //приведение в зависимости от типа хранимых данных
                DateTime date = (string)dateObj == "Неопределено" ? DateTime.MinValue : DateTime.Parse((string)dateObj);
                ParseData(idClientObj, out int idClient, out string _);
                ParseData(idProductObj, out int idProduct, out string _);
                ParseData(countObj, out int count, out string _);
                ParseData(priceObj, out int price, out string _);

                Dictionary<Product, int> purchase = new Dictionary<Product, int>();
                Product product = Shop.GetProductById(idProduct);
                purchase.Add(product, count);

                Order order = new Order(date, idClient, purchase, price);
                orders.Add(order);
            }
            return orders;
        }

        //проверка наличия клиента в бд
        internal static int CheckClientDB(string login, string password = null)
        {
            int isHasClient;

            //проверка только по логину
            if (password == null)
            {
                OpenConn();
                using (DbCommand command = Factory.CreateCommand())
                {
                    command.Connection = Connection;   
                    //Проверка на наличие данного клиента в бд
                    command.CommandText = "SELECT Count(*) " +
                        "FROM Clients " +
                        "WHERE Clients.Login = @login;";
                    
                    DbParameter loginParam = Factory.CreateParameter();
                    loginParam.ParameterName = "@login";
                    loginParam.Value = login;
                    command.Parameters.Add(loginParam);

                    object isHasClientObj = command.ExecuteScalar();
                    ParseData(isHasClientObj, out isHasClient, out string _);
                }
                CloseConn();
            }
            //проверка по логину/паролю
            else
            {
                OpenConn();
                using (DbCommand command = Factory.CreateCommand())
                {
                    command.Connection = Connection;
                    //Проверка на наличие данного клиента в бд по логину/паролю
                    command.CommandText = "SELECT Count(*) " +
                        "FROM Clients " +
                        "WHERE Clients.Login = @login AND Clients.ClientPassword = @password;";

                    DbParameter loginParam = Factory.CreateParameter();
                    loginParam.ParameterName = "@login";
                    loginParam.Value = login;
                    command.Parameters.Add(loginParam);

                    DbParameter passwordParam = Factory.CreateParameter();
                    passwordParam.ParameterName = "@password";
                    passwordParam.Value = password;
                    command.Parameters.Add(passwordParam);

                    object isHasClientObj = command.ExecuteScalar();
                    ParseData(isHasClientObj, out isHasClient, out string _);
                }
                CloseConn();
            }
            return isHasClient;
        }

        //регистрация нового клиента
        internal static void SetNewClientDB(string fullName, string login, string password)
        {
            OpenConn();
            using (DbCommand command = Factory.CreateCommand())
            {
                command.Connection = Connection;
                //Внести клиента в бд
                command.CommandText = "INSERT INTO Clients (FullName, Login, ClientPassword) VALUES " +
                    "(@fullname, @login, @password);";

                DbParameter fullnameParam = Factory.CreateParameter();
                fullnameParam.ParameterName = "@fullname";
                fullnameParam.Value = fullName;
                command.Parameters.Add(fullnameParam);

                DbParameter loginParam = Factory.CreateParameter();
                loginParam.ParameterName = "@login";
                loginParam.Value = login;
                command.Parameters.Add(loginParam);

                DbParameter passwordParam = Factory.CreateParameter();
                passwordParam.ParameterName = "@password";
                passwordParam.Value = password;
                command.Parameters.Add(passwordParam);

                command.ExecuteNonQuery();                           
            }
            CloseConn();
        }

        //получение клиента из БД
        internal static (int id, string name) GetClientDB (string login, string password)
        {
            int id = 0;
            string name = null;
            OpenConn();
            using (DbCommand command = Factory.CreateCommand())
            {
                command.Connection = Connection;
                //Получение id клиента
                command.CommandText = "SELECT Clients.Id, Clients.FullName " +
                    "FROM Clients " +
                    "WHERE Clients.Login = @login AND Clients.ClientPassword = @password ";

                DbParameter loginParam = Factory.CreateParameter();
                loginParam.ParameterName = "@login";
                loginParam.Value = login;
                command.Parameters.Add(loginParam);

                DbParameter passwordParam = Factory.CreateParameter();
                passwordParam.ParameterName = "@password";
                passwordParam.Value = password;
                command.Parameters.Add(passwordParam);

                DbDataReader reader = command.ExecuteReader();
                
                if (reader.HasRows)
                {
                    //авторизация
                    while (reader.Read())
                    {
                        object idObj = reader.GetValue(0);
                        object nameObj = reader.GetValue(1); 

                        ParseData(idObj, out id, out string _);
                        ParseData(nameObj, out int _, out name);                  
                    }
                }
                reader.Close();
            }
            CloseConn();
            return (id, name);
        }

        //открыть соединение
        private static void OpenConn()
        {
            Connection = Factory.CreateConnection();
            Connection.ConnectionString = _connectionString;
            Connection.Open();
        }

        //закрыть соединение
        private static void CloseConn()
        {
            if (Connection?.State != ConnectionState.Closed)
                Connection?.Close();
        }

        //Конвертация данный в инт или строку
        private static void ParseData(object obj, out int intObj, out string strObj)  
        {
            switch (obj)
            {
                case Int64: 
                    intObj = (int)(Int64)obj; strObj = null; break;
                case Int32:
                    intObj = (int)obj; strObj = null; break;
                case string:
                    intObj = 0; strObj = (string)obj; break;
                default:
                    intObj = 0; strObj = null; break;
            }
        }
    }
}
