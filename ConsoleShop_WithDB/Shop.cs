
using System.Numerics;

namespace ConsoleShop_WithDB
{
    internal abstract class Shop
    {
        protected virtual string Name { get; }
        protected virtual string Description { get; }
        protected static Dictionary<Product, int> ProductsInShop { get; } = DataBase.LoadDB();
        internal placeStatus PlaceInShop { get; set; }
        internal enum placeStatus
        {
            ВходВМагазин,
            ПереходНаГлавныйЭкран,
            ВКорзину
        }
        protected virtual Account Account { get; set; }

        //Запуск магазина
        public virtual void StartShop()
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
                    while (true)
                    {
                        Console.WriteLine("Хотите начать покупку?");
                        Console.WriteLine("[1] Да. \n[-1] Нет. \n[2]. Перейти в корзину. ");
                        Console.WriteLine("[3] Войти в личный кабинет.");
                        answerWantToBuy = Feedback.PlayerAnswer();

                        if (Feedback.CheckСonditions(answerWantToBuy, 3, 1, -1)) break;
                    }
                }

                // Если в корзине уже добавлен товар, начальное меню меняется
                // (продолжить покупку, а не начать покупку - пока не отоваришь корзину)
                if (Account.PurchaseStatus == Account.purchaseStatus.ПродуктыВкорзине)
                {
                    while (true)
                    {
                        Color.Cyan("Хотите продолжить покупку?");
                        Console.WriteLine("[1]. Продолжить покупки. \n[2]. Перейти в корзину. \n[3] Войти в личный кабинет." +
                            "\n[-1]. Выйти из магазина.");
                        answerWantToBuy = Feedback.PlayerAnswer();

                        if (Feedback.CheckСonditions(answerWantToBuy, 3, 1, -1)) break;
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

                // Вход в аккаунт
                if (answerWantToBuy == 3)
                {
                   if(CheckAuthorization())
                   {
                        GoToAccount();
                   }
                }

                // выход из программы
                if (answerWantToBuy == -1 || Account.PurchaseStatus == Account.purchaseStatus.ЗакончитьПокупку)
                {
                    Console.WriteLine("Всего доброго!");
                    break;
                }
            }
        }

        //Начать покупку
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
                    Console.WriteLine();
                    if (Account.PurchaseStatus != Account.purchaseStatus.ПродуктыВкорзине)
                        Account.PurchaseStatus = Account.purchaseStatus.ЗакончитьПокупку;

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
                if (Account.Busket.ProductsInBusket.ContainsKey(product)) Account.Busket.ProductsInBusket[product] += amountOfChosenProduct;
                else Account.Busket.ProductsInBusket.Add(product, amountOfChosenProduct);

                ProductsInShop[product] -= amountOfChosenProduct; // уменьшить количество товара в магазине
                Account.PurchaseStatus = Account.purchaseStatus.ПродуктыВкорзине;
                PlaceInShop = placeStatus.ПереходНаГлавныйЭкран;

                Color.Green($"{amountOfChosenProduct} шт товара \"{product.Name}\" добавлено в корзину.");
                Console.WriteLine($"Стоимость всех товаров в корзине составляет {Account.Busket.TotalSum()}р.");
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
                if (!Account.Busket.ProductsInBusket.Any())
                {
                    Color.Red("Корзина пуста! Для оформления покупки сперва добавьте товаров корзину.");

                    Console.WriteLine($"Нажмите клавишу для продолжения.");
                    Console.ReadKey();
                    break;
                }

                Color.Cyan("Вы находитесь в корзине!");
                // информация о продуктах в корзине
                Account.Busket.BusketInfo();

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
                    if (CheckAuthorization())
                    {
                        Account.PayPayment();
                    }
                    if (!Account.Busket.ProductsInBusket.Any()) PlaceInShop = placeStatus.ПереходНаГлавныйЭкран;
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
                Account.Busket.BusketInfo();

                //выбор товара на удаление из корзины
                int answerIntRemoveProduct;
                while (true)
                {
                    Color.Cyan("Введите номер товара, который вы хотите удалить: ");
                    Color.Cyan("[-1]. Вернуться в корзину.");
                    answerIntRemoveProduct = Feedback.PlayerAnswer();

                    if (Feedback.CheckСonditions(answerIntRemoveProduct, Account.Busket.ProductsInBusket.Count(), 1)) break;

                    //[-1]. Вернуться в корзину.");
                    if (answerIntRemoveProduct == -1) return;
                }

                // получение списка продуктов, определение удаляемого товара
                List<Product> productsList = new List<Product>();
                foreach (var product in Account.Busket.ProductsInBusket)
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

                    if (Feedback.CheckСonditions(removeAmount, Account.Busket.ProductsInBusket[deleteProduct], 1)) break;
                }

                //удаление товара
                // если удаляется не все количество товара в корзине - уменьшить количество, иначе удалить товар полностью
                if (Account.Busket.ProductsInBusket[deleteProduct] > removeAmount) Account.Busket.ProductsInBusket[deleteProduct] -= removeAmount;
                else Account.Busket.ProductsInBusket.Remove(deleteProduct);

                ProductsInShop[deleteProduct] += removeAmount;// добавление товара на полки магазина

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
        protected virtual bool CheckAuthorization()
        {
            //если НЕавторизован
            while (true)
            {
                if (Account.ClientStatus == Account.clientStatus.Аноним)
                {
                    Color.Red("Вы не авторизованы!");
                    Console.WriteLine();

                    //выбор действия в аккаунте
                    int answerInAccount;
                    while (true)
                    {
                        Color.Cyan("Выберите дальнейшее действие:");
                        Console.WriteLine("[1]. Регистрация. \n[2]. Авторизация. " +
                            "\n[-1]. Вернуться к покупкам. ");
                        answerInAccount = Feedback.PlayerAnswer();

                        if (Feedback.CheckСonditions(answerInAccount, 2, 1, -1)) break;
                    }

                    //регистрация
                    if (answerInAccount == 1)
                    {
                        Registration();
                        break;
                    }
                    // авторизация
                    else if (answerInAccount == 2)
                    {
                        Authorization();         
                    }
                    // -1 вернуться к покупкам
                    else return false;
                }
                else return true;
            }
            return false;
        }

        //регистрация
        protected virtual void Registration()
        {
            while (true)
            {
                Console.Clear();

                //введите ФИО
                string answerFullName;
                while (true)
                {
                    Color.Cyan("Введите ФИО: ");
                    Color.Cyan("Для возврата к покупкам нажмите [-1]: ");
                    answerFullName = Feedback.PlayerAnswerString();

                    if (Feedback.CheckСonditionsString(answerFullName)) break;
                }

                //выход из регистрации
                if (answerFullName == "-1") break;

                //введите логин
                string answerLogin;
                while (true)
                {
                    Color.Cyan("Придумайте логин: ");
                    answerLogin = Feedback.PlayerAnswerString();

                    if (Feedback.CheckСonditionsString(answerLogin)) break;
                }

                //введите пароль
                string answerPassword;
                while (true)
                {
                    Color.Cyan("Придумайте пароль: ");
                    answerPassword = Feedback.PlayerAnswerString();

                    if (Feedback.CheckСonditionsString(answerPassword)) break;
                }

                //Проверка на наличие данного клиента в бд
                int isHasClint;
                using (SqliteConnection connection = new SqliteConnection(DataBase.connectionString))
                {
                    connection.Open();
                    SqliteCommand command = new SqliteCommand();
                    command.Connection = connection;
                                        
                    //Проверка на наличие данного клиента в бд
                    command.CommandText = "SELECT Count(*) " +
                        "FROM Clients " +
                        "WHERE Clients.Login = @login;";

                    SqliteParameter loginParam = new SqliteParameter("@login", answerLogin);
                    command.Parameters.Add(loginParam);

                    isHasClint = (int)(Int64)command.ExecuteScalar();
                }

                //Если клиента нет в бд - регистрация нового клиента
                if (isHasClint == 0)
                {
                    using (SqliteConnection connection = new SqliteConnection(DataBase.connectionString))
                    {
                        connection.Open();
                        SqliteCommand command = new SqliteCommand();
                        command.Connection = connection;

                        //Внести клиента в бд
                        command.CommandText = "INSERT INTO Clients (FullName, Login, ClientPassword) VALUES " +
                            "(@fullname, @login, @password);";

                        SqliteParameter fullnameParam = new SqliteParameter("@fullname", answerFullName);
                        SqliteParameter loginParam = new SqliteParameter("@login", answerLogin);
                        SqliteParameter passwordParam = new SqliteParameter("@password", answerPassword);
                        command.Parameters.Add(fullnameParam);
                        command.Parameters.Add(loginParam);
                        command.Parameters.Add(passwordParam);

                        command.ExecuteNonQuery();

                        Color.Green("Регистрация прошла успешно!");
                        Console.WriteLine();

                        Console.WriteLine($"Нажмите клавишу для продолжения.");
                        Console.ReadKey();
                        break;
                    }
                }
                //Если логин/пароль заняты
                else
                {
                    Color.Red("Введенный логин занят!");
                    Console.WriteLine();

                    Console.WriteLine($"Нажмите клавишу для продолжения.");
                    Console.ReadKey();
                }
            }
        }

        //авторизация
        protected virtual void Authorization()
        {
            while (true)
            {
                Console.Clear();

                //введите логин
                string answerLogin;
                while (true)
                {
                    Color.Cyan("Введите логин: ");
                    Color.Cyan("Для возврата к покупкам нажмите [-1]: ");
                    answerLogin = Feedback.PlayerAnswerString();

                    if (Feedback.CheckСonditionsString(answerLogin)) break;
                }

                //выход из авторизации
                if (answerLogin == "-1") break;

                //введите пароль
                string answerPassword;
                while (true)
                {
                    Color.Cyan("Введите пароль: ");
                    answerPassword = Feedback.PlayerAnswerString();

                    if (Feedback.CheckСonditionsString(answerPassword)) break;
                }

                //Проверка на наличие данного клиента в бд
                int isHasClint;
                using (SqliteConnection connection = new SqliteConnection(DataBase.connectionString))
                {
                    connection.Open();
                    SqliteCommand command = new SqliteCommand();
                    command.Connection = connection;

                    //Проверка на наличие данного клиента в бд по логину/паролю
                    command.CommandText = "SELECT Count(*) " +
                        "FROM Clients " +
                        "WHERE Clients.Login = @login AND Clients.ClientPassword = @password;";

                    SqliteParameter loginParam = new SqliteParameter("@login", answerLogin);
                    SqliteParameter passwordParam = new SqliteParameter("@password", answerPassword);
                    command.Parameters.Add(loginParam);
                    command.Parameters.Add(passwordParam);

                    isHasClint = (int)(Int64)command.ExecuteScalar();
                }

                //Если клиент есть в бд - авторизация
                if (isHasClint == 1)
                {
                    using (SqliteConnection connection = new SqliteConnection(DataBase.connectionString))
                    {

                        connection.Open();
                        SqliteCommand command = new SqliteCommand();
                        command.Connection = connection;

                        //Получение id клиента
                        command.CommandText = "SELECT Clients.Id, Clients.FullName " +
                            "FROM Clients " +
                            "WHERE Clients.Login = @login AND Clients.ClientPassword = @password ";

                        SqliteParameter loginParam = new SqliteParameter("@login", answerLogin);
                        SqliteParameter passwordParam = new SqliteParameter("@password", answerPassword);
                        command.Parameters.Add(loginParam);
                        command.Parameters.Add(passwordParam);

                        SqliteDataReader reader = command.ExecuteReader();

                        if (reader.HasRows)
                        {
                            //авторизация
                            while (reader.Read())
                            {
                                int id = (int)(Int64)reader.GetValue(0);
                                string name = (string)reader.GetValue(1);
                                Account = new Account(id, name, (Busket)Account.Busket.Clone(), Account.PurchaseStatus);
                            }
                        }

                        Color.Green("Авторизация прошла успешно!");
                        Console.WriteLine();

                        Console.WriteLine($"Нажмите клавишу для продолжения.");
                        Console.ReadKey();
                        break;
                    }
                }
                //Если логин/пароль не подходят
                else
                {
                    Color.Red("Введенные логин/пароль не подходят!");
                    Console.WriteLine();

                    Console.WriteLine($"Нажмите клавишу для продолжения.");
                    Console.ReadKey();
                }
            }
        }

        //деавторизация
        protected virtual void Deauthorization()
        {
            Account = new Account((Busket)Account.Busket.Clone(), Account.PurchaseStatus);

            Color.Green("Выход из аккаунта произведен успешно!");
            Console.WriteLine();

            Console.WriteLine($"Нажмите клавишу для продолжения.");
            Console.ReadKey();
        }

        // переход в аккаунт 
        protected void GoToAccount()
        {
            Console.Clear();

            while (true)
            {
                Color.Cyan("Вы находитесь в личном кабинете!");
                Color.Cyan($"ФИО: {Account.ClientFullName}");

                //выбор действия в корзине
                int answerInAccount;
                while (true)
                {
                    Color.Cyan("Выберите дальнейшее действие:");
                    Console.WriteLine("[1]. Посмотреть историю заказов. \n[2]. Деавторизоваться. " +
                        "\n[-1]. Вернуться к покупкам. ");
                    answerInAccount = Feedback.PlayerAnswer();

                    if (Feedback.CheckСonditions(answerInAccount, 2, 1, -1)) break;
                }

                //История заказов.
                if (answerInAccount == 1)
                {
                    Account.HistoryPurchaseInfo();
                }
                // деавторизация
                else if (answerInAccount == 2)
                {
                    Deauthorization();
                    break;
                }
                // -1 вернуться к покупкам
                else break;
            }
        }

        //получение товара по id
        internal static Product GetProductById (int id)
        {
            foreach (var product in ProductsInShop)
            {
                if (product.Key.Id == id) return product.Key;
            }
            return null;
        }

    }
}
