using PdfLibCore;
using PdfLibCore.Enums;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using Tesseract;

class Program
{
    static void Main(string[] args)
    {
        var dpiX = 300D;
        var dpiY = 300D;
        var cropRect = new Rectangle(600, 600, 600, 600);

        using (var pdfDocument = new PdfDocument(File.Open("output.pdf", FileMode.Open)))
        {
            var page = pdfDocument.Pages[0];
            using (var pdfPage = page)
            {
                var pageWidth = (int)(dpiX * pdfPage.Size.Width / 72);
                var pageHeight = (int)(dpiY * pdfPage.Size.Height / 72);
                using var bitmap = new PdfiumBitmap(pageWidth, pageHeight, true);
                pdfPage.Render(bitmap, PageOrientations.Normal, RenderingFlags.LcdText);
                using (var stream = bitmap.AsBmpStream(dpiX, dpiY))
                {
                    using (var image = Image.Load<Rgba32>(stream))
                    {
                        image.SaveAsPng($"output.png");
                    }
                }
            }
        }

        using (Image<Rgba32> image = Image.Load<Rgba32>("output.png"))
        {
            image.Mutate(x => x.Crop(cropRect));

            image.Save("cropped.png");
        }

        FileStream fs = new FileStream($"cropped.png", FileMode.Open, FileAccess.Read);
        var ms = new MemoryStream();
        fs.CopyTo(ms);
        fs.Close();
        Byte[] fileBytes = ms.ToArray();
        ms.Close();
        using (var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default))
        {
            using (var img = Pix.LoadFromMemory(fileBytes))
            {
                using (var pageProc = engine.Process(img))
                {
                    var txt = pageProc.GetText();
                    System.Console.WriteLine(txt);
                }
            }
        }
    }
}
