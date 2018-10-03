using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BLL.Core.Domain;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace BLL.Extensions
{
    public static class ImageExtensions
    {
        private static string _camera_green = /*"data:image/png;base64," +*/ "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";
        private static string _camera_black = /*"data:image/png;base64," + */"iVBORw0KGgoAAAANSUhEUgAAACIAAAAiCAQAAACQTsNJAAAABGdBTUEAALGPC/xhBQAAACBjSFJNAAB6JgAAgIQAAPoAAACA6AAAdTAAAOpgAAA6mAAAF3CculE8AAAAAmJLR0QA/4ePzL8AAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAHdElNRQffBwcBIAp02Ed4AAAD+0lEQVRIx82VbWiVZRjHf9f9POd5zsvOdrbp3NxLDbex1op0JAnNUlIE8UNF84O2PhmEpFAU+aU3IemNoFCwtGIURkVfCobSh1Ab1AeHGKmxmss53eKkS+d2znme++rDjnM7xyUJQdf17fr/7//zv+7r4n7g/xJSXFrPXmJ8LR46h3hZWxF20Xtz1W4OkKbGmSsvAKbWAbin6IxbWFjJBEdkJMQnUmBliuAR6dPzNxcZ4UVBm5+Z3J6MWXu9bkz6QsVrpz7DbLPvFt7JOlxOyyQmX6gyZWHJim/7JrTgvhQpnXjsLjN4yhnKi1sSLNcMrivHpMuW81Ke68lFEh0ZyJnI3G5sMJn4ufbSoPJ7HviOw7xl7rbuLo3J+/4gW/LkU+6RoKI0AGMFRFUBQRRQsh0nj77ivIAHBLiadr4KJxGVxg/Pb+DqjHkRKwsmYyghZqZLi8VBIn86qiXYfINgqj8Y2iZVqfTpsIrCG7CAiREZYxhsXa4qA1ZUHX6gn9g0zlXnweji7tsgxQhKQDiTVgKsn2voeajz81SaL9iTuq+zuicakEV5+tpMS90oC15PZJ8rcQV1ADPLiVWnfKCp64/+76Vdu4A1l44fjR9Ztv/kJxfryu5vuXI1KRbUxP/67V7N5gykGEWx6HSKRWvO7WjcyWIP1Oxq3dEAe1jkdbPljrIRNGJdnc6IReOjm1OFIhaNZto7mqj0ofWphb9W5JITiWMtT0K5fyftq/yQLFly5MiRQRNjxSIBWr8fiEB9j6OEKEroaFMP4EHVAVFy110nRjcVO/F0+cqVCMu2ukpWAlGxBGQ9bdlcSSdrHvWU4LrvxOimlJkzWEUio+tO7EYZeiJAjDqKCo6YLOPb0jxO7Y/RDI7MXmYKRTDDr14c4c1FmSWgM6gKjC2ojNTx8dmpX0D/QSRPd/BEihChjjJAbSEyhyoCbtPLi25n+wXTn9/baUSh8szx3Am66qOt08x5RFTQK6WfNm8gTv0+B6yEKCqh2ggt+xp5h8GOKZ9QC57VohGX7gXPh5qdkWt162vs2ekRNxykaDo3WDYvbOlsozoKKzbW9SWHk8MNfas3wsJoE0sf9mdt93wiikXjQ6ubH6DCA3UO+Yd8dSDhNbKk2T/LjUSkUEQJ0diZtk5AvgTgbRDoWO+f49oOzxLZnDIKVhArOpMi4WT94OHG3qVrz9WMR4ejl2va1tb2/vRNplpCkVlMC9gslKVMoZNZX0pqfCA2kJgXR5Oj21PiE3+jZCvjBWsnqIRWQleioFNOYFQdZM6fCKyWsXv4eVFwiDJfKPmHel7GFKG0cta0W5/D/Pv4iDTvmVVWdhKSE3sLEqBEKFfnls7+N/E3+SIPGi0O7EwAAAAldEVYdGRhdGU6Y3JlYXRlADIwMTUtMDctMDdUMDE6MzI6MTAtMDQ6MDBTJ8BcAAAAJXRFWHRkYXRlOm1vZGlmeQAyMDE1LTA3LTA3VDAxOjMyOjEwLTA0OjAwInp44AAAABh0RVh0U29mdHdhcmUAcGFpbnQubmV0IDQuMC41ZYUyZQAAAABJRU5ErkJggg==";
        private static string _comment_black = /*"data:image/png;base64," +*/" iVBORw0KGgoAAAANSUhEUgAAAC0AAAAcCAMAAAG15fY2AAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAGwUExURQAAAAoKCoaDg3VzcwkJCQAAADIxMQoKCgAAAAUFBQQEBCEgIA0NDQAAAAkICCYlJTMxMTg2NgcHBwAAAIuIiBAPDwsLCwQEBAAAAEpJSUlHRwkJCZ+bm46LiwAAAAsLCwAAAGhlZVdVVQwMDBsbGwAAAAsLCxQSEhkZGQUFBQAAAAsLCwkJCaSgoA0NDQcHB1RSUgYGBjEwMAwMDAoKCggICAgICAAAAAMDA6ejo5aTk3BtbSUkJB4eHg0NDQsLCwgICAAAADk4OGtpaaqmppmWlgAAAAYFBQQEBKyoqFFPTwYGBk1LSwAAAA4ODgQEBGVjYwcHBwAAAEVDQwAAAAEBAa+rqwwMDImFhSUkJAAAAAwLCwkICCsrKygnJwoKCiopKVZUVAgICERCQgAAAAAAAKGengcHBwoKCnt4eGpoaFhWVgcHBzU0NAAAACcmJiAgIAMDAwwLCwwLCwAAAH57e21rawoKCgUFBQsLC5GNjQYGBoB9fQoKCgAAAAwMDAAAAA4ODggHBw0NDU5NTZSQkAgICIOAgAUFBSIhIQUFBQICAg0NDQQEBAYGBgAAACYHPaAAAACQdFJOUwDU//+IXPbcCF968sEQn/Xy9Y0Y//ar+SD69qj//3zhKP//9PUw1vLy9jje0/+w9vl89bit9o9I+/////Ty08jrUPj9//8E93b/+/D4DNB+/4lo+hT9/83/7hzJ7PX53fH7fvd4JP+G5f//+/X3LPb0i7P0NP//9fqh/3j/vP/1RMfuz/v/5//37WLG12r3VMePwlwAAAAJcEhZcwAAFxEAABcRAcom8z8AAAHWSURBVChThZP9PxRRFIdPpPKW5DVZQkmJsGmtxFR2ahQKXUJtKLVlI+UlFWI1KvQvd8+937V37X6m54fZ73nO2Xm5c4eI+i15IMEc/pxlJdm+IQ/52qke0QvVkExzLbFlnkRWc1SpYxdnyaBl/dXpnm58krFTRyEOcFFmlqgAUbwhykbclPO31QkMRivQFWN1d+CotBcuTpTvjsKoDFz2OSgMHrEnO4oSBHuULsoUSwdP7re4IbH2+Ez3ayWJ/uQjpGezudqyuqtGUCoCK8U4tfN+Ao5oxYFU3Dqu7V3UcRw/211UCTrLpXZRGOxJHUI2uCL1MLIBv/qvSffBPJeWyB9ECdr6lS5Bqck4qSQ9FE7h4sCvpVBlMBJbzdGSztdsXUA0CAwheDA+GC57Flt2fSEm413fy7Lwxw9oJnGppyGS8gSMk5l7LPnqdukCdmt6mk741Q5S7F88ur1ScOZbMfygEMqTGL40K80LSqX3qZ4ejUB44vupp8vndmA8mG3n3cPY36NpF8/g3ERiUehmgdd45PJpY/bzdR/Lpppa98hSOt9m6hp/G6N06tqU2OnbqB/ir8u++ipve8+y1jve/vhSlTWuRxK0rncsBsx/exF/0v9D9A8Zfw2q/JDBywAAAABJRU5ErkJggg==";
        private static string _comment_blue = /*"data:image/png;base64," + */"iVBORw0KGgoAAAANSUhEUgAAACsAAAAdCAMAAAAEoGVCAAAA0lBMVEUAAADV4Om3ydqguc6guM2Jp8Nnj7Jtk7WxxdbP2+bt8vbD0uBWgqpBcZxSgKpShrZOhrhQir1UkMdTj8VSgq1KeKNbhq2BosC9zt3b5OxEc55ThbJXlcxbm9VVkshRi8BHdqFzmLjh6e9Qf6qUsMl9oL9ViblamtNvlbeRrslWjLxyl7hUiLeovtJSjcNYls5Rfqdhiq9RiLmOrMZeiK9ahq5diK5Vh7W0x9lPfaZNeqNThbBYl9BId6FWj8RXibeEpMJVhK6rwdSet81kjbJFdJ6ogPYcAAAAAXRSTlMAQObYZgAAAAFiS0dEDfa0YfUAAAAJcEhZcwAAAEgAAABIAEbJaz4AAAEbSURBVDjLndNrV4IwGMBxaIEi1w2McG6KFJSVqGUXy27a9/9KbXDKQ4o8p//r39nZ5Zmi/Dv1CB0jTRe1UNvo1DpTs2zH9TwPE0J833ODrn0Snu7CKOxR3Gd/4wM6jI2KHOkJZnX5ga5uKXIIO5QfnP3Q8zRjDaUXJb0cN0m59JWkRgCgjF3fCBtPQDa3hJ3mIMtsYWccRLOesG0XZOe38nB3kIX7i3I47kkjzRdRecGd1sPhx8hSK/p95MentP42MpygyvSg5TPex18wXYU7c2xqyaDC+MSjzlIz9w57WFretYtmMTJVpaZpMeuvev3X2TaU9O0d8i9NKs788QmhynrOuKWCqKKT8QayVdmKgrYqi75gWy0awWlz3y5uS3mDBW78AAAAJXRFWHRkYXRlOmNyZWF0ZQAyMDE1LTA3LTA3VDAwOjI0OjA2LTA0OjAw7VuD+gAAACV0RVh0ZGF0ZTptb2RpZnkAMjAxNS0wNy0wN1QwMDoyNDowNi0wNDowMJwGO0YAAAAASUVORK5CYII=";
        private static string _alert_image = /*"data:image/png;base64," + */"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";
        /// <summary>
        /// Return a byte[] of the proper icon of the image. If image is not available returns the black icon otherwise green or blue or ...
        /// </summary>
        /// <param name="_image">byte[] of the actual image</param>
        /// <param name="width"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public static byte[] ToIcon(this byte[] _image, int width, int height, ImageIcon _icon)
        {
            byte[] result;
            if (IsValidImage(_image))
                switch (_icon)
                {
                    case ImageIcon.Camera:
                        result = Convert.FromBase64String(_camera_green);
                        break;
                    case ImageIcon.Comment:
                        result = Convert.FromBase64String(_comment_blue);
                        break;
                    default: result = Convert.FromBase64String(_alert_image);
                        break;
                }
            else switch (_icon)
                {
                    case ImageIcon.Camera:
                        result = Convert.FromBase64String(_camera_black);
                        break;
                    case ImageIcon.Comment:
                        result = Convert.FromBase64String(_comment_black);
                        break;
                    default:
                        result = Convert.FromBase64String(_alert_image);
                        break;
                }
            using (var stream = new MemoryStream())
            {
                Transparent2Color(ResizeImage(result, width, height), Color.White).Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }
        }

        public static byte[] ToIcon(this string comment, int width, int height, ImageIcon _icon)
        {
            byte[] result;
            if (comment != null && comment.Length > 0)
                switch (_icon)
                {
                    case ImageIcon.Camera:
                        result = Convert.FromBase64String(_camera_green);
                        break;
                    case ImageIcon.Comment:
                        result = Convert.FromBase64String(_comment_blue);
                        break;
                    default:
                        result = Convert.FromBase64String(_alert_image);
                        break;
                }
            else switch (_icon)
                {
                    case ImageIcon.Camera:
                        result = Convert.FromBase64String(_camera_black);
                        break;
                    case ImageIcon.Comment:
                        result = Convert.FromBase64String(_comment_black);
                        break;
                    default:
                        result = Convert.FromBase64String(_alert_image);
                        break;
                }
            using (var stream = new MemoryStream())
            {
                Transparent2Color(ResizeImage(result, width, height),Color.White).Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }
        }

        public static bool IsValidImage(this byte[] bytes)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream(bytes))
                    Image.FromStream(ms);
            }
            catch (ArgumentException)
            {
                return false;
            }
            return true;
        }

        public static byte[] ResizeImageWithBackColor(this byte[] image, int width, int height)
        {
            using (var stream = new MemoryStream())
            {
                Transparent2Color(ResizeImage(image, width, height), Color.White).Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        private static Bitmap ResizeImage(byte[] _image, int max_width, int max_height)
        {
            Image image;
            try
            {
                using (MemoryStream ms = new MemoryStream(_image))
                    image = Image.FromStream(ms);
            }
            catch (ArgumentException)
            {
                return new Bitmap(max_width, max_height);
            }
 
            var ratioX = (double)max_width / image.Width;
            var ratioY = (double)max_height / image.Height;
            var ratio = Math.Min(ratioX, ratioY);

            var width = (int)(image.Width * ratio);
            var height = (int)(image.Height * ratio);
            

            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);
            
            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
        static Bitmap Transparent2Color(Bitmap bmp1, Color target)
        {
            Bitmap bmp2 = new Bitmap(bmp1.Width, bmp1.Height);
            Rectangle rect = new Rectangle(Point.Empty, bmp1.Size);
            using (Graphics G = Graphics.FromImage(bmp2))
            {
                G.Clear(target);
                G.DrawImageUnscaledAndClipped(bmp1, rect);
            }
            return bmp2;
        }
    }
}