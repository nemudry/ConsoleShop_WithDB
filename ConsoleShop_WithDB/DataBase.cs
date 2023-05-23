
namespace ConsoleShop_WithDB
{
    internal static class DataBase
    {
        //расположение базы данных SQLite
        const string connectionString = $"Data Source = D:\\Source\\ConsoleShop_WithDB\\ConsoleShop_WithDB\\ShopDB.db";

        //загруженная БД
        static internal DataSet Data { get; } = new DataSet();

        //Загрузка базы данных 
        internal static void LoadDB()
        {
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
                adapter.Fill(Data);
            }
        }

        //Покупка товара (уменьшение количества товара на складах в БД)
        internal static void BuyProductsDB(Dictionary<Product, int> order)
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
    }
}
