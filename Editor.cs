using SkiaSharp;
using System;
using System.IO;

namespace WatermarkProject
{
    public class ImageProcessingException : Exception
    {
        public ImageProcessingException(string msg) : base(msg) { }
    }

    public class ImageEditor
    {
        public SKBitmap Bitmap { get; private set; }
        private SKCanvas _canvas;

        public ImageEditor(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Шлях до файлу не може бути порожнім.");

            Bitmap = SKBitmap.Decode(path);
            if (Bitmap == null)
                throw new ImageProcessingException("Формат файлу не підтримується або файл пошкоджено.");
                
            _canvas = new SKCanvas(Bitmap);
        }

        public void ConvertToGrayscale()
        {
            for (int y = 0; y < Bitmap.Height; y++)
            {
                for (int x = 0; x < Bitmap.Width; x++)
                {
                    var color = Bitmap.GetPixel(x, y);
                    byte gray = (byte)((color.Red * 0.21) + (color.Green * 0.72) + (color.Blue * 0.07));
                    
                    Bitmap.SetPixel(x, y, new SKColor(gray, gray, gray, color.Alpha));
                }
            }
        }

        public void ApplyWatermark(string watermarkPath)
        {
            using var originalWatermark = SKBitmap.Decode(watermarkPath);
            if (originalWatermark == null)
                throw new ImageProcessingException("Не вдалося прочитати файл водяного знака.");

            SKBitmap watermarkToUse = originalWatermark;
            bool wasResized = false;

            if (originalWatermark.Width > Bitmap.Width || originalWatermark.Height > Bitmap.Height)
            {
                float ratio = (float)(Bitmap.Width * 0.20) / originalWatermark.Width;
                
                int newWidth = (int)(originalWatermark.Width * ratio);
                int newHeight = (int)(originalWatermark.Height * ratio);

                if (newWidth < 1) newWidth = 1;
                if (newHeight < 1) newHeight = 1;

                var info = new SKImageInfo(newWidth, newHeight);
                watermarkToUse = originalWatermark.Resize(info, SKFilterQuality.High);
                wasResized = true;
            }

            using var paint = new SKPaint();
            
            for (int x = 0; x < Bitmap.Width; x += watermarkToUse.Width)
            {
                for (int y = 0; y < Bitmap.Height; y += watermarkToUse.Height)
                {
                    _canvas.DrawBitmap(watermarkToUse, x, y, paint);
                }
            }

            if (wasResized)
            {
                watermarkToUse.Dispose();
            }
        }

        public void Save(string outPath)
        {
            if (string.IsNullOrWhiteSpace(outPath))
                throw new ArgumentException("Шлях збереження не коректний.");

            using var image = SKImage.FromBitmap(Bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, 100);
            
            using var stream = File.OpenWrite(outPath);
            data.SaveTo(stream);
        }
    }
}