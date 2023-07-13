namespace TechClasses;
public class Converter
{
    //Конвертация данный в инт или строку
    public static void ParseData(object obj, out int intObj, out string strObj)
    {
        switch (obj)
        {
            case Int64:
                intObj = (int)(Int64)obj; strObj = null; break;
            case Int32:
                intObj = (int)obj; strObj = null; break;
            case string:
                intObj = 0; strObj = (string)obj; break;
            default:
                intObj = 0; strObj = null; break;
        }
    }
}
