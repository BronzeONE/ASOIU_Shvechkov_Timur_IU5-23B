using System;

class Program
{
    static int Distance(string s1, string s2)
    {
        int m = s1.Length;
        int n = s2.Length;

        int[,] d = new int[m + 1, n + 1];

        for (int i = 0; i <= m; i++)
            d[i, 0] = i;

        for (int j = 0; j <= n; j++)
            d[0, j] = j;

        for (int i = 1; i <= m; i++)
        {
            for (int j = 1; j <= n; j++)
            {
                int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;

                int ins = d[i, j - 1] + 1;
                int del = d[i - 1, j] + 1;
                int rep = d[i - 1, j - 1] + cost;

                d[i, j] = Math.Min(Math.Min(ins, del), rep);

                if (i > 1 && j > 1 &&
                    s1[i - 1] == s2[j - 2] &&
                    s1[i - 2] == s2[j - 1])
                {
                    d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + cost);
                }
            }
        }

        return d[m, n];
    }

    static void Main()
    {
        while (true)
        {
            Console.Write("Введите первую строку: ");
            string s1 = Console.ReadLine();

            if (s1 == "exit")
                break;

            Console.Write("Введите вторую строку: ");
            string s2 = Console.ReadLine();

            int result = Distance(s1, s2);

            Console.WriteLine("Расстояние: " + result);
            Console.WriteLine();
        }
    }
}