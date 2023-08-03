namespace ConsoleShop_WithDB;
//заказ
public class Order
{
    //клиент-покупатель
    internal int IdClient { get; }
    //дата и время заказа
    internal DateTime DateTimeOrder { get; }
    //купленный товар
    internal Product Product { get; }
    //количество товара
    internal int CountProduct { get; }
    //цена
    internal double Price { get; }

    public Order(DateTime dateTimeOrder, int idClient, int productId, int countProduct, double price)
    {
        IdClient = idClient;
        DateTimeOrder = dateTimeOrder;
        Product = Shop.GetProductById(productId);
        CountProduct = countProduct;
        Price = price;
    }
}        
