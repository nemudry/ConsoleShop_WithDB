namespace ConsoleShop_WithDB;
internal class ShopNN : Shop
{
    protected override string Name { get; }
    protected override string Description { get; }

    internal ShopNN ()
        :base()
    {
        Name = "Магазин \"Слизь Сизня\"";
        Description = "Нижегородское отделение лицензионной продукции по консольной РПГ \"Hero and SVIN\".";
    }
}
