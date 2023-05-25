
namespace ConsoleShop_WithDB
{
    internal class Product
    {
        internal int Id { get; }
        internal string Name { get; }
        internal string Category { get; }
        internal string Description { get; }
        internal string Made { get; }

        private double price;
        internal double Price
        {
            get => price;
            set
            {
                if (Discount != 0)
                {
                    price = value - value * (Discount / 100); // снижение цены на скидку
                }
                else price = value;
            }
        }
        internal double Discount { get; }       

        internal Product (int id, string name, string category, string description, string made, int price, int discount)
        {
            Id = id;
            Name = name;
            Category = category;
            Description = description;
            Made = made;
            Discount = discount;
            Price = price;
        }

        internal void ProductInfo ()
        {
            Color.Cyan("Характеристики выбранного товара:");
            Console.WriteLine($"{Description}");
            Console.WriteLine($"Cтрана-производитель - {Made}.");
            Color.GreenShort($"Цена - {Price}");
            if (Discount != 0) Color.Green($" - СКИДКА {Discount}% !!!.");
            else Console.WriteLine();
        }
    }
}
