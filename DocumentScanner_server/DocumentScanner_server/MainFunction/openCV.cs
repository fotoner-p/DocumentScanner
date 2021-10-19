using System;
using OpenCvSharp;

namespace DocumentScanner_server
{
    class OpenCV : IDisposable
    {
        IplImage bin;
        IplImage canny;
        IplImage dil;
        IplImage ero;
        IplImage gray;
        IplImage perspective;

        public CvPoint[] OrderPoints(IplImage src, CvPoint[] points)
        {
            CvPoint[] rect = new CvPoint[4];
            CvPoint[] win = new CvPoint[4];
            win[0].X = 0; win[0].Y = 0;
            win[1].X = 0; win[1].Y = src.Height;
            win[2].X = src.Width; win[2].Y = 0;
            win[3].X = src.Width; win[3].Y = src.Height;


            for (int i = 0; i < 4; i++)
            {
                int distance = Int32.MaxValue;
                for (int j = 0; j < 4; j++)
                {
                    if (win[i].DistanceTo(points[j]) < distance)
                    {
                        rect[i] = points[j];
                        distance = (int)win[i].DistanceTo(points[j]);
                    }
                }
            }
            return rect;
        }

        public IplImage PerspectiveTransform(IplImage src, CvPoint[] points)
        {
            perspective = new IplImage(src.Size, BitDepth.U8, 3);

            float width = src.Size.Width;
            float height = src.Size.Height;

            CvPoint2D32f[] srcPoint = new CvPoint2D32f[4];
            CvPoint2D32f[] dstPoint = new CvPoint2D32f[4];

            srcPoint[0] = new CvPoint2D32f(points[0].X, points[0].Y);
            srcPoint[1] = new CvPoint2D32f(points[1].X, points[1].Y);
            srcPoint[2] = new CvPoint2D32f(points[2].X, points[2].Y);
            srcPoint[3] = new CvPoint2D32f(points[3].X, points[3].Y);

            dstPoint[0] = new CvPoint2D32f(0.0f, 0.0f);
            dstPoint[1] = new CvPoint2D32f(0.0f, height);
            dstPoint[2] = new CvPoint2D32f(width, 0.0f);
            dstPoint[3] = new CvPoint2D32f(width, height);

            CvMat mapMatrix = Cv.GetPerspectiveTransform(srcPoint, dstPoint);
            Cv.WarpPerspective(src, perspective, mapMatrix, Interpolation.Linear, CvScalar.ScalarAll(0));

            return perspective;
        }

        public IplImage Binary(IplImage src)
        {
            bin = new IplImage(src.Size, BitDepth.U8, 1);
            Cv.CvtColor(src, bin, ColorConversion.RgbToGray);
            Cv.Threshold(bin, bin, 0, 255, ThresholdType.Binary);
            return bin;
        }
        public IplImage DilateImage(IplImage src)
        {
            dil = new IplImage(src.Size, BitDepth.U8, 1);

            IplConvKernel element = new IplConvKernel(4, 4, 2, 2, ElementShape.Custom, new int[3, 3]);
            Cv.Dilate(src, dil, element, 3);
            return dil;
        }

        public IplImage ErodeImage(IplImage src)
        {
            ero = new IplImage(src.Size, BitDepth.U8, 1);

            IplConvKernel element = new IplConvKernel(4, 4, 2, 2, ElementShape.Custom, new int[3, 3]);
            Cv.Erode(src, ero, element, 3);
            return ero;
        }

        public IplImage CannyEdge(IplImage src)
        {
            canny = new IplImage(src.Size, BitDepth.U8, 1);
            Cv.Canny(src, canny, 25, 200);
            return canny;
        }

        public IplImage GrayScale(IplImage src)
        {
            gray = new IplImage(src.Size, BitDepth.U8, 1);
            Cv.CvtColor(src, gray, ColorConversion.BgrToGray);
            return gray;
        }
        
        public void Dispose()
        {
            if (canny != null) Cv.ReleaseImage(canny);
            if (dil != null) Cv.ReleaseImage(dil);
            if (bin != null) Cv.ReleaseImage(bin);
            if (gray != null) Cv.ReleaseImage(gray);
            if (ero != null) Cv.ReleaseImage(ero);
        }
    }
}
