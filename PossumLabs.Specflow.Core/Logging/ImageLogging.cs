using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace PossumLabs.Specflow.Core.Logging
{
    public class ImageLogging
    {
        public ImageLogging(double sizePercentage=.05, string color = "DarkOrange" )
        {
            FontPercentage = sizePercentage;
            var properties = typeof(Brushes).GetProperties();
            if (properties.Any(p => p.Name == "DarkOrange"))
                Brush = (Brush)properties.First(p => p.Name == "DarkOrange").GetValue(null);
            else
                throw new GherkinException($"The Brush color of '{color}' is invalid, please use one of these {properties.LogFormat(p => p.Name)}");
            if (sizePercentage < 0 || sizePercentage > 1)
                throw new GherkinException($"The sizePercentage of {sizePercentage} is invalid, please provide a value between 0 and 1");
        }

        private double FontPercentage { get; }
        private Brush Brush { get; }

        public byte[] AddTextToImage(byte[] image, string text)
        {
            var msIn = new MemoryStream(image);

            var img = Image.FromStream(msIn);

            using (Graphics graphics = Graphics.FromImage(img))
            {
                var pixelSize = Convert.ToSingle(img.Size.Height * FontPercentage);
                using (Font arialFont = new Font(new FontFamily("Arial"), pixelSize, GraphicsUnit.Pixel))
                {
                    graphics.DrawString(text, arialFont, Brush, graphics.RenderingOrigin);
                }
            }
            var msOut = new MemoryStream();
            img.Save(msOut, img.RawFormat);
            return msOut.ToArray();
        }
    }
}
