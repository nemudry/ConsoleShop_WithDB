namespace ConsoleShop_WithDB;
//корзина
internal class Busket
{
    //продукты в корзине
    internal Dictionary<Product, int> ProductsInBusket { get; }
    internal Busket()
    {
        ProductsInBusket = new Dictionary<Product, int>();
    }

    // общая цена товаров в корзине
    internal double TotalSum()
    {
        double sum = 0;
        foreach (var product in ProductsInBusket)
            sum += product.Key.Price * product.Value;
        return sum;
    }

    // перебор товаров в корзине
    internal void BusketInfo()
    {
        Console.Clear();

        Color.Cyan("Товары в корзине:");
        int numberInBusket = 0;
        foreach (var product in ProductsInBusket)
        {
            Console.WriteLine($"[{++numberInBusket}]. Товар \"{product.Key.Name}\", количество {product.Value} шт., " +
                $"общая цена: {product.Value * product.Key.Price}р");
        }
        Console.WriteLine($"Стоимость всех товаров в корзине составляет {TotalSum()}р.");
        Console.WriteLine();
    }
}
