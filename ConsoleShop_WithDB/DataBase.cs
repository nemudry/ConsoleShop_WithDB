
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
    }
}
