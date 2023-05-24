global using System.Collections;
global using ConsoleShop_WithDB;
global using Microsoft.Data.Sqlite;
global using System.Data.SQLite;
global using System.Data;

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
        }
    }
}