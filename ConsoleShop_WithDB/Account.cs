using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ConsoleShop_WithDB;

//клиент
public class Account: IValidatableObject
{
    //id клиента
    internal int ClientId { get; }
    //имя клиента
    internal string ClientFullName { get; }
    //логин
    internal string ClientLogin { get; }
    //пароль
    internal string ClientPassword { get; }
    //корзина
    internal Busket Busket { get; }
    //зкаказы
    protected virtual List<Order> Orders { get; }
    //статус покупки
    internal purchaseStatus PurchaseStatus { get; set; }
    internal enum purchaseStatus { НоваяПокупка, ПродуктыВкорзине, ЗакончитьПокупку }
    //статус аккаунта
    internal clientStatus ClientStatus { get; set; }
    internal enum clientStatus { Авторизован, Аноним }

    //основной конструктор для работы в приложении
    internal Account(int id, string fullName, string login, string password, purchaseStatus purchaseStatus, clientStatus clientStatus, List<Order> orders, Busket busket)
    {
        ClientId = id;
        ClientFullName = fullName;
        ClientLogin = login;
        ClientPassword = password;
        Busket = busket;
        Orders = orders;
        PurchaseStatus = purchaseStatus;
        ClientStatus = clientStatus;
    }

    //гостевой аккаунт
    internal Account()
     : this(0, "Гость", null, null, purchaseStatus.НоваяПокупка, clientStatus.Аноним, null, new Busket())
    { }

    //деавторизация
    internal Account(purchaseStatus purchaseStatus, Busket busket)
        : this(0, "Гость", null, null, purchaseStatus, clientStatus.Аноним, null, busket)
    { }

    //авторизация
    internal Account(Account account, purchaseStatus purchaseStatus, Busket busket)
        : this(account.ClientId, account.ClientFullName, account.ClientLogin, account.ClientPassword, purchaseStatus, clientStatus.Авторизован, DataBase.GetOrdersDBAsync(account.ClientId).Result, busket)
    { }

    //валидация аккаунта, загрузка аккаунта из бд
    internal Account(int id, string fullName, string login, string password)
        : this(id, fullName, login, password, purchaseStatus.НоваяПокупка, clientStatus.Аноним, null, null)
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
        Feedback.AcceptPlayer();
    }

    //самовалидация модели
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        List<ValidationResult> errors = new List<ValidationResult>();

        if (string.IsNullOrWhiteSpace(ClientFullName))
            errors.Add(new ValidationResult("Не указано имя."));
        if (ClientFullName.Length < 3 || ClientFullName.Length > 20)
            errors.Add(new ValidationResult("Некорректная длина имени."));
        Regex regex = new Regex(@"[\d]");
        if (regex.IsMatch(ClientFullName))
            errors.Add(new ValidationResult("В имени не могут присутствовать цифры."));

        if (string.IsNullOrWhiteSpace(ClientLogin))
            errors.Add(new ValidationResult("Не указан логин."));
        if (ClientLogin.Length < 3 || ClientLogin.Length > 20)
            errors.Add(new ValidationResult("Некорректная длина логина."));

        if (string.IsNullOrWhiteSpace(ClientPassword))
            errors.Add(new ValidationResult("Не указан пароль."));
        if (ClientPassword.Length < 3 || ClientPassword.Length > 20)
            errors.Add(new ValidationResult("Некорректная длина пароля."));

        return errors;
    }
}
