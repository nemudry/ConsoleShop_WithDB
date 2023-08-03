namespace TechClasses;

public static class Exceptions
{
    //информация по исключению
    public static void ShowExInfo (Exception ex)
    {
        Console.WriteLine(ex.Message);
        Console.WriteLine(ex.StackTrace);
        Console.WriteLine(ex.TargetSite);
        Console.WriteLine(ex.Source);
        Console.WriteLine(ex.InnerException?.Message);
    }
}
