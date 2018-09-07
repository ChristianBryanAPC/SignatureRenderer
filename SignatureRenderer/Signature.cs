using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using LanguageExt;
using static LanguageExt.Prelude;

namespace SignatureRenderer
{
    public static class Signature
    {
        public const int RAISE_PEN = 288;

        public static Size BlackBaySize = new Size(265, 265);
        public static Size APCSize = new Size(236, 238);


        public static void SaveBitmapAsPng(SKBitmap bitmap, string filePath)
        {
            var image = SKImage.FromBitmap(bitmap);
            var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using (var stream = File.OpenWrite(filePath))
            {
                data.SaveTo(stream);
            }   
        }

        public static byte[] ConvertBitmapToPng(SKBitmap bitmap)
        {
            var image = SKImage.FromBitmap(bitmap);
            var data = image.Encode(SKEncodedImageFormat.Png, 100);
            var stream = new MemoryStream();
            data.SaveTo(stream);
            return stream.ToArray();
        }

        public static string ConvertImageMapToPngBase64(string imagemap, Size size)
        {
            var imageBytes = ConvertBitmapToPng(ConvertImagemapToBitmap(imagemap, size));
            return Convert.ToBase64String(imageBytes);
        }

        public static byte[] ConvertImageMapToPngBytes(string imagemap, Size size)
        {
            return ConvertBitmapToPng(ConvertImagemapToBitmap(imagemap, size));
        }

        public static string ConvertBlackBaySignaturePointsToApcImagemap(string signature)
        {
            var coords = ConvertBlackBaySignaturePointsToCoords(signature);
            var paths = ConvertBlackBayCoordsToPaths(coords);
            var apccoords = ConvertPathsToAPCCoords(paths);
            var imagemap = ConvertAPCCoordsToImageMap(apccoords);
            return imagemap;
        }

        public static string ConvertApcImagemapToBlackBaySignaturePoints(string imageMap)
        {
            var builder = new StringBuilder();
            var coords = ConvertAPCImageMapToCoords(imageMap);
            coords.ForEach(coord => builder.AppendFormat("({0},{1}),", coord.Item1, coord.Item2));
            var temp = builder.ToString();
            var paths = ConvertAPCCoordsToPaths(coords);
            var builder2 = new StringBuilder();
            builder2.Append("(");
            paths.ForEach(path =>
            {
                builder2.Append("[");
                path.ForEach(coord => builder2.AppendFormat("({0},{1})", coord.Item1, coord.Item2));
            });
            builder2.Append(")");
            var temp2 = builder2.ToString();
            var bbcoords = ConvertAPCPathsToBlackBayCoords(paths);
            var signaturepoints = ConvertBlackBayCoordsToSignaturePoints(bbcoords);
            return signaturepoints;
        }

        public static SKBitmap ConvertImagemapToBitmap(string imagemap, Size size)
        {
            return ConvertImagemapToBitmap(imagemap, size, SKColors.White, SKColors.Black);
        }

        public static SKBitmap ConvertImagemapToBitmap(string imagemap, Size size, SKColor backgroundColour, SKColor penColour)
        {  
            var conv = compose<string, List<Tuple<short, short>>, List<List<Tuple<short, short>>>>(ConvertAPCImageMapToCoords, ConvertAPCCoordsToPaths);
            return ConvertPointsToBitmap(imagemap, size, conv, backgroundColour, penColour);
        }

        public static SKBitmap ConvertBlackBaySignaturePointsToBitmap(string signaturePoints, Size size)
        {
            return ConvertBlackBaySignaturePointsToBitmap(signaturePoints, size, SKColors.White, SKColors.Black);
        }

        public static SKBitmap ConvertBlackBaySignaturePointsToBitmap(string signaturePoints, Size size, SKColor backgroundColour, SKColor penColour)
        {
            var conv = compose<string, List<Tuple<short, short>>, List<List<Tuple<short, short>>>> (ConvertBlackBaySignaturePointsToCoords, ConvertBlackBayCoordsToPaths);
            return ConvertPointsToBitmap(signaturePoints, size, conv, backgroundColour, penColour);
            
        }

        public static SKBitmap ConvertPointsToBitmap(string points, Size size, Func<string, List<List<Tuple<short, short>>>> pathsFunc, SKColor backgroundColour , SKColor penColour)
        {
            var paths = pathsFunc(points);
            var bitmap = new SKBitmap((int)size.Width, (int)size.Height, true);
            var canvas = new SKCanvas(bitmap);
            var skpaths = CreateSKPaths(paths);
            canvas.DrawRect(0, 0, size.Width, size.Height, new SKPaint { Style = SKPaintStyle.Fill, Color = backgroundColour });
            skpaths.ForEach(skpath =>
                canvas.DrawPath(skpath, new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = 1, Color = penColour }
            ));
            return bitmap;
        }

        public static List<SKPath> CreateSKPaths(List<List<Tuple<short, short>>> paths)
        {
            return paths.Select(x => CreateSKPath(x)).ToList();
        }

        public static SKPath CreateSKPath(List<Tuple<short, short>> pathCoords)
        {
            var path = new SKPath();
            var start = pathCoords.FirstOrDefault();
            if(start != null) path.MoveTo(start.Item1, start.Item2);
            pathCoords.Skip(1).ToList().ForEach(x => path.LineTo(x.Item1, x.Item2));
            return path;
        }

        public static List<List<Tuple<short, short>>> ConvertAPCCoordsToPaths(List<Tuple<short, short>> coords)
        {
            var paths = new List<List<Tuple<short, short>>>();
            var stuff = coords;
            while (stuff.Any())
            {
                var path = new List<Tuple<short, short>>();
                var startOfPath = Tuple((short)(stuff.First().Item1), (short)(stuff.First().Item2 - RAISE_PEN));
                path.Add(startOfPath);
                var temp = stuff.Skip(1).TakeWhile(x => !(x.Item2 > RAISE_PEN)).ToList();
                stuff = stuff.Skip(1).SkipWhile(x => !(x.Item2 > RAISE_PEN)).ToList();
                path.AddRange(temp);
                paths.Add(path);
            }
            return paths;
        }

        public static List<List<Tuple<short, short>>> ConvertBlackBayCoordsToPaths(List<Tuple<short, short>> coords)
        {
            var paths = new List<List<Tuple<short, short>>>();
            var stuff = coords;
            while (stuff.Any())
            {
                var path = new List<Tuple<short, short>>();
                var startOfPath = Tuple(stuff.First().Item1, stuff.First().Item2);
                if (!(startOfPath.Item1 == -1 && startOfPath.Item2 == -1)) path.Add(startOfPath);
                var temp = stuff.Skip(1).TakeWhile(x => !(x.Item1 == -1 && x.Item2 == -1)).ToList();
                stuff = stuff.Skip(1).SkipWhile(x => !(x.Item1 == -1 && x.Item2 == -1)).ToList();
                path.AddRange(temp);
                paths.Add(path);
            }
            return paths;
        }

        public static List<Tuple<short, short>> ConvertAPCPathsToBlackBayCoords(List<List<Tuple<short, short>>> apcPaths)
        {
            var bbCoords = new List<Tuple<short, short>>();
            apcPaths.ForEach(path => bbCoords.AddRange(ConvertAPCPathToBlackBayCoords(path)));
            bbCoords.RemoveAt(0);
            return bbCoords;
        }

        public static List<Tuple<short, short>> ConvertAPCPathToBlackBayCoords(List<Tuple<short, short>> apcPath)
        {
            var bbCoords = new List<Tuple<short, short>>();
            bbCoords.Add(Tuple((short)-1, (short)-1)); //This is BlackBay's Pen Up Delimiter
            bbCoords.AddRange(apcPath);
            return bbCoords;
        }

        public static List<Tuple<short, short>> ConvertPathsToAPCCoords(List<List<Tuple<short, short>>> bbPaths)
        {
            var apcCoords = new List<Tuple<short, short>>();
            bbPaths.ForEach(path => apcCoords.AddRange(ConvertPathToAPCCoords(path)));
            return apcCoords;
        }

        public static List<Tuple<short, short>> ConvertPathToAPCCoords(List<Tuple<short, short>> bbPath)
        {
            var apcCoords = new List<Tuple<short, short>>();
            var firstCoord = bbPath.FirstOrDefault();
            if (firstCoord != null)
            {
                apcCoords.Add(Tuple((short)(firstCoord.Item1), (short)(firstCoord.Item2 + RAISE_PEN)));
                apcCoords.AddRange(bbPath.Skip(1).ToList());
            }
            return apcCoords;
        }

        public static List<Tuple<short, short>> ConvertAPCImageMapToCoords(string imageMap)
        {
            return ChunksUpto(imageMap, 4)
                .Select(x => ChunksUpto(x, 2).ToList())
                .Select(x => Tuple((short)(ToBase10(x[0], 24)), (short)(ToBase10(x[1], 24)))).ToList();
        }

        public static List<Tuple<short, short>> ConvertBlackBaySignaturePointsToCoords(string blackbayCoords) 
        {
            var semiFree = blackbayCoords.Replace(';', ',');
            var points = semiFree.Split(',').ToList();
            points.RemoveAt(points.Count - 1);
            var shorts = points.Select(x => short.Parse(x)).ToList();

            var selectEvens = shorts.Where((x, i) => i % 2 == 0);
            var selectOdds = shorts.Where((x, i) => i % 2 != 0);

            var coordPairs = selectEvens.Zip(selectOdds, (x, y) => new Tuple<short, short>(x, y)).ToList();
            return coordPairs;
        }

        public static string ConvertAPCCoordsToImageMap(List<Tuple<short, short>> coords)
        {
            var builder = new StringBuilder();
            coords.ForEach(x => {
                var xCoord = FromBase10(x.Item1, 24, 2);
                var yCoord = FromBase10(x.Item2, 24, 2);
                builder.Append(xCoord);
                builder.Append(yCoord);
            });
            return builder.ToString();
        }

        public static string ConvertBlackBayCoordsToSignaturePoints(List<Tuple<short, short>> coords)
        {
            coords.Add(new Tuple<short, short>(-1, -1)); //We need to stick this on the end
            var signaturePoints = coords.Select(x => $"{x.Item1},{x.Item2}")
                .Aggregate((s1, s2) => s1 + ";" + s2);

            //We need to do this as BlackBay are really not taking into account of the last coord and applying
            //the ; delimiter regardless
            return signaturePoints + ";";
        }

        public static int ToBase10(string number, int start_base)
        {
            if (start_base < 2 || start_base > 64) return 0;
            if (start_base == 10) return Convert.ToInt32(number);

            char[] chrs = number.ToCharArray();
            int m = chrs.Length - 1;
            int n = start_base;
            int x;
            int rtn = 0;

            foreach (char c in chrs)
            {
                if (char.IsNumber(c))
                    x = int.Parse(c.ToString());
                else
                    x = Convert.ToInt32(c) - 55;

                rtn += x * (Convert.ToInt32(Math.Pow(n, m)));
                m--;
            }
            return rtn;
        }

        public static string FromBase10(int number, int target_base, int padding)
        {
            if (target_base < 2 || target_base > 64) return "";
            if (target_base == 10) return number.ToString();

            int n = target_base;
            int q = number;
            int r;
            string rtn = "";

            while (q >= n)
            {

                r = q % n;
                q = q / n;

                if (r < 10)
                    rtn = r.ToString() + rtn;
                else
                    rtn = Convert.ToChar(r + 55).ToString() + rtn;

            }

            if (q < 10)
                rtn = q.ToString() + rtn;
            else
                rtn = Convert.ToChar(q + 55).ToString() + rtn;

            if (rtn.Length < padding)
            {
                var numberofZeros = padding - rtn.Length;
                var zeros = new String('0', numberofZeros);
                rtn = zeros + rtn;
            }
            return rtn;
        }

        private static IEnumerable<string> ChunksUpto(string str, int maxChunkSize)
        {
            for (int i = 0; i < str.Length; i += maxChunkSize)
                yield return str.Substring(i, Math.Min(maxChunkSize, str.Length - i));
        }
    }

    public static class EnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int chunksize)
        {
            while (source.Any())
            {
                yield return source.Take(chunksize);
                source = source.Skip(chunksize);
            }
        }
    }
}