using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;

namespace BLL.Services
{
    public class ResizeImage
    {
        /// <summary>
        /// Returns a thumbnail size of the given image. 
        /// </summary>
        /// <param name="initialImage"></param>
        /// <returns></returns>
        public static byte[] GetThumbnail(byte[] initialImage)
        {
            using (var ms = new MemoryStream(initialImage))
            {
                var image = Image.FromStream(ms);

                var ratioX = (double)150 / image.Width;
                var ratioY = (double)50 / image.Height;
                var ratio = Math.Min(ratioX, ratioY);

                var width = (int)(image.Width * ratio);
                var height = (int)(image.Height * ratio);

                var newImage = new Bitmap(width, height);
                Graphics.FromImage(newImage).DrawImage(image, 0, 0, width, height);
                Bitmap bmp = new Bitmap(newImage);

                ImageConverter converter = new ImageConverter();

                return (byte[])converter.ConvertTo(bmp, typeof(byte[]));
            }
        }

        /// <summary>
        /// Returns a smaller version of the given image. Resizes it and keeps the ratio. 
        /// </summary>
        /// <param name="initialImage"></param>
        /// <returns></returns>
        public static byte[] Get160by120(byte[] initialImage)
        {
            using (var ms = new MemoryStream(initialImage))
            {
                var image = Image.FromStream(ms);

                var ratioX = (double)120 / image.Width;
                var ratioY = (double)160 / image.Height;
                var ratio = Math.Min(ratioX, ratioY);

                var width = (int)(image.Width * ratio);
                var height = (int)(image.Height * ratio);

                var newImage = new Bitmap(width, height);
                Graphics.FromImage(newImage).DrawImage(image, 0, 0, width, height);
                Bitmap bmp = new Bitmap(newImage);

                ImageConverter converter = new ImageConverter();

                return (byte[])converter.ConvertTo(bmp, typeof(byte[]));
            }
        }
    }
}