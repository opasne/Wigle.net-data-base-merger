using System;

namespace WigleDBMerger
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length >= 2 && args[args.Length - 1].Contains("fout:"))
            {
                try
                {
                    using (Tools.SqLite sqlite = new Tools.SqLite(args))
                    {
                        double totalSeconds = 0;

                        DateTime timeStart = DateTime.Now;
                        sqlite.FillDicts();
                        TimeSpan span = DateTime.Now - timeStart;
                        totalSeconds += span.TotalSeconds;

                        Console.Clear();
                        Console.WriteLine();
                        Console.WriteLine("-----------------------Загружено из {0} файлов суммарно:------------------------", args.Length);
                        Console.WriteLine("Сетей: {0,68} шт", sqlite.loadedNets);
                        Console.WriteLine("Локаций: {0,66} шт", sqlite.loadedLocations);
                        Console.WriteLine("------------------------------------------------------------------------------");

                        timeStart = DateTime.Now;
                        sqlite.SortDicts();
                        span = DateTime.Now - timeStart;
                        totalSeconds += span.TotalSeconds;

                        Console.WriteLine("Сортировка сетей по точности координат и уровню сигнала заняла {0,11:0.00} сек", span.TotalSeconds);
                        Console.WriteLine("------------------------------------------------------------------------------");

                        timeStart = DateTime.Now;
                        sqlite.GenerateListOfNetworksViaBestCoordinates();
                        span = DateTime.Now - timeStart;
                        totalSeconds += span.TotalSeconds;

                        Console.WriteLine("Посчитано уникальных Сетей: {0,47} шт", sqlite.outNets);
                        Console.WriteLine("Объединено локаций: {0,55} шт", sqlite.outLocations);
                        Console.WriteLine("Затрачено времени: {0,55:0.00} сек", span.TotalSeconds);
                        Console.WriteLine("------------------------------------------------------------------------------");

                        timeStart = DateTime.Now;
                        sqlite.UploadDictsToFile(args[args.Length - 1].Split(':')[1]);
                        span = DateTime.Now - timeStart;
                        totalSeconds += span.TotalSeconds;
                        sqlite.UploadOpenNetsKML();
                        Console.WriteLine();
                        Console.WriteLine("Генерация нового файла заняла: {0,43:0.00} сек", span.TotalSeconds);

                        Console.WriteLine("------------------------------------------------------------------------------");
                        Console.WriteLine("Итого обработка данных заняла: {0,43:0.00} сек", totalSeconds);
                        Console.WriteLine();
                        Console.WriteLine("Для продолжения нажмите любую клавишу...");
                    };
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Произошла ошибка:\r\n{0}\r\n\r\n{1}", ex.Message, ex.StackTrace);
                }
                Console.ReadKey();
            }
            else if (args.Length == 2)
            {
                if (args[1].Contains("/kml"))
                {
                    using (Tools.SqLite sqlite = new Tools.SqLite(args))
                    {
                        sqlite.FillDicts();
                        sqlite.SortDicts();
                        sqlite.GenerateListOfNetworksViaBestCoordinates();
                        sqlite.UploadOpenNetsKML();
                    }
                }
            }
            else
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("Соединить файлы в один и сгенерировать kml:");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("WigleDBSplitter file1 file2 <file3> <fileN> fout:fineoutname.sqlite");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("Сгенерировать kml:");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("WigleDBSplitter file1 /kml\r\n");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("Для продолжения нажмите любую клавишу...");
                Console.ReadKey();
            }

        }




    }
}
