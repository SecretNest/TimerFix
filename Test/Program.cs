using SecretNest.TimerFix;
using System;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            TimerFix x = new TimerFix(Job);
            x.Change(0, 1000);
            Console.Read();
        }

        static void Job(object stateInfo)
        {
            Console.WriteLine(
                DateTime.Now.ToString("HH:mm:ss.fff"));
        }
    }
}
