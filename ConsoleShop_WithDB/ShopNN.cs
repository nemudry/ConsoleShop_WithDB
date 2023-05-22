
namespace ConsoleShop_WithDB
{
    internal class ShopNN : Shop
    {
        protected override string Name { get; }
        protected override string Description { get; }
        protected override DataSet Data { get; }
        public ShopNN ()
        {
            Name = "Магазин \"Слизь Сизня\"";
            Description = "Нижегородское отделение лицензионной продукции по консольной РПГ \"Hero and SVIN\".";
            Data = DataBase.LoadDB();            
        }
    }
}
