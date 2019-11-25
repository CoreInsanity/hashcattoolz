using HashCatToolz.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HashCatToolz.Helpers
{
    class ConversionHelper
    {
        /// <summary>
        /// Reads all files in a given directory, sends them to the hashcat.net online conversion tool.
        /// Only saves the result in inputPath\converted if it's a VALID handshake
        /// </summary>
        /// <param name="inputPath"></param>
        /// <returns></returns>
        public static async Task ConvertHandshakes(LaunchArgs opts)
        {
            string convDir = Path.Combine(opts.InDir, @"converted\");

            if (!Directory.Exists(convDir)) Directory.CreateDirectory(convDir);

            foreach (var handshake in Directory.GetFiles(opts.InDir))
            {
                Console.Write("Converting {0}... ", Path.GetFileNameWithoutExtension(handshake));
                if (new FileInfo(handshake).Length > 20000000) continue; //Make sure the pcap is smaller than 20MB

                HttpContent bytesContent = new ByteArrayContent(File.ReadAllBytes(handshake));
                using (var client = new HttpClient())
                using (var formData = new MultipartFormDataContent())
                {
                    formData.Add(bytesContent, "\"file\"", String.Format("\"{0}\"", Path.GetFileName(handshake)));
                    var response = await client.PostAsync(@"https://hashcat.net/cap2hccapx/index.pl", formData);

                    var convBytes = await response.Content.ReadAsByteArrayAsync();

                    if (!new StreamReader(new MemoryStream(convBytes)).ReadToEnd().ToLower().StartsWith("networks detected:"))
                    {
                        File.WriteAllBytes(Path.Combine(convDir, Path.GetFileNameWithoutExtension(handshake) + ".hccapx"), convBytes);
                        Console.WriteLine("Done");
                    }
                    else Console.WriteLine("Invalid");
                }

                opts.InDir = convDir;
            }
        }
    }
}
