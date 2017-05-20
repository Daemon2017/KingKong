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
        private bool DetectBarcode(Bitmap startImg)
        {
            OpenCvSharp.Point[][] contours;

            using (Mat imgForOperations = new Mat())
            {
                Cv2.CvtColor(BitmapConverter.ToMat(startImg),
                             imgForOperations,
                             ColorConversionCodes.BGR2GRAY);

                Scalar imgMean = Cv2.Mean(imgForOperations);

                using (Mat gradX = new Mat(),
                           gradY = new Mat())
                {
                    Cv2.Sobel(imgForOperations,
                              gradX,
                              MatType.CV_8U,
                              1,
                              0);

                    Cv2.Sobel(imgForOperations,
                              gradY,
                              MatType.CV_8U,
                              0,
                              1);

                    Cv2.Subtract(gradX,
                                 gradY,
                                 imgForOperations);
                }

                Cv2.ConvertScaleAbs(imgForOperations,
                                    imgForOperations);

                OpenCvSharp.Size brushSize = new OpenCvSharp.Size(9, 9);
                Cv2.Blur(imgForOperations,
                         imgForOperations,
                         brushSize);

                Cv2.Threshold(imgForOperations,
                              imgForOperations,
                              (int)(imgMean[0] * 0.65),
                              255,
                              ThresholdTypes.Binary);

                brushSize = new OpenCvSharp.Size(21, 7);

                using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect,
                                                       brushSize))
                {
                    Cv2.MorphologyEx(imgForOperations,
                                     imgForOperations,
                                     MorphTypes.Close,
                                     kernel);
                }

                Cv2.Erode(imgForOperations,
                          imgForOperations,
                          null,
                          null,
                          5);
                Cv2.Dilate(imgForOperations, imgForOperations, null, null, 5);

                HierarchyIndex[] hierarchy;
                Cv2.FindContours(imgForOperations,
                                 out contours,
                                 out hierarchy,
                                 RetrievalModes.External,
                                 ContourApproximationModes.ApproxSimple);
            }

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

            GC.Collect();

            if (contours.Length != 0)
            {
                HierarchyIndex[] myHierarchy = new HierarchyIndex[1];
                myHierarchy[0] = new HierarchyIndex();
                myHierarchy[0].Child = -1;
                myHierarchy[0].Next = 1;
                myHierarchy[0].Parent = -1;
                myHierarchy[0].Previous = -1;

                OpenCvSharp.Point[][] smoothedContour = SmoothContour(contours, largestContourIndex);

                int horizontalSize = Math.Abs(smoothedContour[0][0].X - smoothedContour[0][3].X);
                int verticalSize = Math.Abs(smoothedContour[0][0].Y - smoothedContour[0][1].Y);

                if (horizontalSize >= 100 && verticalSize >= 50)
                {
                    using (Mat finalImg = BitmapConverter.ToMat(startImg))
                    {
                        Cv2.DrawContours(finalImg,
                                         smoothedContour,
                                         0,
                                         new Scalar(0, 255, 0),
                                         2,
                                         LineTypes.Link8,
                                         myHierarchy,
                                         int.MaxValue);

                        pictureBox1.Image = BitmapConverter.ToBitmap(finalImg);
                    }

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

        private OpenCvSharp.Point[][] SmoothContour(OpenCvSharp.Point[][] contours, int largestContourIndex)
        {
            OpenCvSharp.Point[][] smoothedContour;
            smoothedContour = new OpenCvSharp.Point[1][];
            smoothedContour[0] = new OpenCvSharp.Point[4];

            smoothedContour[0][0].X = Convert.ToInt32(contours[largestContourIndex][0].X);
            smoothedContour[0][0].Y = Convert.ToInt32(contours[largestContourIndex][0].Y);

            smoothedContour[0][1].X = Convert.ToInt32(contours[largestContourIndex][0].X);
            smoothedContour[0][1].Y = Convert.ToInt32(contours[largestContourIndex][0].Y);

            smoothedContour[0][2].X = Convert.ToInt32(contours[largestContourIndex][0].X);
            smoothedContour[0][2].Y = Convert.ToInt32(contours[largestContourIndex][0].Y);

            smoothedContour[0][3].X = Convert.ToInt32(contours[largestContourIndex][0].X);
            smoothedContour[0][3].Y = Convert.ToInt32(contours[largestContourIndex][0].Y);

            for (int i = 0; i < contours[largestContourIndex].Length; i++)
            {
                if (contours[largestContourIndex][i].X < smoothedContour[0][0].X)
                {
                    smoothedContour[0][0].X = Convert.ToInt32(contours[largestContourIndex][i].X);
                }

                if (contours[largestContourIndex][i].Y < smoothedContour[0][0].Y)
                {
                    smoothedContour[0][0].Y = Convert.ToInt32(contours[largestContourIndex][i].Y);
                }

                if (contours[largestContourIndex][i].X < smoothedContour[0][1].X)
                {
                    smoothedContour[0][1].X = Convert.ToInt32(contours[largestContourIndex][i].X);
                }

                if (contours[largestContourIndex][i].Y > smoothedContour[0][1].Y)
                {
                    smoothedContour[0][1].Y = Convert.ToInt32(contours[largestContourIndex][i].Y);
                }

                if (contours[largestContourIndex][i].X > smoothedContour[0][2].X)
                {
                    smoothedContour[0][2].X = Convert.ToInt32(contours[largestContourIndex][i].X);
                }

                if (contours[largestContourIndex][i].Y > smoothedContour[0][2].Y)
                {
                    smoothedContour[0][2].Y = Convert.ToInt32(contours[largestContourIndex][i].Y);
                }

                if (contours[largestContourIndex][i].X > smoothedContour[0][3].X)
                {
                    smoothedContour[0][3].X = Convert.ToInt32(contours[largestContourIndex][i].X);
                }

                if (contours[largestContourIndex][i].Y < smoothedContour[0][3].Y)
                {
                    smoothedContour[0][3].Y = Convert.ToInt32(contours[largestContourIndex][i].Y);
                }
            }

            return smoothedContour;
        }

        private void DecodeBarcode(Bitmap img)
        {
            string[] results = BarcodeReader.read(img, BarcodeReader.EAN13);

            if (results != null)
            {
                var sb = new StringBuilder(results[0]);
                string changer = "4";
                var temp = changer.ToCharArray(0, 1);
                sb[0] = temp[0];
                results[0] = sb.ToString();

                Connect(results[0], workMode);

                locked = true;
                lockedFrames = 0;
            }
        }

        private void Connect(string barcode, string mode)
        {
            client.SendMessage(username + "@" + hostname, barcode + "," + mode);
        }
    }
}
