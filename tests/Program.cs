using System;

namespace Yepi.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            UtilTests.CleanAppVerTest();
            UtilTests.ProgramVersionGreaterTests();
            Console.WriteLine("Tests are finished");
        }
    }
}