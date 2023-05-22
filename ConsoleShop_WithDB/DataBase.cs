
namespace ConsoleShop_WithDB
{
    internal static class DataBase
    {
        //расположение базы данных SQLite
        const string connectionString = $"Data Source = D:\\Source\\ConsoleShop_WithDB\\ConsoleShop_WithDB\\ShopDB.db";

        //Загрузка базы данных 
        internal static DataSet LoadDB()
        {
            using (SqliteConnection connection = new SqliteConnection(connectionString))
            {
                string command = "SELECT Products.Name, Products.Category, Products.Description, Products.Made, Products.Price," +
                    "NN_Storehouse.ProductCount + MSC_Storehouse.ProductCount as AllProductCount " +
                    "from NN_Storehouse " + //первый склад
                    "JOIN MSC_Storehouse " + //второй склад
                    "on MSC_Storehouse.ProductId = NN_Storehouse.ProductId " +
                    "JOIN Products " + //продукты
                    "on Products.Id = NN_Storehouse.ProductId " +
                    "WHERE AllProductCount > 0"; //в наличии

                SQLiteDataAdapter adapter = new SQLiteDataAdapter(command, connectionString);
                DataSet data = new DataSet();
                adapter.Fill(data);
                return data;
            }
        }
    }
}
