using System;

namespace Pong
{
    class Program
    {
        static void Main(string[] args)
        {
            const int fieldLength = 50, fieldWidth = 15;
            const char fieldTile = '#';
            string line = string.Concat(Enumerable.Repeat(fieldTile, fieldLength));

            const int racketLength = fieldWidth / 4;
            const char racketTile = '|';

            while (true)
            {
                Console.SetCursorPosition(0, 0);
                Console.WriteLine(line);

                Console.SetCursorPosition(0, fieldWidth);
                Console.WriteLine(line);

                for (int i = 0; i < racketLength; i++) 
                {
                    Console.SetCursorPosition(0, i + 1);
                }
            }
        }
    }
}