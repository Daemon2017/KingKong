using System.Drawing;
using BarcodeLib.BarcodeReader;
using System.Text;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace roadTrack
{
    public partial class Form1
    {
        private Image DetectBarcode(Bitmap inputImage)
        {
            Mat img = BitmapConverter.ToMat(inputImage);
            Mat imgGray = new Mat();
            Cv2.CvtColor(img,
                         imgGray,
                         ColorConversionCodes.BGR2GRAY);

            Scalar imgMean = Cv2.Mean(imgGray);

            Mat gradX = new Mat();
            Cv2.Sobel(imgGray,
                      gradX,
                      MatType.CV_8U,
                      1,
                      0);
            Mat gradY = new Mat();
            Cv2.Sobel(imgGray,
                      gradY,
                      MatType.CV_8U,
                      0,
                      1);

            Mat gradient = new Mat();
            Cv2.Subtract(gradX,
                         gradY,
                         gradient);
            Cv2.ConvertScaleAbs(gradient,
                                gradient);

            Mat blured = new Mat();
            OpenCvSharp.Size s = new OpenCvSharp.Size(7, 7);
            Cv2.Blur(gradient,
                     blured,
                     s);
            Mat treshed = new Mat();
            Cv2.Threshold(blured,
                          treshed,
                          (int)(imgMean[0] * 0.65),
                          255,
                          ThresholdTypes.Binary);

            s = new OpenCvSharp.Size(21, 7);
            Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect,
                                                   s);
            Mat closed = new Mat();
            Cv2.MorphologyEx(treshed,
                             closed,
                             MorphTypes.Close,
                             kernel);

            Cv2.Erode(closed,
                      closed,
                      null,
                      null,
                      5);
            Cv2.Dilate(closed, closed, null, null, 5);

            OpenCvSharp.Point[][] contours;
            HierarchyIndex[] h;
            Cv2.FindContours(closed,
                             out contours,
                             out h,
                             RetrievalModes.External,
                             ContourApproximationModes.ApproxSimple);

            double largestArea = 0;
            int largestContourIndex = 0;
            Rect rect = new Rect();

            for (int i = 0; i < contours.Length; i++)
            {
                double area = Cv2.ContourArea(contours[i]);

                if (area > largestArea)
                {
                    largestArea = area;
                    largestContourIndex = i;
                    rect = Cv2.BoundingRect(contours[i]);
                }
            }

            Mat final = BitmapConverter.ToMat(inputImage);

            Cv2.DrawContours(final,
                             contours,
                             largestContourIndex,
                             new Scalar(0, 255, 0),
                             2,
                             LineTypes.Link8,
                             h,
                             int.MaxValue);

            return BitmapConverter.ToBitmap(final);
        }


        private void DecodeBarcode(Bitmap image)
        {
            string[] results = BarcodeReader.read(image, BarcodeReader.EAN13);

            if (results != null)
            {
                var sb = new StringBuilder(results[0]);
                string changer = "4";
                var temp = changer.ToCharArray(0, 1);
                sb[0] = temp[0];
                results[0] = sb.ToString();

                Connect(results[0], workMode);

                locked = true;
            }
        }

        private void Connect(string barcode, string mode)
        {
            client.SendMessage(username + "@" + hostname, barcode + "," + mode);
        }
    }
}
