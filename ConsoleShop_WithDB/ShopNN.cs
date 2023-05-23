
namespace ConsoleShop_WithDB
{
    internal class ShopNN : Shop
    {
        protected override string Name { get; }
        protected override string Description { get; }
        protected override Dictionary<Product, int> ProductsInShop { get; }
        protected override Client Client { get; set; }

        public ShopNN ()
        {
            Name = "Магазин \"Слизь Сизня\"";
            Description = "Нижегородское отделение лицензионной продукции по консольной РПГ \"Hero and SVIN\".";
            PlaceInShop = placeStatus.ВходВМагазин;
            Client = new Client();
            ProductsInShop = new Dictionary<Product, int> ();
            DataBase.LoadDB();
            GetProductsFromDB(DataBase.Data);
        }
    }
}
