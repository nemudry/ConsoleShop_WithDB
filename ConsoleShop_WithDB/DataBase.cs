
namespace ConsoleShop_WithDB
{
    internal static class DataBase
    {
        //расположение базы данных SQLite
        internal const string connectionString = $"Data Source = D:\\Source\\ConsoleShop_WithDB\\ConsoleShop_WithDB\\ShopDB.db";

        //Загрузка базы данных 
        internal static Dictionary<Product, int> LoadDB()
        {
            DataSet data = new DataSet();            

            using (SqliteConnection connection = new SqliteConnection(connectionString))
            {
                string command = "SELECT Products.Id, Products.Name, Products.Category, Products.Description, Products.Made, Products.Price," +
                    "NN_Storehouse.ProductCount + MSC_Storehouse.ProductCount as AllProductCount, " +
                    "IFNULL(Discounts.Discount, 0) as Discount " + // если скидки нет установить вместо null 0
                    "from NN_Storehouse " + //первый склад
                    "JOIN MSC_Storehouse " + //второй склад
                    "on MSC_Storehouse.ProductId = NN_Storehouse.ProductId " +
                    "JOIN Products " + //товары
                    "on Products.Id = NN_Storehouse.ProductId " +
                    "LEFT JOIN Discounts " + // скидки
                    "on Discounts.ProductId = NN_Storehouse.ProductId "; 

                SQLiteDataAdapter adapter = new SQLiteDataAdapter(command, connectionString);                
                adapter.Fill(data);
            }

            //преобразование данных бд в дикшинери ProductsInShop
            Dictionary<Product, int> ProductsInShop = new Dictionary<Product, int>();
            foreach (DataRow row in data.Tables[0].Rows)
            {
                var cells = row.ItemArray;

                //проверка на нуль, и получение данных
                object id = cells[0] ?? 0;
                object name = cells[1] ?? "Неопределено";
                object category = cells[2] ?? "Неопределено";
                object description = cells[3] ?? "Неопределено";
                object made = cells[4] ?? "Неопределено";
                object price = cells[5] ?? 0;
                object count = cells[6] ?? 0;
                object discount = cells[7] ?? 0;

                Product product = new Product((int)(Int64)id, (string)name, (string)category, (string)description, (string)made, 
                    (int)(Int64)price, (int)(Int64)discount);
                ProductsInShop.Add(product, (int)(Int64)count);
            }
            return ProductsInShop;            
        }

        //Покупка товара (уменьшение количества товара на складах в БД)
        internal static void SetBuyProductsDB(Dictionary<Product, int> order)
        {
            DataSet data = new DataSet();

            using (SqliteConnection connection = new SqliteConnection(connectionString))
            {
                //получение общих данных по количеству товара на разных складах
                string command = "SELECT NN_Storehouse.ProductId, Products.Name, " +
                    "NN_Storehouse.ProductCount as ProductCountNN, MSC_Storehouse.ProductCount as ProductCountMSC, " +
                    "NN_Storehouse.ProductCount + MSC_Storehouse.ProductCount as ProductCountAll " +
                    "FROM NN_Storehouse " +
                    "JOIN MSC_Storehouse " +
                    "on MSC_Storehouse.ProductId = NN_Storehouse.ProductId " +
                    "JOIN Products " +
                    "on Products.Id = NN_Storehouse.ProductId ";

                SQLiteDataAdapter adapter = new SQLiteDataAdapter(command, connectionString);
                adapter.Fill(data);
            }

            //фильтрация данных и получение листка productCountInStorehouses
            List<(Product, int, int)> productCountInStorehouses = new List<(Product, int, int)>();
            foreach (var product in order)
            {
                foreach (DataRow row in data.Tables[0].Rows)
                {
                    int id = (int)(Int64)row[0];

                    if (id == product.Key.Id)
                    {
                        int countNN = (int)(Int64)row[2]; //количество товара на первом складе
                        int countMSC = (int)(Int64)row[3]; //количество товара на втором складе
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
                            using (SqliteConnection connection = new SqliteConnection(connectionString))
                            {
                                connection.Open();
                                SqliteCommand command = new SqliteCommand();
                                command.Connection = connection;

                                //уменьшение товара на ближайшем складе
                                command.CommandText = "UPDATE NN_Storehouse " +
                                    "SET ProductCount = ProductCount - @prodBuyCount " +
                                    "WHERE ProductId = @prodBuyId;";

                                SqliteParameter idParam = new SqliteParameter("@prodBuyId", product.Key.Id);
                                SqliteParameter countParam = new SqliteParameter("@prodBuyCount", product.Value);
                                command.Parameters.Add(idParam);
                                command.Parameters.Add(countParam);

                                command.ExecuteNonQuery();
                            }
                        }
                        //если на ближайшем складе НЕдостаточно товара
                        else
                        {
                            using (SqliteConnection connection = new SqliteConnection(connectionString))
                            {
                                connection.Open();
                                SqliteCommand command = new SqliteCommand();
                                command.Connection = connection;

                                //уменьшение товара на обоих складе
                                command.CommandText = "UPDATE NN_Storehouse " +
                                    "SET ProductCount = ProductCount - @prodBuyCountNN " +
                                    "WHERE ProductId = @prodBuyId; " +
                                    "UPDATE MSC_Storehouse " +
                                    "SET ProductCount = ProductCount - @prodBuyCountMSC " +
                                    "WHERE ProductId = @prodBuyId;";

                                SqliteParameter idParam = new SqliteParameter("@prodBuyId", product.Key.Id);
                                // с первого слада NN берется сколько есть
                                SqliteParameter countNNParam = new SqliteParameter("@prodBuyCountNN", prod.Item2);
                                // со второго склада MSC добирается остаток
                                order[product.Key] -= prod.Item2;
                                SqliteParameter countMSCParam = new SqliteParameter("@prodBuyCountMSC", order[product.Key]);

                                command.Parameters.Add(idParam);
                                command.Parameters.Add(countNNParam);
                                command.Parameters.Add(countMSCParam);

                                command.ExecuteNonQuery();
                            }
                        }
                        break;
                    }
                }
            }
        }

        //формирование заказа в БД
        internal static void SetOrderDB(Order order)
        {
            using (SqliteConnection connection = new SqliteConnection(connectionString))
            {     
                connection.Open();

                //через транзакцию, по каждому продукту в корзине 
                SqliteTransaction transaction = connection.BeginTransaction();
                SqliteCommand command = new SqliteCommand();
                command.Connection = connection;
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

                        SqliteParameter dateParam = new SqliteParameter($"@dateOrder{numberProduct}", order.DateTimeOrder);
                        SqliteParameter clientIdParam = new SqliteParameter($"@clientId{numberProduct}", order.IdClient);
                        SqliteParameter productIdParam = new SqliteParameter($"@productId{numberProduct}", product.Key.Id);
                        SqliteParameter countProductParam = new SqliteParameter($"@countProduct{numberProduct}", product.Value);
                        SqliteParameter priceParam = new SqliteParameter($"@price{numberProduct}", product.Value * product.Key.Price);

                        command.Parameters.Add(dateParam);
                        command.Parameters.Add(clientIdParam);
                        command.Parameters.Add(productIdParam);
                        command.Parameters.Add(countProductParam);
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
            }  
        }

        //получение заказов из бд
        internal static List<Order> GetOrdersDB(int ClientId)
        {
            DataSet data = new DataSet();

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                SQLiteDataAdapter adapter = new SQLiteDataAdapter();

                SQLiteCommand command = new SQLiteCommand();
                command.Connection = connection;
                command.CommandText = "SELECT * " +
                    "FROM Orders " +
                    "WHERE Orders.ClientId = @idClient";

                SQLiteParameter idParam = new SQLiteParameter("@idClient", ClientId);
                command.Parameters.Add(idParam);

                adapter.SelectCommand = command;
                adapter.Fill(data);
            }

            //преобразование данных бд в листок заказов
            List<Order> orders = new List<Order>();
            foreach (DataRow row in data.Tables[0].Rows)
            {
                var cells = row.ItemArray;

                //проверка на нуль, и получение данных
                DateTime date = cells[1] != null ? DateTime.Parse((string)cells[1]) : DateTime.MinValue;
                int idClient = cells[2] != null ? (int)(Int64)cells[2] : 0;
                int idProduct = cells[3] != null ? (int)(Int64)cells[3] : 0;
                int count = cells[4] != null ? (int)(Int64)cells[4] : 0;
                int price = cells[5] != null ? (int)(Int64)cells[5] : 0;

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
            int isHasClint;

            //проверка только по логину
            if (password == null)
            {                
                using (SqliteConnection connection = new SqliteConnection(connectionString))
                {
                    connection.Open();
                    SqliteCommand command = new SqliteCommand();
                    command.Connection = connection;

                    //Проверка на наличие данного клиента в бд
                    command.CommandText = "SELECT Count(*) " +
                        "FROM Clients " +
                        "WHERE Clients.Login = @login;";

                    SqliteParameter loginParam = new SqliteParameter("@login", login);
                    command.Parameters.Add(loginParam);

                    isHasClint = (int)(Int64)command.ExecuteScalar();
                }
            }
            //проверка по логину/паролю
            else
            {
                using (SqliteConnection connection = new SqliteConnection(connectionString))
                {
                    connection.Open();
                    SqliteCommand command = new SqliteCommand();
                    command.Connection = connection;

                    //Проверка на наличие данного клиента в бд по логину/паролю
                    command.CommandText = "SELECT Count(*) " +
                        "FROM Clients " +
                        "WHERE Clients.Login = @login AND Clients.ClientPassword = @password;";

                    SqliteParameter loginParam = new SqliteParameter("@login", login);
                    SqliteParameter passwordParam = new SqliteParameter("@password", password);
                    command.Parameters.Add(loginParam);
                    command.Parameters.Add(passwordParam);

                    isHasClint = (int)(Int64)command.ExecuteScalar();
                }
            }
            return isHasClint;
        }

        //регистрация нового клиента
        internal static void SetNewClientDB(string fullName, string login, string password)
        {
            using (SqliteConnection connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                SqliteCommand command = new SqliteCommand();
                command.Connection = connection;

                //Внести клиента в бд
                command.CommandText = "INSERT INTO Clients (FullName, Login, ClientPassword) VALUES " +
                    "(@fullname, @login, @password);";

                SqliteParameter fullnameParam = new SqliteParameter("@fullname", fullName);
                SqliteParameter loginParam = new SqliteParameter("@login", login);
                SqliteParameter passwordParam = new SqliteParameter("@password", password);
                command.Parameters.Add(fullnameParam);
                command.Parameters.Add(loginParam);
                command.Parameters.Add(passwordParam);

                command.ExecuteNonQuery();                           
            }
        }

        //получение клиента из БД
        internal static (int id, string name) GetClientDB (string login, string password)
        {
            int id = 0;
            string name = null;
            using (SqliteConnection connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                SqliteCommand command = new SqliteCommand();
                command.Connection = connection;

                //Получение id клиента
                command.CommandText = "SELECT Clients.Id, Clients.FullName " +
                    "FROM Clients " +
                    "WHERE Clients.Login = @login AND Clients.ClientPassword = @password ";

                SqliteParameter loginParam = new SqliteParameter("@login", login);
                SqliteParameter passwordParam = new SqliteParameter("@password", password);
                command.Parameters.Add(loginParam);
                command.Parameters.Add(passwordParam);

                SqliteDataReader reader = command.ExecuteReader();
                
                if (reader.HasRows)
                {
                    //авторизация
                    while (reader.Read())
                    {
                        id = (int)(Int64)reader.GetValue(0);
                        name = (string)reader.GetValue(1);                        
                    }
                }
                reader.Close();
            }
            return (id, name);
        }
    }
}
