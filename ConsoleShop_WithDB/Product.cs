
namespace ConsoleShop_WithDB
{
    internal class Product
    {
        internal int Id { get; }
        internal string Name { get; }
        internal string Category { get; }
        internal string Description { get; }
        internal string Made { get; }
        internal double Price { get; }

        public Product (int id, string name, string category, string description, string made, int price)
        {
            Id = id;
            Name = name;
            Category = category;
            Description = description;
            Made = made;
            Price = price;            
        }
    }
}
