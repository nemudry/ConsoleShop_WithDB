namespace ConsoleShop_WithDB;
internal class DataBase
{
    static DataBase ()
    {
        DbProviderFactories.RegisterFactory("SQLite", SQLiteFactory.Instance);
        DbProviderFactories.RegisterFactory("SqlServer", SqlClientFactory.Instance);

        //Данные о поставщике СУБД и строка подключения - в конфиг.файле
        string provider = ConfigurationManager.AppSettings["provider"];
        _connectionString = ConfigurationManager.ConnectionStrings[provider].ConnectionString;

        Factory = DbProviderFactories.GetFactory(provider);
    }       
    
    private static readonly DbProviderFactory Factory; // поставщик субд
    private static readonly string _connectionString; // строка подключения
    private static DbConnection Connection { get; set; } 

    //открыть соединение
    private static async Task OpenConnAsync()
    {
        Connection = Factory.CreateConnection();
        Connection.ConnectionString = _connectionString;
        await Connection.OpenAsync();
    }

    //закрыть соединение
    private static async Task CloseConnAsync()
    {
        if (Connection?.State != ConnectionState.Closed)
            await Connection?.CloseAsync();
    }

    //Загрузка продуктов в магазин
    internal static async Task<Dictionary<Product, int>> LoadDBAsync()
    {
        Dictionary<Product, int> ProductsInShop = new Dictionary<Product, int>();
        try
        {
            DataSet data = new DataSet();
            await OpenConnAsync();
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
                else if (Factory is SqlClientFactory) command.CommandText += "ISNULL(Discounts.Discount, 0) as Discount "; //различие в языке запроса SQL - ifnull isnull
                else command.CommandText += "Discounts.Discount ";
                command.CommandText += "from NN_Storehouse " + //первый склад
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
            await CloseConnAsync();

            //преобразование данных бд в дикшинери ProductsInShop
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
                Converter.ParseData(idObj, out int id, out string _);
                Converter.ParseData(nameObj, out int _, out string name);
                Converter.ParseData(categoryObj, out int _, out string category);
                Converter.ParseData(descriptionObj, out int _, out string description);
                Converter.ParseData(madeObj, out int _, out string made);
                Converter.ParseData(priceObj, out int price, out string _);
                Converter.ParseData(countObj, out int count, out string _);
                Converter.ParseData(discountObj, out int discount, out string _);

                Product product = new Product(id, name, category, description, made, price, discount);
                ProductsInShop.Add(product, count);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Ошибка при загрузке товаров в магазин!");
            Exceptions.ShowExInfo(e);
            Feedback.AcceptPlayer();
        }
        return ProductsInShop;            
    }

    //Покупка товара (уменьшение количества товара на складах в БД)
    internal static async Task SetBuyProductsDBAsync(Dictionary<Product, int> order)
    {
        try
        {
            DataSet data = new DataSet();
            await OpenConnAsync();
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
            await CloseConnAsync();

            //фильтрация данных и получение листка productCountInStorehouses
            List<(Product, int, int)> productCountInStorehouses = new List<(Product, int, int)>();
            foreach (var product in order)
            {
                foreach (DataRow row in data.Tables[0].Rows)
                {
                    //получение данных
                    object idObj = row[0];

                    //приведение
                    Converter.ParseData(idObj, out int id, out string _);

                    if (id == product.Key.Id)
                    {
                        object countNNObj = row[2]; //количество товара на первом складе
                        object countMSCObj = row[3]; //количество товара на втором складе

                        //приведение
                        Converter.ParseData(countNNObj, out int countNN, out string _);
                        Converter.ParseData(countMSCObj, out int countMSC, out string _);

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
                        //если на ближайшем складе достаточно товара - уменьшение товара на ближайшем складе
                        if (prod.Item2 >= product.Value)
                        {
                            await OpenConnAsync();
                            using (DbCommand command = Factory.CreateCommand())
                            {
                                command.Connection = Connection;
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

                                await command.ExecuteNonQueryAsync();
                            }
                            await CloseConnAsync();
                        }
                        //если на ближайшем складе недостаточно товара - уменьшение товара на обоих складе
                        else
                        {
                            await OpenConnAsync();
                            using (DbCommand command = Factory.CreateCommand())
                            {
                                command.Connection = Connection;
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

                                await command.ExecuteNonQueryAsync();
                            }
                            await CloseConnAsync();
                        }
                        break;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Ошибка при уменьшении товара в базе данных!");
            Exceptions.ShowExInfo(e);
            Feedback.AcceptPlayer();
        }
    }

    //формирование заказа в БД
    internal static async Task SetOrderDBAsync(DateTime dateTimeOrder, Account account, Dictionary<Product, int> order)
    {
        await OpenConnAsync();
        using (DbCommand command = Factory.CreateCommand())
        {
            //через транзакцию, по каждому продукту в корзине 
            DbTransaction transaction = Connection.BeginTransaction();
            command.Connection = Connection;
            command.Transaction = transaction;
            
            try
            {
                int numberProduct = 1;
                foreach (var product in order)
                {
                    //создание заказа
                    command.CommandText = $"INSERT INTO Orders (DateOrder, ClientId, ProductId, CountProduct, Price) VALUES " +
                        $"(@dateOrder{numberProduct}, @clientId{numberProduct}, @productId{numberProduct}, " +
                        $"@countProduct{numberProduct}, @price{numberProduct})";

                    DbParameter dateParam = Factory.CreateParameter();
                    dateParam.ParameterName = $"@dateOrder{numberProduct}";
                    dateParam.Value = dateTimeOrder.ToString();
                    command.Parameters.Add(dateParam);

                    DbParameter clientIdParam = Factory.CreateParameter();
                    clientIdParam.ParameterName = $"@clientId{numberProduct}";
                    clientIdParam.Value = account.ClientId;
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

                    await command.ExecuteNonQueryAsync();
                    numberProduct++;
                }
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при формировании заказа в базе данных!");
                Exceptions.ShowExInfo(ex);
                await transaction.RollbackAsync();
                Feedback.AcceptPlayer();
            }
            finally
            {
                await CloseConnAsync();
            }
        }  
    }

    //получение заказов из бд
    internal static async Task<List<Order>> GetOrdersDBAsync(int ClientId)
    {
        List<Order> orders = new List<Order>();
        try
        {
            DataSet data = new DataSet();
            await OpenConnAsync();
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
            await CloseConnAsync();

            //преобразование данных бд в листок заказов
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
                Converter.ParseData(idClientObj, out int idClient, out string _);
                Converter.ParseData(idProductObj, out int idProduct, out string _);
                Converter.ParseData(countObj, out int count, out string _);
                Converter.ParseData(priceObj, out int price, out string _);

                Order order = new Order(date, idClient, idProduct, count, price);
                orders.Add(order);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Ошибка при получении заказов клиента из базы данных!");
            Exceptions.ShowExInfo(e);
            Feedback.AcceptPlayer();
        }
        return orders;
    }

    //проверка наличия клиента в бд
    internal static async Task<bool> CheckClientDBAsync(string login, string password = null)
    {
        int isHasClient = 0;
        try
        {
            //проверка только по логину
            if (password == null)
            {
                await OpenConnAsync();
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

                    object isHasClientObj = await command.ExecuteScalarAsync();
                    Converter.ParseData(isHasClientObj, out isHasClient, out string _);
                }
                await CloseConnAsync();
            }
            //проверка по логину/паролю
            else
            {
                await OpenConnAsync();
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

                    object isHasClientObj = await command.ExecuteScalarAsync();
                    Converter.ParseData(isHasClientObj, out isHasClient, out string _);
                }
                await CloseConnAsync();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Ошибка при проверке клиента в базе данных!");
            Exceptions.ShowExInfo(e);
            Feedback.AcceptPlayer();
        }
        return isHasClient == 0 ? false : true;
    }

    //регистрация нового клиента
    internal static async Task<bool> SetNewClientDBAsync(string fullName, string login, string password)
    {
        var result = false;
        try
        {
            await OpenConnAsync();
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

                await command.ExecuteNonQueryAsync();
            }
            await CloseConnAsync();
            result = true;
        }
        catch (Exception e)
        {
            Console.WriteLine("Ошибка при создании клиента в базе данных!");
            Exceptions.ShowExInfo(e);
            Feedback.AcceptPlayer();
        }
        return result;
    }

    //получение клиента из БД
    internal static async Task<(int id, string name)> GetClientDBAsync(string login, string password)
    {
        int id = 0;
        string name = null;
        try
        {
            await OpenConnAsync();
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

                DbDataReader reader = await command.ExecuteReaderAsync();

                if (reader.HasRows)
                {
                    //авторизация
                    while (await reader.ReadAsync())
                    {
                        object idObj = reader.GetValue(0);
                        object nameObj = reader.GetValue(1);

                        Converter.ParseData(idObj, out id, out string _);
                        Converter.ParseData(nameObj, out int _, out name);
                    }
                }
                await reader.CloseAsync();
            }
            await CloseConnAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine("Ошибка при получении клиента из базы данных!");
            Exceptions.ShowExInfo(e);
            Feedback.AcceptPlayer();
        }
        return (id, name);
    }
}
