
namespace ConsoleShop_WithDB
{
    internal class Account
    {
        internal int ClientId { get; }
        internal string ClientFullName { get; }       
        internal Busket Busket { get; }
        protected virtual List<Order> HistoryPurchase { get; }
        internal purchaseStatus PurchaseStatus { get; set; }
        internal enum purchaseStatus
        {
            НоваяПокупка,
            ПродуктыВкорзине,
            ЗакончитьПокупку,
        }
        internal clientStatus ClientStatus { get; set; }
        internal enum clientStatus
        {
            Авторизован,
            Аноним
        }

        internal Account()
        {
            ClientId = 0;
            ClientFullName = "Гость";            
            Busket = new Busket();
            HistoryPurchase = null;
            PurchaseStatus = purchaseStatus.НоваяПокупка;
            ClientStatus = clientStatus.Аноним;
        }

        internal Account(Busket busket, purchaseStatus purchaseStatus)
        {
            ClientId = 0;
            ClientFullName = "Гость";
            Busket = busket;
            HistoryPurchase = null;
            PurchaseStatus = purchaseStatus;
            ClientStatus = clientStatus.Аноним;
        }

        internal Account(int id, string fullName, Busket busket, purchaseStatus purchaseStatus)
        {
            ClientId = id;
            ClientFullName = fullName;
            Busket = busket;
            HistoryPurchase = DataBase.GetOrdersDBAsync(id).Result;
            PurchaseStatus = purchaseStatus;
            ClientStatus = clientStatus.Авторизован;
        }
                
        // Оплата товара
        internal async Task PayPayment()
        {
            int answerPayment;
            while (true)
            {
                Console.Clear();

                // выберите способ оплаты
                while (true)
                {
                    Console.WriteLine($"Стоимость всех товаров в корзине составляет {Busket.TotalSum()}р.");
                    Color.Cyan("Выберите способ оплаты: ");
                    Console.WriteLine("[1]. Оплата по карте. \n[-1]. Вернуться в корзину.");
                    answerPayment = Feedback.PlayerAnswer();

                    if (Feedback.CheckСonditions(answerPayment, 1, 1, -1)) break;
                }

                //3. Оплата по карте.
                if (answerPayment == 1)
                {
                    //формирование заказа в бд
                    Order order = new Order(DateTime.Now, ClientId, Busket.ProductsInBusket);
                    await DataBase.SetOrderDBAsync(order);

                    //покупка товаров(уменьшение товара на складах)
                    await DataBase.SetBuyProductsDBAsync(Busket.ProductsInBusket);

                    Color.Green($"Денежные средства в размере {Busket.TotalSum()}р списаны с Вашей банковской карты. Благодарим за покупку!");
                    Feedback.ReadKey();

                    // очистить корзину
                    Busket.ProductsInBusket.Clear();

                    PurchaseStatus = purchaseStatus.НоваяПокупка;
                    break;
                }

                // Вернуться в корзину.
                if (answerPayment == -1) break;
            }
        }       

        // показать историю заказов
        internal void HistoryPurchaseInfo ()
        {
            if (HistoryPurchase.Count != 0)
            {
                Console.Clear();
                Color.Green("История заказов:");
                Console.WriteLine();

                //номера заказов (даты)
                var idOrders = HistoryPurchase.Select(e => e.DateTimeOrder).Distinct();

                int numberOrder = 1;
                double price = 0;
                foreach (var idOrder in idOrders)
                {
                    Console.WriteLine($"[{numberOrder}]. {ClientId}-{idOrder} ");
                    foreach (var order in HistoryPurchase)
                    {                        
                        if (idOrder == order.DateTimeOrder)
                        {
                            foreach (var purchase in order.Purchase)
                            {
                                Console.WriteLine($"{purchase.Key.Name} - {purchase.Value}шт.");
                                price += purchase.Key.Price * purchase.Value;
                            }                            
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
                Color.Red("Заказы отсутсвуют!");
                Console.WriteLine();
            }
        }
    }
}
