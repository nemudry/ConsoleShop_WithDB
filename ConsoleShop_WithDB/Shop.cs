namespace ConsoleShop_WithDB;
internal abstract class Shop
{
    protected virtual string Name { get; }
    protected virtual string Description { get; }
    protected static Dictionary<Product, int> ProductsInShop { get; private set; }
    protected placeStatus PlaceInShop { get; set; }
    protected enum placeStatus { ВходВМагазин, ПереходНаГлавныйЭкран, ВКорзину }
    protected virtual Account Account { get; set; }

    internal Shop()
    {
        Name = "";
        Description = "";
        PlaceInShop = placeStatus.ВходВМагазин;
        Account = new Account();
        ProductsInShop = DataBase.LoadDBAsync().Result; 
    }

    //Запуск магазина
    public virtual void StartShop()
    {
        try
        {
            int answerWantToBuy = 0;
            while (true)
            {
                Console.Clear();
                PlaceInShop = placeStatus.ВходВМагазин;

                if (Account.PurchaseStatus == Account.purchaseStatus.НоваяПокупка)
                {
                    Color.Cyan($"Добро пожаловать в {Name}!");
                    Color.Cyan($"{Description}");
                    Console.WriteLine();

                    answerWantToBuy = Validator.GetChechedAnswer("Хотите начать покупку?",
                        new string[] { "Начать покупку", "Перейти в корзину", "Войти в личный кабинет" }, "Выйти из магазина");
                }

                // Если в корзине уже добавлен товар, начальное меню меняется
                // (продолжить покупку, а не начать покупку - пока не отоваришь корзину)
                if (Account.PurchaseStatus == Account.purchaseStatus.ПродуктыВкорзине)
                {
                    answerWantToBuy = Validator.GetChechedAnswer("Хотите продолжить покупку?",
                        new string[] { "Продолжить покупку", "Перейти в корзину", "Войти в личный кабинет" }, "Выйти из магазина");
                }

                //Начать покупку
                if (answerWantToBuy == 1) StartPurchase();

                // В Корзину
                if (answerWantToBuy == 2) GoToBusket();

                // Вход в аккаунт
                if (answerWantToBuy == 3) GoToAccount();

                // выход из программы
                if (answerWantToBuy == -1 || Account.PurchaseStatus == Account.purchaseStatus.ЗакончитьПокупку)
                {
                    Console.WriteLine("Всего доброго!");
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Возникла непредвиденная ошибка!");
            Exceptions.ShowExInfo(e);
        }
    }

    //Начать покупку, выбор категории
    protected virtual void StartPurchase()
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
                if (Account.PurchaseStatus != Account.purchaseStatus.ПродуктыВкорзине)
                    Account.PurchaseStatus = Account.purchaseStatus.ЗакончитьПокупку;

                Feedback.AcceptPlayer();
                break;
            }

            //выбор категории пользователем
            int answerIntCategory = Validator.GetChechedAnswer("Выберите категорию товара.", categories.ToArray<string>());

            // получение выбранной пользователем категории по номеру
            string chosenCategory = categories[answerIntCategory - 1];

            // переход к выбору товара из выбранной категории
            SelectProduct(chosenCategory);
        }
    }

    //вывод товаров выбранной категории на экран
    protected virtual void ShowProducts(List<Product> productOfThisCategory, string chosenCategory)
    {
        Color.Cyan($"Товары категории {chosenCategory}:");
        int numberOfProduct = 0;
        foreach (var product in productOfThisCategory)
        {
            numberOfProduct++;
            Console.Write($"{numberOfProduct}. {product.Name}, ");
            if (ProductsInShop[product] != 0)
            {
                Console.Write($"количество на складе: {ProductsInShop[product]} шт.");
                product.ShowDiscount();
            }
            else Color.Red($"отсутствует в наличии.");
        }
        Console.WriteLine();
    }

    // выбор товара из выбранной категории
    protected virtual void SelectProduct(string chosenCategory)
    {
        while (true)
        {
            if (PlaceInShop == placeStatus.ПереходНаГлавныйЭкран) break;

            Console.Clear();

            // получение доступных продуктов выбранной категории
            var productOfThisCategory = ProductsInShop.Where(e => e.Key.Category == chosenCategory).Select(e => e.Key).ToList();

            //вывод товаров выбранной категории на экран
            ShowProducts(productOfThisCategory, chosenCategory);

            //выбор продукта для добавления в корзину            
            int chosenProduct = Validator.GetChechedAnswer("Выберите дальнейшее действие.\nДля перехода на страничку товара введите его номер.",
                new string[productOfThisCategory.Count()], "Желаете вернуться к категориям", "Желаете вернуться на главный экран");

            // назад в категории
            if (chosenProduct == -1) break;

            // назад на главный экран
            if (chosenProduct == -2)
            {
                PlaceInShop = placeStatus.ПереходНаГлавныйЭкран;
                break;
            }

            //определение товара    
            Product selectedProduct = productOfThisCategory[chosenProduct - 1];

            // показ карточки товара
            Console.Clear();
            selectedProduct.ProductInfo();

            // добавление товара в корзину
            AddProductInBusket(selectedProduct);
        }
    }

    // добавление товара в корзину
    protected virtual void AddProductInBusket(Product product)
    {
        //добавить товар или уйти?
        int addToBusket = Validator.GetChechedAnswer("Выберите дальнейшее действие:",
            new string[] { "Добавить товар в корзину" }, "Вернуться к выбору товара");

        // добавление товара в корзину
        if (addToBusket == 1)
        {
            if (ProductsInShop[product] == 0)
            {
                Color.Red($"Товар временно отсутствует в наличии. Вернитесь позже.");
                Feedback.AcceptPlayer();
                return;
            }

            //покупка "оптом", определение товара в корзину
            int amountOfChosenProduct = Validator.GetChechedAnswer($"Сколько штук товара \"{product.Name}\" вы хотите добавить в корзину? " +
                $"\nНа складе доступно \"{ProductsInShop[product]}\" шт.",
                new string[ProductsInShop[product]]);

            // если данный товар уже есть в корзине - добавить к нему количества, иначе добавить новый товар
            if (Account.Busket.ProductsInBusket.ContainsKey(product))
                Account.Busket.ProductsInBusket[product] += amountOfChosenProduct;
            else Account.Busket.ProductsInBusket.Add(product, amountOfChosenProduct);

            ProductsInShop[product] -= amountOfChosenProduct; // уменьшить количество товара в магазине
            Account.PurchaseStatus = Account.purchaseStatus.ПродуктыВкорзине;
            PlaceInShop = placeStatus.ПереходНаГлавныйЭкран;

            Color.Green($"{amountOfChosenProduct} шт товара \"{product.Name}\" добавлено в корзину.");
            Console.WriteLine($"Стоимость всех товаров в корзине составляет {Account.Busket.TotalSum()}р.");
            Feedback.AcceptPlayer();
        }
        // 2 - Вернуться к выбору товара.
        else return;
    }

    // переход в корзину 
    protected virtual void GoToBusket()
    {
        while (true)
        {
            // если покупка совершена - выход в начальный цикл                
            if (PlaceInShop == placeStatus.ПереходНаГлавныйЭкран) break;

            // если в корзине нет товаров - выход в начальный цикл
            if (!Account.Busket.ProductsInBusket.Any())
            {
                Color.Red("Корзина пуста! Для оформления покупки сперва добавьте товаров корзину.");
                Feedback.AcceptPlayer();
                break;
            }

            Color.Cyan("Вы находитесь в корзине!");
            // информация о продуктах в корзине
            Account.Busket.BusketInfo();

            //выбор действия в корзине
            int answerInBusket = Validator.GetChechedAnswer("Выберите дальнейшее действие",
                new string[] { "Перейти к оплате", "Удалить товар из корзины" }, "Вернуться к покупкам");

            //Перейти к оплате.
            if (answerInBusket == 1) PayPayment().Wait();
            // удалить товар из корзины
            else if (answerInBusket == 2) DeleteProductFromBusket();
            else break; // -1 вернуться к покупкам
        }
    }

    //удаление товара из корзины
    protected virtual void DeleteProductFromBusket()
    {
        while (true)
        {
            // информация о продуктах в корзине
            Account.Busket.BusketInfo();

            //выбор товара на удаление из корзины
            int answerIntRemoveProduct = Validator.GetChechedAnswer("Введите номер товара, который вы хотите удалить: ",
                new string[Account.Busket.ProductsInBusket.Count()], "Вернуться в корзину");

            //[-1]. Вернуться в корзину.
            if (answerIntRemoveProduct == -1) return;

            // определение удаляемого товара
            Product deleteProduct = Account.Busket.ProductsInBusket.ElementAt(answerIntRemoveProduct - 1).Key;

            // получение количества продукта на удаление
            int removeAmount = Validator.GetChechedAnswer("Введите количество товара, который вы хотите удалить: ",
                new string[Account.Busket.ProductsInBusket[deleteProduct]]);

            //удаление товара
            // если удаляется не все количество товара в корзине - уменьшить количество, иначе удалить товар полностью
            if (Account.Busket.ProductsInBusket[deleteProduct] > removeAmount) Account.Busket.ProductsInBusket[deleteProduct] -= removeAmount;
            else Account.Busket.ProductsInBusket.Remove(deleteProduct);

            ProductsInShop[deleteProduct] += removeAmount;// возврат товара на полки магазина

            Color.Green($"{removeAmount} шт. товара \"{deleteProduct.Name}\" удалено из корзины.");
            Console.WriteLine();

            // если из корзины удалены все товары
            if (Account.Busket.ProductsInBusket.Count() == 0)
            {
                Account.PurchaseStatus = Account.purchaseStatus.НоваяПокупка;
                PlaceInShop = placeStatus.ПереходНаГлавныйЭкран;
                break;
            }
        }
    }

    //проверка на авторизацию
    protected virtual async Task<bool> CheckAuthorizationAsync()
    {
        while (true)
        {
            if (Account.ClientStatus == Account.clientStatus.Аноним)
            {
                Color.Red("Вы не авторизованы!");
                Console.WriteLine();

                //выбор действия в аккаунте
                int answerInAccount = Validator.GetChechedAnswer("Выберите дальнейшее действие",
                new string[] { "Регистрация", "Авторизация" }, "Вернуться к покупкам");

                //регистрация
                if (answerInAccount == 1)
                {
                    await RegistrationAsync();
                    break;
                }
                // авторизация
                else if (answerInAccount == 2) await AuthorizationAsync();
                else return false; // -1 вернуться к покупкам
            }
            else return true;
        }
        return false;
    }

    //регистрация
    protected virtual async Task RegistrationAsync()
    {
        while (true)
        {
            Console.Clear();

            //введите ФИО
            string answerFullName = Validator.GetChechedAnswerString("Введите имя: ",
                new Regex(@"[а-яА-Яa-zA-Z]{3,15}", RegexOptions.IgnoreCase), "Для возврата нажмите [-1]");

            //выход из регистрации
            if (answerFullName == "-1") break;

            //введите логин
            string answerLogin = Validator.GetChechedAnswerString("Придумайте логин: ", new Regex(@"\w{5,15}"));

            //введите пароль
            string answerPassword = Validator.GetChechedAnswerString("Придумайте пароль: ", new Regex(@"\w{8,20}")); ;

            //Проверка на наличие данного клиента в бд        
            bool isHasClint = await DataBase.CheckClientDBAsync(answerLogin);

            //Если клиента нет в бд - регистрация нового клиента
            if (!isHasClint)
            {
                var result = await DataBase.SetNewClientDBAsync(answerFullName, answerLogin, answerPassword);
                if (result) Color.Green("Регистрация прошла успешно!");
                else Color.Red("Регистрация не прошла!");
                Feedback.AcceptPlayer();
                break;
            }
            //Если логин/пароль заняты
            else
            {
                Color.Red("Введенный логин занят!");
                Feedback.AcceptPlayer();
            }
        }
    }

    //авторизация
    protected virtual async Task AuthorizationAsync()
    {
        while (true)
        {
            Console.Clear();

            //введите логин
            string answerLogin = Validator.GetChechedAnswerString("Введите логин: ", new Regex(@"\w{5,15}"), "Для возврата нажмите [-1]");

            //выход из авторизации
            if (answerLogin == "-1") break;

            //введите пароль
            string answerPassword = Validator.GetChechedAnswerString("Введите пароль: ", new Regex(@"\w{8,20}"));

            //Проверка на наличие данного клиента в бд
            bool isHasClint = await DataBase.CheckClientDBAsync(answerLogin, answerPassword);

            //Если клиент есть в бд - авторизация
            if (isHasClint)
            {
                var client = await DataBase.GetClientDBAsync(answerLogin, answerPassword);
                Account = new Account(client.id, client.name, Account.PurchaseStatus, Account.Busket);
                Color.Green("Авторизация прошла успешно!");
                Feedback.AcceptPlayer();
                break;
            }
            //Если логин/пароль не подходят
            else
            {
                Color.Red("Введенные логин/пароль не подходят!");
                Feedback.AcceptPlayer();
            }
        }
    }

    //деавторизация
    protected virtual void Deauthorization()
    {
        Account = new Account(Account.PurchaseStatus, Account.Busket);

        Color.Green("Выход из аккаунта произведен успешно!");
        Feedback.AcceptPlayer();
    }

    // переход в аккаунт 
    protected virtual void GoToAccount()
    {
        if (!CheckAuthorizationAsync().Result) return;

        Console.Clear();

        while (true)
        {
            Color.Cyan("Вы находитесь в личном кабинете!");
            Color.Cyan($"ФИО: {Account.ClientFullName}");

            //выбор действия в корзине
            int answerInAccount = Validator.GetChechedAnswer("Выберите дальнейшее действие",
                new string[] { "Посмотреть историю заказов", "Деавторизоваться" }, "Вернуться к покупкам");

            //История заказов.
            if (answerInAccount == 1) Account.HistoryOrdersInfo();
            // деавторизация
            else if (answerInAccount == 2)
            {
                Deauthorization();
                break;
            }
            else break; // -1 вернуться к покупкам
        }
    }

    // Оплата товара
    protected virtual async Task PayPayment()
    {
        //проверка авторизации
        if (!CheckAuthorizationAsync().Result) return;

        int answerPayment;
        while (true)
        {
            Console.Clear();

            // выберите способ оплаты
            answerPayment = Validator.GetChechedAnswer($"Стоимость всех товаров в корзине составляет {Account.Busket.TotalSum()}р." +
                $"\nВыберите способ оплаты: ",
                new string[] { "Оплата по карте" }, "Вернуться в корзину");

            // Оплата по карте.
            if (answerPayment == 1)
            {
                //формирование заказа в бд
                await DataBase.SetOrderDBAsync(DateTime.Now, Account, Account.Busket.ProductsInBusket);

                //покупка товаров(уменьшение товара на складах)
                await DataBase.SetBuyProductsDBAsync(Account.Busket.ProductsInBusket);

                Color.Green($"Денежные средства в размере {Account.Busket.TotalSum()}р списаны с Вашей банковской карты. Благодарим за покупку!");
                Feedback.AcceptPlayer();

                // очистить корзину
                Account.Busket.ProductsInBusket.Clear();

                Account.PurchaseStatus = Account.purchaseStatus.НоваяПокупка;
                PlaceInShop = placeStatus.ПереходНаГлавныйЭкран;
                break;
            }
            else break; // Вернуться в корзину.
        }
    }

    //получение товара по id
    internal static Product GetProductById(int id)
    {
        foreach (var product in ProductsInShop)
        {
            if (product.Key.Id == id) return product.Key;
        }
        return null;
    }
}
