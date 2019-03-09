using System;
using System.IO;
using Tesseract;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System.Threading;
using System.Drawing;

namespace OcrLib
{
    public class OcrService
    {
        private readonly string _destination;

        public OcrService(string dest = null)
        {
            this._destination = dest;
        }

        public void Scan(string fullPath, bool force = false)
        {

            var file = System.IO.Path.GetFileNameWithoutExtension(fullPath);
            var dstPath = _destination ?? System.IO.Path.GetDirectoryName(fullPath);
            var dstFile = System.IO.Path.Combine(dstPath, $"{file}.ocr");

            if (!File.Exists(dstFile + ".pdf") || force)
            {
                Console.WriteLine($"Scanning {file}");
                using (var pdfReader = new PdfReader(fullPath))
                {
                    var parser = new PdfReaderContentParser(pdfReader);
                    var extractor = new ImageExtractor(dstPath);
                    var dllDir = AppDomain.CurrentDomain.BaseDirectory;
                    bool containsText = false;
                    using (var engine = new TesseractEngine($"{dllDir}/tessdata", "deu", EngineMode.Default))
                    {
                        using (var pdf = ResultRenderer.CreatePdfRenderer(dstFile, @"./tessdata"))
                        {
                            pdf.BeginDocument(file);
                            for (int i = 1; i <= pdfReader.NumberOfPages; i++)
                            {
                                parser.ProcessContent(i, extractor);
                                var tempFile = extractor.TempFile;
                                containsText |= extractor.ContainsText;
                                if (containsText)
                                {
                                    break;      // Don't process files that contain text.
                                }
                                using (var img = Pix.LoadFromFile(tempFile))
                                {
                                    Console.WriteLine($"Scanning page {i}");
                                    using (var page = engine.Process(img, $"page-{i}"))
                                    {
                                        pdf.AddPage(page);
                                    }
                                }
                                File.Delete(tempFile);
                            }
                        }
                    }

                    // Don't duplicate files that contain text.
                    if (containsText)
                    {
                        Console.WriteLine($"Skipping {file}, as it contains text.");
                        File.Delete(dstFile+".pdf");
                    }
                }
            }
        }

        public void Commit(string fullPath)
        {
            if(fullPath.EndsWith(".ocr.pdf"))
            {
                var filename = System.IO.Path.GetFileName(fullPath);
                filename = $"{filename.Substring(0, filename.Length - ".ocr.pdf".Length)}.pdf";     // get rid of ".ocr"
                var dst = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(fullPath), filename);
                File.Copy(fullPath, dst, true);
                File.Delete(fullPath);
            }
        }
    }

    class ImageExtractor : IRenderListener
    {
        private readonly string _destination;

        public string TempFile { get; private set; }
        public Image TempImage { get; set; }
        public bool ContainsText { get;  private set; }

        public ImageExtractor(string destination)
        {
            _destination = destination;
        }

        public void BeginTextBlock()
        {
        }

        public void EndTextBlock()
        {
        }

        public void RenderImage(ImageRenderInfo ri)
        {
            var imageObject = ri.GetImage();
            var data = imageObject.GetImageAsBytes();
            TempImage = imageObject.GetDrawingImage();
            var fileType = imageObject.GetFileType();
            TempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"temp.{fileType}");
            File.WriteAllBytes(TempFile, data);
        }

        public void RenderText(TextRenderInfo renderInfo)
        {
            ContainsText = renderInfo.GetText().Length > 0;
        }
    }

}
