namespace ConsoleShop_WithDB;
internal class Order 
{
    internal int IdClient { get; }
    internal DateTime DateTimeOrder { get; }
    internal Product Product { get; }
    internal int CountProduct { get; }
    internal double Price { get; }

    internal Order(DateTime dateTimeOrder, int idClient, int productId, int countProduct, double price)
    {
        IdClient = idClient;
        DateTimeOrder = dateTimeOrder;
        Product = Shop.GetProductById(productId);
        CountProduct = countProduct;
        Price = price;
    }
}        
