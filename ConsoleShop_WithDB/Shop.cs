using System.ComponentModel.DataAnnotations;
namespace ConsoleShop_WithDB;

public abstract class Shop
{
    //название
    public virtual string Name { get; protected set; } 
    //описание магазина
    public virtual string Description { get; protected set; }
    //продукты на продажу
    internal static Dictionary<Product, int> ProductsInShop { get; private set; }
    //место в магазине (для быстрого перехода)
    protected virtual placeStatus PlaceInShop { get; set; }
    internal protected enum placeStatus { ВходВМагазин, ПереходНаГлавныйЭкран, ВКорзину }    
    //клиент
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
            while (true)
            {
                Console.Clear();
                PlaceInShop = placeStatus.ВходВМагазин;
                //проверка на выход
                if (Account.PurchaseStatus == Account.purchaseStatus.ЗакончитьПокупку)
                {
                    Console.WriteLine("Всего доброго!");
                    return;
                }
                // выбрать желаемое начальное действие
                int answerWantToBuy = StartAction();
                switch (answerWantToBuy)
                {
                    case -1: Account.PurchaseStatus = Account.purchaseStatus.ЗакончитьПокупку; break; // выход из программы
                    case 1: StartPurchase(); break;// Начать покупку
                    case 2: GoToBusket(); break;// В Корзину
                    case 3: GoToAccount(); break;// Вход в аккаунт
                    default: break;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Возникла непредвиденная ошибка!");
            Exceptions.ShowExInfo(e);
        }
    }

    // выбрать желаемое начальное действие
    protected virtual int StartAction()
    {
        int answerWantToBuy = 0;
        //новая покупка
        if (Account.PurchaseStatus == Account.purchaseStatus.НоваяПокупка)
        {
            ShopInfo();// информация о магазине

            answerWantToBuy = ValidatorInput.GetChechedAnswer("Хотите начать покупку?",
                new string[] { "Начать покупку", "Перейти в корзину", "Войти в личный кабинет" }, "Выйти из магазина");
        }

        // Если в корзине уже добавлен товар, начальное меню меняется
        // (продолжить покупку, а не начать покупку - пока не отоваришь корзину)
        if (Account.PurchaseStatus == Account.purchaseStatus.ПродуктыВкорзине)
        {
            answerWantToBuy = ValidatorInput.GetChechedAnswer("Хотите продолжить покупку?",
                new string[] { "Продолжить покупку", "Перейти в корзину", "Войти в личный кабинет" }, "Выйти из магазина");
        }
        return answerWantToBuy;
    }

    //информация о магазине
    protected virtual void ShopInfo()
    {
        Color.Cyan($"Добро пожаловать в {Name}!");
        Color.Cyan($"{Description}");
        Console.WriteLine();
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
            int answerIntCategory = ValidatorInput.GetChechedAnswer("Выберите категорию товара.", categories.ToArray<string>());
            // получение выбранной пользователем категории по номеру
            string chosenCategory = categories[answerIntCategory - 1];
            // переход к выбору товара из выбранной категории
            SelectProduct(chosenCategory);
        }
    }

    // выбор товара из определенной категории
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
            int chosenProduct = ValidatorInput.GetChechedAnswer("Выберите дальнейшее действие.\nДля перехода на страничку товара введите его номер.",
                new string[productOfThisCategory.Count()], "Желаете вернуться к категориям", "Желаете вернуться на главный экран");

            // назад в категории
            if (chosenProduct == -1) break;
            // назад на главный экран
            else if (chosenProduct == -2)
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

    //вывод товаров выбранной категории на экран
    protected virtual void ShowProducts(List<Product> productOfThisCategory, string chosenCategory)
    {
        Color.Cyan($"Товары категории {chosenCategory}:");
        int numberOfProduct = 0;
        foreach (var product in productOfThisCategory)
        {
            Console.Write($"{++numberOfProduct}. {product.Name}, ");
            if (ProductsInShop[product] != 0)
            {
                Console.Write($"количество на складе: {ProductsInShop[product]} шт.");
                product.ShowDiscount(); // скидка
            }
            else Color.Red($"отсутствует в наличии.");
        }
        Console.WriteLine();
    }

    // добавление товара в корзину
    protected virtual void AddProductInBusket(Product product)
    {
        //добавить товар или уйти?
        int addToBusket = ValidatorInput.GetChechedAnswer("Выберите дальнейшее действие:",
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
            int amountOfChosenProduct = ValidatorInput.GetChechedAnswer($"Сколько штук товара \"{product.Name}\" вы хотите добавить в корзину? " +
                $"\nНа складе доступно \"{ProductsInShop[product]}\" шт.", new string[ProductsInShop[product]]);

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
            int answerInBusket = ValidatorInput.GetChechedAnswer("Выберите дальнейшее действие",
                new string[] { "Перейти к оплате", "Удалить товар из корзины" }, "Вернуться к покупкам");
            switch (answerInBusket)
            {
                case -1: return; // -1 вернуться к покупкам
                case 1: PayPayment().Wait(); break;//Перейти к оплате.
                case 2: DeleteProductFromBusket(); break;// // удалить товар из корзины
                default: break;
            }
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
            int answerIntRemoveProduct = ValidatorInput.GetChechedAnswer("Введите номер товара, который вы хотите удалить: ",
                new string[Account.Busket.ProductsInBusket.Count()], "Вернуться в корзину");
            //[-1]. Вернуться в корзину.
            if (answerIntRemoveProduct == -1) return;

            // определение удаляемого товара
            Product deleteProduct = Account.Busket.ProductsInBusket.ElementAt(answerIntRemoveProduct - 1).Key;
            // получение количества продукта на удаление
            int removeAmount = ValidatorInput.GetChechedAnswer("Введите количество товара, который вы хотите удалить: ",
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
                int answerInAccount = ValidatorInput.GetChechedAnswer("Выберите дальнейшее действие",
                new string[] { "Регистрация", "Авторизация" }, "Вернуться к покупкам");
                switch (answerInAccount)
                {
                    case -1: return false; // -1 вернуться к покупкам
                    case 1: await RegistrationAsync(); break; //регистрация
                    case 2: await AuthorizationAsync(); break;// авторизация
                    default: break;
                }
                break;
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
            string answerFullName = ValidatorInput.GetAnswerString("Введите имя: ", "Для возврата нажмите [-1]");
            if (answerFullName == "-1") break;//выход из регистрации
            //введите логин
            string answerLogin = ValidatorInput.GetAnswerString("Придумайте логин: ");
            //введите пароль
            string answerPassword = ValidatorInput.GetAnswerString("Придумайте пароль: ");

            //cоздание нового клиента и проверка самовалидацией
            var newClient = new Account(0, answerFullName, answerLogin, answerPassword);
            if (!ValidateAccount(newClient)) return;

            //Проверка на наличие данного клиента в бд        
            bool isHasClint = await DataBase.CheckClientDBAsync(newClient, false);
            //Если клиента нет в бд - регистрация нового клиента
            if (!isHasClint)
            {
                var result = await DataBase.SetNewClientDBAsync(newClient);
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

    //Валидация созданного аккаунта
    protected virtual bool ValidateAccount(Account newClient)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(newClient);
        if (!Validator.TryValidateObject(newClient, context, results, true))
        {
            foreach (var error in results)
                Color.Red(error.ErrorMessage);
            Feedback.AcceptPlayer();
            return false;
        }
        else
        {
            Color.Green("Пользователь прошел валидацию");
            Feedback.AcceptPlayer();
            return true;
        }
    }

    //авторизация
    protected virtual async Task AuthorizationAsync()
    {
        while (true)
        {
            Console.Clear();
            //введите логин
            string answerLogin = ValidatorInput.GetAnswerString("Введите логин: ", "Для возврата нажмите [-1]");
            if (answerLogin == "-1") break;//выход из авторизации
            //введите пароль
            string answerPassword = ValidatorInput.GetAnswerString("Введите пароль: ");

            //cоздание клиента и Проверка на наличие данного клиента в бд
            var сlient = new Account(0, null, answerLogin, answerPassword);
            bool isHasClint = await DataBase.CheckClientDBAsync(сlient, true);
            //Если клиент есть в бд - авторизация
            if (isHasClint)
            {
                Account = new Account(await DataBase.GetClientDBAsync(сlient), Account.PurchaseStatus, Account.Busket);
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

    // переход в аккаунт 
    protected virtual void GoToAccount()
    {
        if (!CheckAuthorizationAsync().Result) return; // проверка на авторизацию

        while (true)
        {
            Console.Clear();
            Color.Cyan("Вы находитесь в личном кабинете!");
            Color.Cyan($"ФИО: {Account.ClientFullName}");

            //выбор действия в корзине
            int answerInAccount = ValidatorInput.GetChechedAnswer("Выберите дальнейшее действие",
                new string[] { "Посмотреть историю заказов", "Деавторизоваться" }, "Вернуться к покупкам");
            switch (answerInAccount)
            {
                case -1: return; // -1 вернуться к покупкам
                case 1: Account.HistoryOrdersInfo(); break; //История заказов.
                case 2: Deauthorization(); return;// деавторизация
                default: break;
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

    // Оплата товара
    protected virtual async Task PayPayment()
    {
        //проверка авторизации
        if (!CheckAuthorizationAsync().Result) return;

        while (true)
        {
            Console.Clear();
            int answerPayment;
            // выберите способ оплаты
            answerPayment = ValidatorInput.GetChechedAnswer($"Стоимость всех товаров в корзине составляет {Account.Busket.TotalSum()}р." +
                $"\nВыберите способ оплаты: ", new string[] { "Оплата по карте" }, "Вернуться в корзину");

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
            if (product.Key.Id == id) return product.Key;
        return null;
    }
}
