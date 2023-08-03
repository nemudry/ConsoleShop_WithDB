namespace ConsoleShop_WithDB;

//продукт
public class Product
{
    internal int Id { get; }
    //название
    internal string Name { get; }
    //категория товара
    internal string Category { get; }
    //описание
    internal string Description { get; }    
    //производитель
    internal string Made { get; }
    //цена
    private double price;
    internal double Price
    {
        get => price;
        set
        {
            if (Discount != 0)
                price = value - value * (Discount / 100); // снижение цены на скидку
            else price = value;
        }
    }
    //скидка
    internal double Discount { get; }

    public Product (int id, string name, string category, string description, string made, int price, int discount)
    {
        Id = id;
        Name = name;
        Category = category;
        Description = description;
        Made = made;
        Discount = discount;
        Price = price;
    }

    //показать информацию о товаре
    internal void ProductInfo ()
    {
        Color.Cyan("Характеристики выбранного товара:");
        Console.WriteLine($"{Name}");
        Console.WriteLine($"{Description}");
        Console.WriteLine($"Cтрана-производитель - {Made}.");
        Color.GreenShort($"Цена - {Price}"); 
        ShowDiscount();
    }

    //показать скидку
    internal void ShowDiscount()
    {
        if (Discount != 0) Color.Green($" - СКИДКА {Discount}% !!!.");
        else Console.WriteLine();
    }
}
