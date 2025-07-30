using System;
using System.Drawing;
using System.Text;
using System.Collections.Generic;
using System.IO;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using ZXing.Windows.Compatibility;

namespace LoboForge.TNOIRC.Services
{
    public class ImageTools
    {
        public string? DecodeQrFromFile(string filePath)
        {
            try
            {
                using var bitmap = (Bitmap)Bitmap.FromFile(filePath);
                // Step 1: Create the luminance source
                var luminanceSource = new BitmapLuminanceSource(bitmap);
                // Step 2: Create the binarizer and binary bitmap
                var binarizer = new HybridBinarizer(luminanceSource);
                var binaryBitmap = new BinaryBitmap(binarizer);
                // Step 3: Decode
                var reader = new QRCodeReader();
                var result = reader.decode(binaryBitmap);

                return result?.Text;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Extracts LSBs from all RGBA channels (b1,rgba,lsb,xy order) and tries to decode the result as ASCII (for base64) and then base64.
        /// Returns the best-effort decoded string, or null if nothing is found.
        /// </summary>
        public string? ExtractLsbAllChannels(string filePath)
        {
            try
            {
                var bits = new List<int>();
                using (var bmp = new Bitmap(filePath))
                {
                    for (int y = 0; y < bmp.Height; y++)
                    {
                        for (int x = 0; x < bmp.Width; x++)
                        {
                            var pixel = bmp.GetPixel(x, y);
                            bits.Add(pixel.R & 1);
                            bits.Add(pixel.G & 1);
                            bits.Add(pixel.B & 1);
                            bits.Add(pixel.A & 1); // Always returns 255 for 24bpp, but works for 32bpp PNGs
                        }
                    }
                }

                // Pack bits into bytes
                var bytes = new List<byte>();
                for (int i = 0; i + 7 < bits.Count; i += 8)
                {
                    byte b = 0;
                    for (int j = 0; j < 8; j++)
                    {
                        b |= (byte)(bits[i + j] << (7 - j));
                    }
                    bytes.Add(b);
                }

                string ascii = Encoding.ASCII.GetString(bytes.ToArray());

                // Search for first base64 string in ASCII (should start with 'cG...' for picoCTF)
                var base64 = ExtractFirstBase64(ascii);
                if (!string.IsNullOrEmpty(base64))
                {
                    try
                    {
                        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
                        return decoded;
                    }
                    catch
                    {
                        // Not decodable as base64; fallback to ASCII
                    }
                }

                // If nothing special, just return ASCII
                return ascii.Trim('\0', '\r', '\n');
            }
            catch
            {
                return null;
            }
        }

        // Helper to extract the first base64-like string from ASCII
        private static string? ExtractFirstBase64(string s)
        {
            // Find the first plausible base64 sequence
            var match = System.Text.RegularExpressions.Regex.Match(s, @"([A-Za-z0-9+/=]{32,})");
            return match.Success ? match.Value : null;
        }

    }
}
