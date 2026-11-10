using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DBD_Clock_Callouts
{
    internal class MapImages
    {
        public async Task<Image> GetImage(string name)
        {
            string uhhh = name.Trim();

            if (!uhhh.EndsWith(".png", StringComparison.OrdinalIgnoreCase) && !uhhh.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)) // some names will be changed in the future
                uhhh += ".png";

            string idk = $"https://raw.githubusercontent.com/vexk1x/dbd-clock-callouts/main/maps/{uhhh}";

            using (WebClient client = new WebClient())
            {
                byte[] data = await client.DownloadDataTaskAsync(idk);

                using (MemoryStream stream = new MemoryStream(data))
                using (Image image = Image.FromStream(stream))
                {
                    return new Bitmap(image);
                }
            }
        }

        public async Task<List<string>> LoadMapNames()
        {
            string idk = "https://raw.githubusercontent.com/vexk1x/dbd-clock-callouts/main/maps/names.json";

            HttpClient client = new HttpClient();

            string json = await client.GetStringAsync(idk);


            return JsonSerializer.Deserialize<List<string>>(json);
        }
    }
}
