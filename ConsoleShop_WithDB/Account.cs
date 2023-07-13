namespace ConsoleShop_WithDB;
internal class Account
{
    internal int ClientId { get; }
    internal string ClientFullName { get; }       
    internal Busket Busket { get; }
    protected virtual List<Order> Orders { get; }
    internal purchaseStatus PurchaseStatus { get; set; }
    internal enum purchaseStatus { НоваяПокупка, ПродуктыВкорзине, ЗакончитьПокупку }
    internal clientStatus ClientStatus { get; set; }
    internal enum clientStatus { Авторизован, Аноним }

    //основной конструктор для работы в приложении
    internal Account(int id, string fullName, purchaseStatus purchaseStatus, clientStatus clientStatus, List<Order> orders, Busket busket)
    {
        ClientId = id;
        ClientFullName = fullName;
        Busket = busket;
        Orders = orders;
        PurchaseStatus = purchaseStatus;
        ClientStatus = clientStatus;
    }

    //гостевой аккаунт
    internal Account()
     : this(0, "Гость", purchaseStatus.НоваяПокупка, clientStatus.Аноним, null, new Busket())
    { }

    //деавторизация
    internal Account(purchaseStatus purchaseStatus, Busket busket)
        : this(0, "Гость", purchaseStatus, clientStatus.Аноним, null, busket)
    { }

    //авторизация
    internal Account(int id, string fullName, purchaseStatus purchaseStatus, Busket busket)
        : this(id, fullName, purchaseStatus, clientStatus.Авторизован, DataBase.GetOrdersDBAsync(id).Result, busket)
    { }

    // показать историю заказов
    internal void HistoryOrdersInfo()
    {
        if (Orders.Count != 0)
        {
            Console.Clear();
            Color.Green("История заказов:");
            Console.WriteLine();

            //номера заказов (даты)
            var idOrders = Orders.Select(e => e.DateTimeOrder).Distinct();

            int numberOrder = 1;
            double price = 0;
            foreach (var idOrder in idOrders)
            {
                Console.WriteLine($"[{numberOrder}]. {ClientId}-{idOrder} ");
                foreach (var order in Orders)
                {
                    if (idOrder == order.DateTimeOrder)
                    {
                        Console.WriteLine($"{order.Product.Name} - {order.CountProduct}шт.");
                        price += order.Price;
                    }
                }
                Console.WriteLine($"Цена заказа - {price}р.");
                Console.WriteLine();
                price = 0;
                numberOrder++;
            }
        }
        else
        {
            Color.Red("Заказы отсутствуют!");
            Console.WriteLine();
        }
    }
}
