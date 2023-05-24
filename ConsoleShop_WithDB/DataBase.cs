
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
                    "NN_Storehouse.ProductCount + MSC_Storehouse.ProductCount as AllProductCount " +
                    "from NN_Storehouse " + //первый склад
                    "JOIN MSC_Storehouse " + //второй склад
                    "on MSC_Storehouse.ProductId = NN_Storehouse.ProductId " +
                    "JOIN Products " + //товары
                    "on Products.Id = NN_Storehouse.ProductId "; 

                SQLiteDataAdapter adapter = new SQLiteDataAdapter(command, connectionString);                
                adapter.Fill(data);
            }

            //преобразование данных бд в дикшинери ProductsInShop
            Dictionary<Product, int> ProductsInShop = new Dictionary<Product, int>();
            foreach (DataRow row in data.Tables[0].Rows)
            {
                var cells = row.ItemArray;

                //проверка на нуль, и получение данных
                long id = cells[0] != null ? (Int64)cells[0] : 0;
                string name = cells[1] != null ? (string)cells[1] : "Неопределено";
                string category = cells[2] != null ? (string)cells[2] : "Неопределено";
                string description = cells[3] != null ? (string)cells[3] : "Неопределено";
                string made = cells[4] != null ? (string)cells[4] : "Неопределено";
                long price = cells[5] != null ? (Int64)cells[5] : 0;
                long count = cells[6] != null ? (Int64)cells[6] : 0;

                Product product = new Product((int)id, name, category, description, made, (int)price);
                ProductsInShop.Add(product, (int)count);
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
                //через транзакцию, по каждому продукту в корзине
                SqliteTransaction transaction = connection.BeginTransaction();

                connection.Open();
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
    }
}
