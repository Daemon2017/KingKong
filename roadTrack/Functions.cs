using System.Drawing;
using BarcodeLib.BarcodeReader;
using System.Text;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;

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
            OpenCvSharp.Size brushSize = new OpenCvSharp.Size(9, 9);
            Cv2.Blur(gradient,
                     blured,
                     brushSize);
            Mat treshed = new Mat();
            Cv2.Threshold(blured,
                          treshed,
                          (int)(imgMean[0] * 0.65),
                          255,
                          ThresholdTypes.Binary);

            brushSize = new OpenCvSharp.Size(21, 7);
            Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect,
                                                   brushSize);
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
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(closed,
                             out contours,
                             out hierarchy,
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

            if (contours.Length > 0)
            {
                OpenCvSharp.Point[][] big;
                big = new OpenCvSharp.Point[1][];
                big[0] = new OpenCvSharp.Point[4];

                big[0][0].X = Convert.ToInt32(contours[largestContourIndex][0].X);
                big[0][0].Y = Convert.ToInt32(contours[largestContourIndex][0].Y);

                big[0][1].X = Convert.ToInt32(contours[largestContourIndex][0].X);
                big[0][1].Y = Convert.ToInt32(contours[largestContourIndex][0].Y);

                big[0][2].X = Convert.ToInt32(contours[largestContourIndex][0].X);
                big[0][2].Y = Convert.ToInt32(contours[largestContourIndex][0].Y);

                big[0][3].X = Convert.ToInt32(contours[largestContourIndex][0].X);
                big[0][3].Y = Convert.ToInt32(contours[largestContourIndex][0].Y);

                for (int i = 0; i < contours[largestContourIndex].Length; i++)
                {
                    if (contours[largestContourIndex][i].X < big[0][0].X)
                    {
                        big[0][0].X = Convert.ToInt32(contours[largestContourIndex][i].X);
                    }

                    if (contours[largestContourIndex][i].Y < big[0][0].Y)
                    {
                        big[0][0].Y = Convert.ToInt32(contours[largestContourIndex][i].Y);
                    }

                    if (contours[largestContourIndex][i].X < big[0][1].X)
                    {
                        big[0][1].X = Convert.ToInt32(contours[largestContourIndex][i].X);
                    }

                    if (contours[largestContourIndex][i].Y > big[0][1].Y)
                    {
                        big[0][1].Y = Convert.ToInt32(contours[largestContourIndex][i].Y);
                    }

                    if (contours[largestContourIndex][i].X > big[0][2].X)
                    {
                        big[0][2].X = Convert.ToInt32(contours[largestContourIndex][i].X);
                    }

                    if (contours[largestContourIndex][i].Y > big[0][2].Y)
                    {
                        big[0][2].Y = Convert.ToInt32(contours[largestContourIndex][i].Y);
                    }

                    if (contours[largestContourIndex][i].X > big[0][3].X)
                    {
                        big[0][3].X = Convert.ToInt32(contours[largestContourIndex][i].X);
                    }

                    if (contours[largestContourIndex][i].Y < big[0][3].Y)
                    {
                        big[0][3].Y = Convert.ToInt32(contours[largestContourIndex][i].Y);
                    }
                }

                verticalSize = Math.Abs(big[0][0].Y - big[0][1].Y);
                horizontalSize = Math.Abs(big[0][0].X - big[0][3].X);

                HierarchyIndex[] myHierarchy = new HierarchyIndex[1];
                myHierarchy[0] = new HierarchyIndex();
                myHierarchy[0].Child = -1;
                myHierarchy[0].Next = 1;
                myHierarchy[0].Parent = -1;
                myHierarchy[0].Previous = -1;

                Mat final = BitmapConverter.ToMat(inputImage);
                Cv2.DrawContours(final,
                                 big,
                                 0,                                 
                                 new Scalar(0, 255, 0),
                                 2,
                                 LineTypes.Link8,
                                 myHierarchy,
                                 int.MaxValue);                                 

                return BitmapConverter.ToBitmap(final);
            }

            return BitmapConverter.ToBitmap(img);
        }

        int horizontalSize = 0,
            verticalSize = 0;

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
