using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using Directory = System.IO.Directory;
using ImageMagick;

namespace InstagramFamous.Classes
{
    class FileManager
    {
        public FileManager()
        {
            string directoryName = Properties.Config.Default.FileDirectory;
            
            if (!Directory.Exists(directoryName))
            {
                try
                {
                    Directory.CreateDirectory(directoryName);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Unable to create a new directory {Environment.NewLine} {ex.ToString()}");
                }
            }
        }

        /// <summary>
        /// Downloads all the posts in a dictionary
        /// </summary>
        /// <param name="dictPost"></param>
        /// <returns></returns>
        public bool DownloadPost(Dictionary<string, string> dictPost)
        {
            string directoryName    = Properties.Config.Default.FileDirectory;
            string fileName         = dictPost["Title"];
            string fileUrl          = dictPost["Link"];
            string filePath         = directoryName + "\\" + fileName;

            filePath += fileUrl.Contains(".png") ? ".png" : ".jpg";

            try
            {
                using (WebClient client = new WebClient())
                {
                    client.DownloadFile(fileUrl, filePath);
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("An error happened while downloading a file from reddit." + Environment.NewLine + ex.ToString());
            }
        }

        /// <summary>
        /// Changes the picture format from .png to .jpg
        /// </summary>
        /// <param name="filePath"></param>
        public void ChangePictureFormat(string filePath)
        {
            string jpgFilePath = filePath.Replace(".png", ".jpg");

            try
            {
                Image pngImage = Image.FromFile(filePath);
                pngImage.Save(jpgFilePath, System.Drawing.Imaging.ImageFormat.Jpeg);
                pngImage.Dispose();
                
            }
            catch (Exception ex)
            {
                throw ex;
            }
           
            File.Delete(filePath);
        }

        /// <summary>
        /// Adds padding to a picture to make it square
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public bool AddPaddingToPicture(string filePath)
        {
            try
            {
                Image originalImage = Image.FromFile(filePath);

                int largestDimension = Math.Max(originalImage.Height, originalImage.Width);
                Size squareSize = new Size(largestDimension, largestDimension);
                Bitmap squareImage = new Bitmap(squareSize.Width, squareSize.Height);
                using (Graphics graphics = Graphics.FromImage(squareImage))
                {
                    graphics.FillRectangle(Brushes.White, 0, 0, squareSize.Width, squareSize.Height);
                    graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                    graphics.DrawImage(originalImage, (squareSize.Width / 2) - (originalImage.Width / 2), (squareSize.Height / 2) - (originalImage.Height / 2), originalImage.Width, originalImage.Height);

                    graphics.Dispose();
                }

                originalImage.Dispose();
                File.Delete(filePath);
                squareImage.Save(filePath);
                squareImage.Dispose();

                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public Bitmap ResizeImage(string filePath, int width = 1080, int height = 1080)
        {
            Image image = Image.FromFile(filePath);

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

                graphics.Dispose();
            }
            
            image.Dispose();
            File.Delete(filePath);
            destImage.Save(filePath,ImageFormat.Jpeg);

            return null;
        }

        /// <summary>
        /// Attempt at removing metadata / exif data from an image
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public bool RemoveExif(string filePath)
        {
            try
            {
                using (MagickImage metaImage = new MagickImage())
                {
                    metaImage.Read(filePath);
                    metaImage.Strip();
                    metaImage.Write(filePath);
                    metaImage.Dispose();
                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
