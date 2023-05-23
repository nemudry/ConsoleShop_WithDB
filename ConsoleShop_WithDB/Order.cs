
namespace ConsoleShop_WithDB
{
    internal class Order
    {

        internal int IdOrder { get; }

        internal int IdClient { get; }

        internal DateTime DateTimeOrder { get; }

        internal Dictionary<Product, int> Purchase { get; }

        internal double Price { get; }

        internal Order (int idOrder, int idClient, DateTime dateTimeOrder, Dictionary<Product, int> purchase, double price)
        {
            IdOrder = idOrder;
            IdClient = idClient;
            DateTimeOrder = dateTimeOrder;
            Purchase = purchase;
            Price = price;
        }
    }
}
