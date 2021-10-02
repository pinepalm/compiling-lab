using System;
using System.Linq;

namespace TestSubmit
{
    class Program
    {
        static void Main(string[] args)
        {
            var numbers = Enumerable.Empty<int>();

            while (numbers.Count() < 2)
            {
                numbers = numbers.Concat(Console.ReadLine().Split(' ', StringSplitOptions.RemoveEmptyEntries).Select((str) => int.Parse(str)));
            }

            Console.WriteLine(numbers.ElementAt(0) + numbers.ElementAt(1));
        }
    }
}
