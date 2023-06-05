
namespace ConsoleShop_WithDB
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                Shop shop = new ShopNN();
                shop.StartShop();
            }
            catch (Exception e)
            {
                Console.WriteLine("Ошибка!");
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                Console.WriteLine(e.TargetSite);
                Console.WriteLine(e.Source);
                Console.WriteLine(e.InnerException?.Message);
            }
        }
    }
}