namespace ConsoleShop_WithDB;
public static class Program
{
    public static void Main(string[] args)
    {
        Shop shop = new ShopNN();
        shop.StartShop();
    }
}