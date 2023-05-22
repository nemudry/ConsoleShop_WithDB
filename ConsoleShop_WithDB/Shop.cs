
using System.Drawing;
using System.Security.Policy;

namespace ConsoleShop_WithDB
{
    internal abstract class Shop
    {
        protected virtual string Name { get; }
        protected virtual string Description { get; }
        protected virtual DataSet Data { get;}

        //Запуск магазина
        public virtual void StartShop()
        {
            ShowProductInShop();

            Console.Read();
        }


        //Показать продукты
        protected virtual void ShowProductInShop()
        {
            Console.WriteLine($"Название продукта - цена");
            for (int i = 0; i < Data.Tables[0].Rows.Count; ++i)
            {
                Console.WriteLine($"№[{i + 1}]. {Data.Tables[0].Rows[i][0]} - {Data.Tables[0].Rows[i][4]} р.");                
            }
            Console.WriteLine();
        }
    }
}
