using System;
using System.IO;
using System.Linq;

namespace TestSubmit
{
    class Program
    {
        static void Main(string[] args)
        {
            using var input = new StreamReader(args[0]);

            var numbers = Enumerable.Empty<int>();
            while (numbers.Count() < 2)
            {
                numbers = numbers.Concat(input.ReadLine().Split(' ', StringSplitOptions.RemoveEmptyEntries).Select((str) => int.Parse(str)));
            }
            Console.WriteLine(numbers.ElementAt(0) + numbers.ElementAt(1));
        }
    }
}
