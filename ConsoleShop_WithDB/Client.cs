
namespace ConsoleShop_WithDB
{
    internal class Client
    {
        internal int Id { get; }
        internal string FullName { get; }
        internal List <Order> HistoryPurchase { get; }
        internal Busket Busket { get; }

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

        internal Client()
        {
            Id = 0;
            FullName = null;
            HistoryPurchase = null;
            Busket = new Busket();
            PurchaseStatus = purchaseStatus.НоваяПокупка;
            ClientStatus = clientStatus.Аноним;
        }

        internal Client(int id, string fullName, List<Order> historyPurchase, Busket busket, purchaseStatus purchaseStatus)
        {
            Id = id;
            FullName = fullName;
            HistoryPurchase = historyPurchase;
            Busket = busket;
            PurchaseStatus = purchaseStatus;
            ClientStatus = clientStatus.Авторизован;
        }
                
        // Оплата товара
        internal void PayPayment()
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
                    Color.Green($"Денежные средства в размере {Busket.TotalSum()}р списаны с Вашей банковской карты. Благодарим за покупку!");
                    Console.WriteLine();

                    Console.WriteLine($"Нажмите клавишу для продолжения.");
                    Console.ReadKey();

                    //покупка товаров
                    DataBase.BuyProductsDB(Busket.ProductsInBusket);

                    // Order order = new Order(1, Id, DateTime.Now, Busket.ProductsInBusket, Busket.TotalSum());

                    // очистить корзину
                    Busket.ProductsInBusket.Clear();

                    PurchaseStatus = purchaseStatus.НоваяПокупка;
                    break;
                }

                // Вернуться в корзину.
                if (answerPayment == -1) break;
            }
        }

        

    }
}
