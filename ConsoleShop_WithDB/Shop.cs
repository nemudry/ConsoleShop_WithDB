
using System.Drawing;

namespace ConsoleShop_WithDB
{
    internal abstract class Shop
    {
        protected virtual string Name { get; }
        protected virtual string Description { get; }
        protected virtual Dictionary<Product, int> ProductsInShop { get; }

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
            int numberOfProduct = 0;
            foreach (var product in ProductsInShop)
            {
                Console.WriteLine($"[{++numberOfProduct}]. {product.Key.Name}, цена {product.Key.Price}р, " +
                    $"количество на складах: {product.Value} шт.");
            }
            Console.WriteLine();
        }

        //преобразование данных бд 
        protected virtual void GetProductsFromDB (DataSet data)
        {
            foreach (DataRow row in data.Tables[0].Rows)
            {
                var cells = row.ItemArray;

                //проверка на нуль, и получение данных
                long id = cells[0] != null ? (Int64)cells[0] : 0;
                string name = cells[1] != null ? (string)cells[1] : "Неопределено";
                string category = cells[2] != null ? (string)cells[2] : "Неопределено";
                string description = cells[3] != null ? (string)cells[3] : "Неопределено";
                string made = cells[4] != null ? (string)cells[4] : "Неопределено";
                long price = cells[5] != null ? (Int64)cells[5] : 0;
                long count = cells[6] != null ? (Int64)cells[6] : 0;

                Product product = new Product((int)id, name, category, description, made, (int) price);
                ProductsInShop.Add(product, (int) count);
            }
        }
    }
}
