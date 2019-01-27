using InstaSharper.API;
using InstaSharper.API.Builder;
using InstaSharper.Classes;
using InstaSharper.Classes.Models;
using InstaSharper.Logger;
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace InstagramFamous.Classes
{
    class InstagramManager
    {
        private IInstaApi InstagramClient;

        public InstagramManager()
        {
            UserSessionData userSession = new UserSessionData
            {
                UserName = Properties.Config.Default.InstagramUsername,
                Password = Properties.Config.Default.InstagramPassword
            };

            var delay = RequestDelay.FromSeconds(2, 3);
            InstagramClient = InstaApiBuilder.CreateBuilder()
                .SetUser(userSession)
                .UseLogger(new DebugLogger(LogLevel.Exceptions))
                .SetRequestDelay(delay)
                .Build();
        }

        /// <summary>
        /// Logs the user in on instagram
        /// </summary>
        /// <returns></returns>
        public bool Login()
        {
            if (!InstagramClient.IsUserAuthenticated)
            {
                var loginResult = InstagramClient.LoginAsync();
                if (loginResult.Result.Succeeded)
                {
                    if (InstagramClient.IsUserAuthenticated)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Logs the user out of instagram
        /// </summary>
        /// <returns></returns>
        public bool Logout()
        {
            if (!InstagramClient.IsUserAuthenticated)
            {
                var logoutResult = InstagramClient.LogoutAsync();
                if (logoutResult.Result.Succeeded)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Uploads a picture to instagram
        /// Use the login() function before uploading.
        /// </summary>
        /// <param name="filePath">Path to the picture that needs to be uploaded</param>
        /// <returns></returns>
        public bool UploadPicture(string filePath)
        {
            string captionTags = Properties.Config.Default.InstagramTags;
            string captionTitle = Path.GetFileNameWithoutExtension(filePath);
            string instagramCaption = captionTitle + Environment.NewLine + captionTags;

            Image image = Image.FromFile(filePath);
            InstaImage instagramImage = new InstaImage
            {
                Width = image.Width,
                Height = image.Height,
                URI = new Uri(Path.GetFullPath(filePath), UriKind.Absolute).LocalPath
            };

            var x = InstagramClient.UploadPhotoAsync(instagramImage, instagramCaption);
            image.Dispose();
            while (!x.IsCompleted)
            {
                Thread.Sleep(1000);
            }
            Console.WriteLine(x.Result);
            if (x.Result.Succeeded)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
