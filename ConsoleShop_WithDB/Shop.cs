
namespace ConsoleShop_WithDB
{
    internal abstract class Shop
    {
        protected virtual string Name { get; }
        protected virtual string Description { get; }
        protected virtual Dictionary<Product, int> ProductsInShop { get; }
        internal placeStatus PlaceInShop { get; set; }
        internal enum placeStatus
        {
            ВходВМагазин,
            ПереходНаГлавныйЭкран,
            ВКорзину
        }
        protected virtual Client Client { get; set; }


        //Запуск магазина
        public virtual void StartShop()
        {
            int answerWantToBuy = 0;
            while (true)
            {
                Console.Clear();
                PlaceInShop = placeStatus.ВходВМагазин;

                if (Client.PurchaseStatus == Client.purchaseStatus.НоваяПокупка)
                {
                    Color.Cyan($"Добро пожаловать в {Name}!");
                    Color.Cyan($"{Description}");
                    Console.WriteLine();
                    while (true)
                    {
                        Console.WriteLine("Хотите начать покупку?");
                        Console.WriteLine("[1] Да. \n[-1] Нет.");
                        answerWantToBuy = Feedback.PlayerAnswer();

                        if (Feedback.CheckСonditions(answerWantToBuy, 1, 1, -1)) break;
                    }
                }

                // Если в корзине уже добавлен товар, начальное меню меняется
                // (продолжить покупку, а не начать покупку - пока не отоваришь корзину)
                if (Client.PurchaseStatus == Client.purchaseStatus.ПродуктыВкорзине)
                {
                    while (true)
                    {
                        Color.Cyan("Хотите продолжить покупку?");
                        Console.WriteLine("[1]. Продолжить покупки. \n[2]. Перейти в корзину. " +
                            "\n[-1]. Выйти из магазина.");
                        answerWantToBuy = Feedback.PlayerAnswer();

                        if (Feedback.CheckСonditions(answerWantToBuy, 2, 1, -1)) break;
                    }
                }

                //Начать покупку
                if (answerWantToBuy == 1)
                {
                    StartPurchase();
                }

                // В Корзину
                if (answerWantToBuy == 2)
                {
                    GoToBusket();
                }

                // выход из программы
                if (answerWantToBuy == -1 || Client.PurchaseStatus == Client.purchaseStatus.ЗакончитьПокупку)
                {
                    Console.WriteLine("Всего доброго!");
                    break;
                }
            } 
        }

        //Начать покупку
        protected virtual void StartPurchase ()
        {
            while (true)
            {
                // выход к начальному меню
                if (PlaceInShop == placeStatus.ПереходНаГлавныйЭкран) break;

                Console.Clear();

                // доступные категории
                var categories = ProductsInShop.Select(e => e.Key.Category).Distinct().ToList();

                // если товаров вообще нет
                if (categories.Count() == 0)
                {
                    Color.Red("Все товары закончились. Вернитесь позже.");
                    Console.WriteLine();
                    if (Client.PurchaseStatus != Client.purchaseStatus.ПродуктыВкорзине)
                        Client.PurchaseStatus = Client.purchaseStatus.ЗакончитьПокупку;

                    Console.WriteLine($"Нажмите клавишу для продолжения.");
                    Console.ReadKey();
                    break;
                }

                Color.Cyan("Выберите категорию товара.");
                foreach (var category in categories)
                {
                    Console.WriteLine($"[{categories.IndexOf(category) + 1}]. {category}");
                }
                Console.WriteLine();

                //выбор категории пользователем
                int answerIntCategory;
                while (true)
                {
                    Color.Cyan("Введите категорию.");
                    answerIntCategory = Feedback.PlayerAnswer();

                    if (Feedback.CheckСonditions(answerIntCategory, categories.Count, 1)) break;
                }

                // получение выбранной пользователем категории по номеру
                string chosenCategory = categories[answerIntCategory - 1];

                // переход к выбору товара из выбранной категории
                SelectProduct(chosenCategory);
            }        
        }

        // выбор товара из выбранной категории
        protected void SelectProduct(string chosenCategory)
        {
            while (true)
            {
                if (PlaceInShop == placeStatus.ПереходНаГлавныйЭкран) break;

                Console.Clear();

                // получение доступных продуктов выбранной категории
                var productOfThisCategory = ProductsInShop.Where(e => e.Key.Category == chosenCategory).Select(e => e.Key).ToList();

                //вывод товаров выбранной категории на экран
                Color.Cyan($"Товары категории {chosenCategory}:");
                int numberOfProduct = 0;
                foreach (var product in productOfThisCategory)
                {
                    numberOfProduct++;
                    Console.Write($"{numberOfProduct}. {product.Name}, ");
                    if (ProductsInShop[product] != 0) Console.WriteLine($"количество на складе: {ProductsInShop[product]} шт.");
                    else Color.Red($"отсутствует в наличии.");

                    // показ скидки
                    /*
                    if (product is IDiscountable discProd)
                    {
                        Console.Write($" - ");
                        Color.GreenShort($"СКИДКА {discProd.Discount}% !!!");
                    }
                    Console.WriteLine();
                    */
                }
                Console.WriteLine();

                //выбор продукта для добавления в корзину
                int chosenProduct = 0;
                while (true)
                {
                    Color.Cyan("Выберите дальнейшее действие.");
                    Console.WriteLine("Для перехода на страничку товара введите его номер.");
                    Console.WriteLine("Желаете вернуться к категориям - нажимите \"-1\".");
                    Console.WriteLine("Желаете вернуться на главный экран - нажимите \"-2\".");
                    chosenProduct = Feedback.PlayerAnswer();

                    if (Feedback.CheckСonditions(chosenProduct, productOfThisCategory.Count, 1, -1, -2)) break;
                };

                // назад в категории
                if (chosenProduct == -1) break;

                // назад на главный экран
                if (chosenProduct == -2)
                {
                    PlaceInShop = placeStatus.ПереходНаГлавныйЭкран;
                    break;
                }
                //определение товара
                else
                {
                    int numberOfProduct2 = 1;
                    foreach (var product in productOfThisCategory)
                    {
                        if (numberOfProduct2 == chosenProduct)
                        {
                            Product selectedProduct = product;
                            //описание товара и добавление его в корзину
                            AddProductInBusket(selectedProduct);
                        }
                        numberOfProduct2++;
                    }
                }
                
            };
        }

        //описание товара и добавление его в корзину
        protected void AddProductInBusket(Product product)
        {
            Console.Clear();

            // показ карточки товара
            product.ProductInfo();
            if (ProductsInShop[product] != 0) Color.Green($"Количество на складе: {ProductsInShop[product]} шт.");
            else
            {
                Color.Red($"Отсутствует в наличии. Вернитесь позже.");

                Console.WriteLine($"Нажмите клавишу для продолжения.");
                Console.ReadKey();
                return;
            }
            Console.WriteLine();

            int addToBusket;
            while (true)
            {
                Color.Cyan("Выберите дальнейшее действие:");
                Console.WriteLine($"1 - Добавить товар в корзину. \n2 - Вернуться к выбору товара.");
                addToBusket = Feedback.PlayerAnswer();

                if (Feedback.CheckСonditions(addToBusket, 2, 1)) break;
            }

            // добавление товара в корзину
            if (addToBusket == 1)
            {
                int amountOfChosenProduct;
                //покупка "оптом"
                while (true)
                {   
                    Color.Cyan($"Сколько штук товара \"{product.Name}\" вы хотите добавить в корзину?");
                    Color.Cyan($"На складе доступно \"{ProductsInShop[product]}\" шт.");
                    amountOfChosenProduct = Feedback.PlayerAnswer();

                    if (Feedback.CheckСonditions(amountOfChosenProduct, ProductsInShop[product], 1)) break;
                }

                // добавление в корзину
                // если данный товар уже есть в корзине - добавить к нему количества, иначе добавить новый товар
                if (Client.Busket.ProductsInBusket.ContainsKey(product)) Client.Busket.ProductsInBusket[product] += amountOfChosenProduct;
                else Client.Busket.ProductsInBusket.Add(product, amountOfChosenProduct);

                ProductsInShop[product] -= amountOfChosenProduct; // уменьшить количество товара в магазине
                Client.PurchaseStatus = purchaseStatus.ПродуктыВкорзине;
                PlaceInShop = placeStatus.ПереходНаГлавныйЭкран;

                Color.Green($"{amountOfChosenProduct} шт товара \"{product.Name}\" добавлено в корзину.");
                Console.WriteLine($"Стоимость всех товаров в корзине составляет {Client.Busket.TotalSum()}р.");
                Console.WriteLine();

                Console.WriteLine($"Нажмите клавишу для продолжения.");
                Console.ReadKey();
            }
            // 2 - Вернуться к выбору товара.
            else return;
        }

        // переход в корзину 
        protected void GoToBusket()
        {
            while (true)
            {                
                // если покупка совершена - выход в начальный цикл                
                if (PlaceInShop == placeStatus.ПереходНаГлавныйЭкран) break;

                // если в корзине нет товаров - выход в начальный цикл
                if (!Client.Busket.ProductsInBusket.Any())
                {
                    Color.Red("Корзина пуста! Для оформления покупки сперва добавьте товаров корзину.");
                    break;
                }

                Color.Cyan("Вы находитесь в корзине!");
                // информация о продуктах в корзине
                Client.Busket.BusketInfo();

                //выбор действия в корзине
                int answerInBusket;
                while (true)
                {
                    Color.Cyan("Выберите дальнейшее действие:");
                    Console.WriteLine("[1]. Перейти к оплате. \n[2]. Удалить товар из корзины. " +
                        "\n[-1]. Вернуться к покупкам. ");
                    answerInBusket = Feedback.PlayerAnswer();

                    if (Feedback.CheckСonditions(answerInBusket, 2, 1, -1)) break;
                }

                //Перейти к оплате.
                if (answerInBusket == 1)
                {
                    Client.PayPayment();
                    if (!Client.Busket.ProductsInBusket.Any()) PlaceInShop = placeStatus.ПереходНаГлавныйЭкран;
                }

                // удалить товар из корзины
                else if (answerInBusket == 2)
                {
                    DeleteProductFromBusket();
                }
                // -1 вернуться к покупкам
                else break;
            }
        }

        //удаление товара из корзины
        protected void DeleteProductFromBusket()
        {
            while (true)
            {
                // информация о продуктах в корзине
                Client.Busket.BusketInfo();

                //выбор товара на удаление из корзины
                int answerIntRemoveProduct;
                while (true)
                {
                    Color.Cyan("Введите номер товара, который вы хотите удалить: ");
                    Color.Cyan("[-1]. Вернуться в корзину.");
                    answerIntRemoveProduct = Feedback.PlayerAnswer();

                    if (Feedback.CheckСonditions(answerIntRemoveProduct, Client.Busket.ProductsInBusket.Count(), 1)) break;

                    //[-1]. Вернуться в корзину.");
                    if (answerIntRemoveProduct == -1) return;
                }

                // получение списка продуктов, определение удаляемого товара
                List<Product> productsList = new List<Product>();
                foreach (var product in Client.Busket.ProductsInBusket)
                {
                    productsList.Add(product.Key);
                }
                Product deleteProduct = productsList[answerIntRemoveProduct - 1];

                // получение количества продукта на удаление
                int removeAmount;
                while (true)
                {
                    Color.Cyan("Введите количество товара, который вы хотите удалить: ");
                    removeAmount = Feedback.PlayerAnswer();

                    if (Feedback.CheckСonditions(removeAmount, Client.Busket.ProductsInBusket[deleteProduct], 1)) break;                 
                }

                //удаление товара
                // если удаляется не все количество товара в корзине - уменьшить количество, иначе удалить товар полностью
                if (Client.Busket.ProductsInBusket[deleteProduct] > removeAmount) Client.Busket.ProductsInBusket[deleteProduct] -= removeAmount;
                else Client.Busket.ProductsInBusket.Remove(deleteProduct);

                ProductsInShop[deleteProduct] += removeAmount;// добавление товара на полки магазина

                Color.Green($"{removeAmount} шт. товара \"{deleteProduct.Name}\" удалено из корзины.");
                Console.WriteLine();

                // если из корзины удалены все товары
                if (Client.Busket.ProductsInBusket.Count() == 0)
                {
                    Client.PurchaseStatus = purchaseStatus.НоваяПокупка;
                    PlaceInShop = placeStatus.ПереходНаГлавныйЭкран;
                    break;
                }                
            }
        }

        //преобразование данных бд в дикшинери ProductsInShop
        protected virtual void GetProductsFromDB (DataSet data)
        {
            foreach (DataRow row in data.Tables[0].Rows)
            {
                var cells = row.ItemArray;

                //проверка на нуль, и получение данных
                long id = cells[0] != null ? (Int64)cells[0] : 0;
                string name = cells[1] != null ? (string)cells[1] : "Неопределено";
                string category = cells[2] != null ? (string)cells[2] : "Неопределено";
                string description = cells[3] != null ? (string)cells[3] : "Неопределено";
                string made = cells[4] != null ? (string)cells[4] : "Неопределено";
                long price = cells[5] != null ? (Int64)cells[5] : 0;
                long count = cells[6] != null ? (Int64)cells[6] : 0;

                Product product = new Product((int)id, name, category, description, made, (int) price);
                ProductsInShop.Add(product, (int) count);
            }
        }

    }
}
