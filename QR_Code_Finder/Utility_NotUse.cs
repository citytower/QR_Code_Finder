using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;

namespace QR_Code_Finder
{
    class Utility_NotUse
    {
        private static byte[] ImageToByte2(Image img)
        {
            using (var stream = new MemoryStream())
            {
                img.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
                return stream.ToArray();
            }
        }

        private static Bitmap SetBitmapRawData1(Bitmap bitmap)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                bitmap.Save(ms, ImageFormat.Bmp);
                byte[] bitmapData = ms.GetBuffer();

                const int BITMAP_HEADER_OFFSET = 54;
                Color colorValue = Color.Purple;

                for (int i = 0; i < bitmapData.Length; i += 4)
                {
                    bitmapData[BITMAP_HEADER_OFFSET + i] = colorValue.R;
                    bitmapData[BITMAP_HEADER_OFFSET + i + 1] = colorValue.G;
                    bitmapData[BITMAP_HEADER_OFFSET + i + 2] = colorValue.B;
                    bitmapData[BITMAP_HEADER_OFFSET + i + 3] = colorValue.A;
                }

                return new Bitmap(ms);
            }
        }
        private static Bitmap SetBitmapRawData(Bitmap bitmap)
        {
            // Lock the bitmap's bits.
            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            System.Drawing.Imaging.BitmapData bmpData = bitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, bitmap.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            byte[] bitmapData = new byte[Math.Abs(bmpData.Stride) * bitmap.Height];

            // Copy the RGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(ptr, bitmapData, 0, bitmapData.Length);

            Color colorValue = Color.Purple;
            for (int i = 0; i < bitmapData.Length; i += 4)
            {
                bitmapData[i] = colorValue.R;
                bitmapData[i + 1] = colorValue.G;
                bitmapData[i + 2] = colorValue.B;
                bitmapData[i + 3] = colorValue.A;
            }

            // Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(bitmapData, 0, ptr, bitmapData.Length);

            // Unlock the bits.
            bitmap.UnlockBits(bmpData);

            return bitmap;
        }
    }

    class sampleCode1
    {

        /// <summary>
        /// https://github.com/Itseez/opencv_extra/blob/master/learning_opencv_v2/ch9_watershed.cpp
        /// </summary>
        private static void watershedExample()
        {
            var src = new Mat(@"..\..\Images\corridor.jpg", ImreadModes.AnyDepth | ImreadModes.AnyColor);
            var srcCopy = new Mat();
            src.CopyTo(srcCopy);

            var markerMask = new Mat();
            Cv2.CvtColor(srcCopy, markerMask, ColorConversionCodes.BGRA2GRAY);

            var imgGray = new Mat();
            Cv2.CvtColor(markerMask, imgGray, ColorConversionCodes.GRAY2BGR);
            markerMask = new Mat(markerMask.Size(), markerMask.Type(), s: Scalar.All(0));

            var sourceWindow = new Window("Source (Select areas by mouse and then press space)")
            {
                Image = srcCopy
            };

            var previousPoint = new OpenCvSharp.Point(-1, -1);
            sourceWindow.OnMouseCallback += (@event, x, y, flags) =>
            {
                if (x < 0 || x >= srcCopy.Cols || y < 0 || y >= srcCopy.Rows)
                {
                    return;
                }

                if (@event == MouseEvent.LButtonUp || !flags.HasFlag(MouseEvent.FlagLButton))
                {
                    previousPoint = new OpenCvSharp.Point(-1, -1);
                }
                else if (@event == MouseEvent.LButtonDown)
                {
                    previousPoint = new OpenCvSharp.Point(x, y);
                }
                else if (@event == MouseEvent.MouseMove && flags.HasFlag(MouseEvent.FlagLButton))
                {
                    var pt = new OpenCvSharp.Point(x, y);
                    if (previousPoint.X < 0)
                    {
                        previousPoint = pt;
                    }

                    Cv2.Line(img: markerMask, pt1: previousPoint, pt2: pt, color: Scalar.All(255), thickness: 5);
                    Cv2.Line(img: srcCopy, pt1: previousPoint, pt2: pt, color: Scalar.All(255), thickness: 5);
                    previousPoint = pt;
                    sourceWindow.Image = srcCopy;
                }
            };

            var rnd = new Random();

            for (;;)
            {
                var key = Cv2.WaitKey(0);

                if ((char)key == 27) // ESC
                {
                    break;
                }

                if ((char)key == 'r') // Reset
                {
                    markerMask = new Mat(markerMask.Size(), markerMask.Type(), s: Scalar.All(0));
                    src.CopyTo(srcCopy);
                    sourceWindow.Image = srcCopy;
                }

                if ((char)key == 'w' || (char)key == ' ') // Apply watershed
                {
                    OpenCvSharp.Point[][] contours; //vector<vector<Point>> contours;
                    HierarchyIndex[] hierarchyIndexes; //vector<Vec4i> hierarchy;
                    Cv2.FindContours(
                        markerMask,
                        out contours,
                        out hierarchyIndexes,
                        mode: RetrievalModes.CComp,
                        method: ContourApproximationModes.ApproxSimple);

                    if (contours.Length == 0)
                    {
                        continue;
                    }

                    var markers = new Mat(markerMask.Size(), MatType.CV_32S, s: Scalar.All(0));

                    var componentCount = 0;
                    var contourIndex = 0;
                    while ((contourIndex >= 0))
                    {
                        Cv2.DrawContours(
                            markers,
                            contours,
                            contourIndex,
                            color: Scalar.All(componentCount + 1),
                            thickness: -1,
                            lineType: LineTypes.Link8,
                            hierarchy: hierarchyIndexes,
                            maxLevel: int.MaxValue);

                        componentCount++;
                        contourIndex = hierarchyIndexes[contourIndex].Next;
                    }

                    if (componentCount == 0)
                    {
                        continue;
                    }

                    var colorTable = new List<Vec3b>();
                    for (var i = 0; i < componentCount; i++)
                    {
                        var b = rnd.Next(0, 255); //Cv2.TheRNG().Uniform(0, 255);
                        var g = rnd.Next(0, 255); //Cv2.TheRNG().Uniform(0, 255);
                        var r = rnd.Next(0, 255); //Cv2.TheRNG().Uniform(0, 255);

                        colorTable.Add(new Vec3b((byte)b, (byte)g, (byte)r));
                    }

                    Cv2.Watershed(src, markers);

                    var watershedImage = new Mat(markers.Size(), MatType.CV_8UC3);

                    // paint the watershed image
                    for (var i = 0; i < markers.Rows; i++)
                    {
                        for (var j = 0; j < markers.Cols; j++)
                        {
                            var idx = markers.At<int>(i, j);
                            if (idx == -1)
                            {
                                watershedImage.Set(i, j, new Vec3b(255, 255, 255));
                            }
                            else if (idx <= 0 || idx > componentCount)
                            {
                                watershedImage.Set(i, j, new Vec3b(0, 0, 0));
                            }
                            else
                            {
                                watershedImage.Set(i, j, colorTable[idx - 1]);
                            }
                        }
                    }

                    watershedImage = watershedImage * 0.5 + imgGray * 0.5;
                    Cv2.ImShow("Watershed Transform", watershedImage);
                    Cv2.WaitKey(1); //do events
                }
            }

            sourceWindow.Dispose();
            Cv2.DestroyAllWindows();
            src.Dispose();
        }
    }
}
