using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using ZXing;
using ZXing.Common;
using ZXing.QrCode.Internal;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace QR_Code_Finder
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btn_loadImage_Click(object sender, EventArgs e)
        {
            if (this.txtBoxFilePath.Text.Trim() != string.Empty)
            {
                this.pictureBox1.Image = Image.FromFile(this.txtBoxFilePath.Text);
            }
        }

        private void btn_Recognize_Click(object sender, EventArgs e)
        {
            if (this.txtBoxFilePath.Text.Trim() != string.Empty)
            {
                //this.Detect1();
                //Bitmap invoiceImage = new Bitmap(this.txtBoxFilePath.Text);
                MessageBox.Show(this.Detect(new Bitmap(this.txtBoxFilePath.Text)));
            }
        }

        public void Detect1()
        {
            BarcodeReader reader = new BarcodeReader();
            reader.Options.CharacterSet = "UTF-8";
            Bitmap map = new Bitmap(this.txtBoxFilePath.Text);
            Result result = reader.Decode(map);
            if (result == null)
                MessageBox.Show("识别失败");
            else
                MessageBox.Show(result.Text);
        }

        public string Detect(Bitmap bitmap)
        {
            try
            {
                LuminanceSource source = new BitmapLuminanceSource(bitmap);
                var binarizer = new HybridBinarizer(source);
                var binBitmap = new BinaryBitmap(binarizer);
                BitMatrix bm = binBitmap.BlackMatrix;
                Detector detector = new Detector(bm);
                DetectorResult result = detector.detect();

                string retStr = "Found at points ";
                foreach (ResultPoint point in result.Points)
                {
                    retStr += point.ToString() + ", ";
                }

                return retStr;
            }
            catch
            {
                return "Failed to detect QR code.";
            }
        }

        private void btn_OpenFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = @"E:\sample\";
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                this.txtBoxFilePath.Text = openFileDialog1.FileName;
                this.btn_loadImage_Click(sender,e);
            }
        }

        private void btn_OpenCV_Processing_Click(object sender, EventArgs e)
        {
            if(this.txtBoxFilePath.Text == string.Empty)
            {
                MessageBox.Show("请选择文件");
                return;
            }
            using (Bitmap bitmap = (Bitmap)Bitmap.FromFile(this.txtBoxFilePath.Text))
            {
                //this.QR_Find(bitmap);
                this.pictureProcessing(bitmap);
            }
        }

        private void pictureProcessing(Bitmap bitmap)
        {
            //var imageSrc = new Mat(new OpenCvSharp.Size(bitmap.Width, bitmap.Height), MatType.CV_8UC1);
            var imageSrc = BitmapConverter.ToMat(bitmap);
            var imageOrignal = new Mat();
            imageSrc.CopyTo(imageOrignal);
            //BitmapConverter.ToMat(bitmap, imageSrc);
            //var imageSrc = new Mat(new OpenCvSharp.Size(bitmap.Width, bitmap.Height), MatType.CV_8UC1);
            //imageTemp.CopyTo(imageSrc);
            //var imageTarget = new Mat(new OpenCvSharp.Size(imageSrc.Width, imageSrc.Height), MatType.CV_8UC1);
            imageSrc.CvtColor(ColorConversionCodes.RGB2GRAY, 0);
            imageSrc.GaussianBlur(new OpenCvSharp.Size(0, 0), 5, 5, BorderTypes.Default);
            var imageTarget = imageSrc.Canny(50, 200, 3, false);
            //Cv2.CvtColor(imageTarget, imageTarget, ColorConversionCodes.RGB2GRAY, 0);
            //Cv2.InRange(imageSrc, new Scalar(100, 190, 5), new Scalar(130, 255, 15), imageTarget);
            //Cv2.InRange(imageSrc, new Scalar(0, 0, 0),new Scalar(0, 0, 0), imageTarget);
            //this.pictureBox1.Image = BitmapConverter.ToBitmap(imageSrc);CV_8UC1
            //OpenCvSharp.Point[][] contours;
            //HierarchyIndex[] hierarchy;
            //Cv2.FindContours(imageTarget, out contours, out hierarchy, RetrievalModes.List, ContourApproximationModes.ApproxSimple, null);
            Mat[] contours;
            Mat hierarchy = new Mat(new OpenCvSharp.Size(imageSrc.Width, imageSrc.Height), MatType.CV_8UC1);
            Cv2.FindContours(imageTarget, out contours, hierarchy, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple, null);
            Array.Sort<Mat>(contours, new MatAreaCompareReverse<Mat>()); //按照查找出轮廓的面积倒叙排列；
            int i = 0, j = 20;

            foreach (Mat contour in contours)
            {
                var peri = contour.ArcLength(true);
                var approx = contour.ApproxPolyDP(0.02 * peri, true);

                if (approx.Rows <= 7 && contour.ContourArea(false) > 5000)
                {
                    approx.DrawContours(imageOrignal, contours, i++, Scalar.Blue, 5, LineTypes.Link8, null, 0, null);

                    Console.WriteLine(contour.ContourArea(false).ToString());
                }
            }
            Window w = new Window(WindowMode.Normal, imageOrignal);
            w.Resize(imageOrignal.Width / 2, imageOrignal.Height / 2);
            w.ShowImage(imageOrignal);
            this.pictureBox1.Image = BitmapConverter.ToBitmap(imageSrc);
            
        }

        private void QR_Find(Bitmap bitmap)
        {
            if (bitmap == null)
            {
                MessageBox.Show("无图像数据");
                this.btn_OpenFile.Focus();
                return;
            }

            var imageSrc = BitmapConverter.ToMat(bitmap);
            //var imageSrc = Cv2.ImRead(@"E:\sample\无法识别影像.bmp", ImreadModes.AnyColor);
            //var imageSrc = Cv2.ImRead(this.txtBoxFilePath.Text, ImreadModes.GrayScale);
            //Cv2.Canny(imageSrc, imageSrc, 75, 200, 3, false);
            var imageTobeFind = Cv2.ImRead(@"E:\sample\无法定位.bmp", ImreadModes.AnyColor);
            //Cv2.Canny(imageTobeFind, imageTobeFind, 75, 200, 3, false);
            //var imageTarget = new Mat(new OpenCvSharp.Size(imageSrc.Width - imageTobeFind.Width + 1, imageSrc.Height - imageTobeFind.Height + 1), MatType.CV_32FC1);
            var imageTarget = new Mat(new OpenCvSharp.Size(imageSrc.Width, imageSrc.Height), MatType.CV_32FC1);

            Cv2.MatchTemplate(imageSrc, imageTobeFind, imageTarget, TemplateMatchModes.CCoeff);
            OpenCvSharp.Point minloc, maxloc;
            Cv2.MinMaxLoc(imageTarget, out minloc, out maxloc);
            //Cv2.Rectangle(imageSrc, minloc, new OpenCvSharp.Point(minloc.X + imageTobeFind.Width, minloc.Y + imageTobeFind.Height), Scalar.Red, 5, LineTypes.Link8, 0);
            Cv2.Rectangle(imageSrc, maxloc, new OpenCvSharp.Point(maxloc.X + imageTobeFind.Width, maxloc.Y + imageTobeFind.Height), Scalar.Red, 5, LineTypes.Link8, 0);


            //Cv2.GaussianBlur(imageSrc, imageTarget, new OpenCvSharp.Size(5, 5), 0, 0, BorderTypes.Isolated);

            //var imageTarget1 = new Mat(new OpenCvSharp.Size(imageSrc.Width, imageSrc.Height), MatType.CV_64FC1);

            //Cv2.Canny(imageTarget, imageTarget, 75, 200, 3, false);
            //var origImage = new Mat();
            //image.CopyTo(origImage);
            //Cv2.GaussianBlur()
            //image = image.CvtColor(ColorConversionCodes.BGR2GRAY, 0);
            //var openCvSize = new OpenCvSharp.Size();
            //image = image.GaussianBlur(openCvSize, 5, 5, BorderTypes.Default);
            //image = image.Canny(200, 75, 3, false);
            //Cv2.ImShow("OpenCV1", image);
            this.pictureBox1.Image = BitmapConverter.ToBitmap(imageSrc);
        }
    }

    class MatAreaCompareReverse<T> : IComparer<T>
        where T : Mat
    {
        public int Compare(T x, T y)
        {
            return -x.ContourArea(false).CompareTo(y.ContourArea(false));
        }
    }
}
