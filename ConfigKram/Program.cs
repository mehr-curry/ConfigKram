using System;
using System.Configuration;
using Configuration;

namespace ConfigKram
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var test = new TestObject();
            var configuration = new ReflectionConfiguration("ConfigKram.config");
            configuration.Load(test);
            test.TestInt++;
            test.TestNullableInt = null;
            configuration.Save(test);
            Console.Read();
        }
    }
}