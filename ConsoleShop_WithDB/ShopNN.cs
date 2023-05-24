
namespace ConsoleShop_WithDB
{
    internal class ShopNN : Shop
    {
        protected override string Name { get; }
        protected override string Description { get; }
        protected override Account Account { get; set; }

        internal ShopNN ()
        {
            Name = "Магазин \"Слизь Сизня\"";
            Description = "Нижегородское отделение лицензионной продукции по консольной РПГ \"Hero and SVIN\".";
            PlaceInShop = placeStatus.ВходВМагазин;
            Account = new Account();
        }
    }
}
