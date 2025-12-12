using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using SkiaSharp;

namespace WatermarkProject
{
    class Program
    {
        static ImageEditor editor = null;
        static string watermarkPath = null;
        static List<OperationLog> logs = new();
        
        static HistoryManager<OperationLog> historyManager = new("history.json");

        static List<Option> options = new()
        {
            new Option("Вибрати зображення (JPG/PNG)", SetImage),
            new Option("Вибрати водяний знак (тільки PNG)", SetWatermark),
            new Option("Перетворити у відтінки сірого", ConvertToGrayscale),
            new Option("Накласти водяний знак", ApplyWatermark),
            new Option("Зберегти зображення", SaveImage),
            new Option("Переглянути історію дій", ShowHistory),
            new Option("Вийти", ExitAndSave)
        };

        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.CursorVisible = false;

            logs = historyManager.Load();

            int selected = 0;
            while (true)
            {
                ShowMenu(selected);
                var keyInfo = Console.ReadKey(true);
                var key = keyInfo.Key;

                if (key == ConsoleKey.DownArrow && selected < options.Count - 1)
                    selected++;
                else if (key == ConsoleKey.UpArrow && selected > 0)
                    selected--;
                else if (key == ConsoleKey.Enter)
                {
                    try
                    {
                        options[selected].Action.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"\n[Критична помилка]: {ex.Message}");
                        Console.ReadLine();
                    }
                }
            }
        }

        static void ShowMenu(int selected)
        {
            Console.Clear();
            Console.WriteLine("=== Watermark Console Editor v2.0 ===\n");
            for (int i = 0; i < options.Count; i++)
            {
                Console.ForegroundColor = i == selected ? ConsoleColor.Green : ConsoleColor.Gray;
                Console.Write(i == selected ? "> " : "  ");
                Console.WriteLine(options[i].Name);
            }
            Console.ResetColor();
        }

        static void SetImage()
        {
            Console.Clear();
            Console.Write("Введіть шлях до основного зображення: ");
            var path = Console.ReadLine()?.Trim('"');

            if (!FileValidator.IsValidImage(path))
            {
                Console.WriteLine("Помилка: Файл не існує або має некоректний формат (потрібен JPG/PNG).");
                Wait();
                return;
            }

            try
            {
                editor = new ImageEditor(path);
                logs.Add(new OperationLog($"Завантажено зображення: {Path.GetFileName(path)}", DateTime.Now));
                Console.WriteLine("Зображення успішно завантажено!");
            }
            catch (ImageProcessingException ex)
            {
                Console.WriteLine($"[Помилка обробки]: {ex.Message}");
            }
            Wait();
        }

        static void SetWatermark()
        {
            Console.Clear();
            Console.Write("Введіть шлях до водяного знака (PNG): ");
            var path = Console.ReadLine()?.Trim('"');

            if (!FileValidator.IsPng(path))
            {
                Console.WriteLine("Помилка: Водяний знак має бути у форматі .PNG!");
                Wait();
                return;
            }

            watermarkPath = path;
            logs.Add(new OperationLog($"Обрано watermark: {Path.GetFileName(path)}", DateTime.Now));
            Console.WriteLine("Водяний знак успішно встановлено!");
            Wait();
        }

        static void ConvertToGrayscale()
        {
            Console.Clear();
            if (editor == null)
            {
                Console.WriteLine("Спочатку завантажте зображення!");
                Wait();
                return;
            }

            editor.ConvertToGrayscale();
            logs.Add(new OperationLog("Застосовано фільтр: Grayscale", DateTime.Now));
            Console.WriteLine("Зображення перетворено у відтінки сірого.");
            Wait();
        }

        static void ApplyWatermark()
        {
            Console.Clear();
            if (editor == null)
            {
                Console.WriteLine("Спочатку завантажте зображення!");
                Wait();
                return;
            }
            if (string.IsNullOrEmpty(watermarkPath))
            {
                Console.WriteLine("Спочатку виберіть файл водяного знаку!");
                Wait();
                return;
            }

            try
            {
                editor.ApplyWatermark(watermarkPath);
                logs.Add(new OperationLog("Накладено watermark патерном", DateTime.Now));
                Console.WriteLine("Водяний знак накладено!");
            }
            catch (ImageProcessingException ex)
            {
                Console.WriteLine($"[Помилка]: {ex.Message}");
            }
            Wait();
        }

        static void SaveImage()
        {
            Console.Clear();
            if (editor == null)
            {
                Console.WriteLine("Немає чого зберігати!");
                Wait();
                return;
            }

            Console.Write("Введіть шлях для збереження (наприклад, result.jpg): ");
            var outputPath = Console.ReadLine()?.Trim('"');

            try
            {
                editor.Save(outputPath);
                logs.Add(new OperationLog($"Збережено файл: {outputPath}", DateTime.Now));
                Console.WriteLine("Успішно збережено!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Помилка запису]: {ex.Message}");
            }
            Wait();
        }

        static void ShowHistory()
        {
            Console.Clear();
            if (logs.Count == 0)
            {
                Console.WriteLine("Історія порожня.");
            }
            else
            {
                Console.WriteLine("--- Історія операцій (Session + Saved) ---");
                foreach (var l in logs)
                    Console.WriteLine($"[{l.Time:HH:mm:ss}] {l.Operation}");
            }
            Wait();
        }

        static void ExitAndSave()
        {
            historyManager.Save(logs);
            Console.WriteLine("Історію збережено. Програма завершує роботу...");
            Environment.Exit(0);
        }

        static void Wait()
        {
            Console.WriteLine("\nНатисніть Enter для продовження...");
            Console.ReadLine();
        }
    }

    public class Option
    {
        public string Name { get; }
        public Action Action { get; }
        public Option(string name, Action action) { Name = name; Action = action; }
    }

    public record OperationLog(string Operation, DateTime Time);

    public static class FileValidator
    {
        public static bool IsValidImage(string path)
        {
            if (!File.Exists(path)) return false;
            var ext = Path.GetExtension(path).ToLower();
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp";
        }

        public static bool IsPng(string path)
        {
            if (!File.Exists(path)) return false;
            return Path.GetExtension(path).ToLower() == ".png";
        }
    }

    public class HistoryManager<T>
    {
        private readonly string _filePath;

        public HistoryManager(string filePath)
        {
            _filePath = filePath;
        }

        public void Save(List<T> data)
        {
            try
            {
                string jsonString = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, jsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Не вдалося зберегти історію: {ex.Message}");
            }
        }

        public List<T> Load()
        {
            if (!File.Exists(_filePath)) return new List<T>();
            
            try
            {
                string jsonString = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<List<T>>(jsonString) ?? new List<T>();
            }
            catch
            {
                return new List<T>();
            }
        }
    }
}