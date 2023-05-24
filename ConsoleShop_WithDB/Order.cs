

namespace ConsoleShop_WithDB
{
    internal class Order 
    {

        internal int IdClient { get; }

        internal DateTime DateTimeOrder { get; }

        internal Dictionary<Product, int> Purchase { get; }

        internal double Price { get; }

        internal Order(DateTime dateTimeOrder, int idClient, Dictionary<Product, int> purchase, double price)
        {
            IdClient = idClient;
            DateTimeOrder = dateTimeOrder;
            Purchase = purchase;
            Price = price;
        }

        internal Order(DateTime dateTimeOrder, int idClient, Dictionary<Product, int> purchase)
        {
            IdClient = idClient;
            DateTimeOrder = dateTimeOrder;
            Purchase = purchase;
        }

    }
        
}
