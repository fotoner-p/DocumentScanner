using System;
using Android.App;
using Android.Content;
using Android.OS;
using System.IO;
using Android.Widget;
using System.Threading;
using Android.Icu.Text;
using Java.Util;
using Android.Graphics;
using Android.Media;
using System.Linq;

namespace DocumentScanner
{
    [Activity(Label = "ResultActivity", Theme = "@style/AppTheme", ParentActivity = typeof(MainActivity))]
    public class ResultActivity : Activity
    {
        private ValueTuple<int, int>[] positions;
        private string imgPath;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.result_layout);

            imgPath = Intent.GetStringExtra("imagePath");

            positions = new ValueTuple<int, int>[4];

            string[] pos = Intent.GetStringArrayExtra("pos");


            TextView[] viewArr = new TextView[4];

            viewArr[0] = FindViewById<TextView>(Resource.Id.pos1);
            viewArr[1] = FindViewById<TextView>(Resource.Id.pos2);
            viewArr[2] = FindViewById<TextView>(Resource.Id.pos3);
            viewArr[3] = FindViewById<TextView>(Resource.Id.pos4);

            for (int i = 0; i < 4; i++)
            {
                positions[i] = new ValueTuple<int, int>(Int32.Parse(pos[i]), Int32.Parse(pos[i + 1]));
                char[] str = positions[i].ToString().ToCharArray();
                viewArr[i].SetText(str, 0, str.Length);
            }

            ImageTransfer transfer = new ImageTransfer("155.230.249.242", 11000, imgPath);

            ExifInterface exif = null;
            try
            {
                exif = new ExifInterface(imgPath);
            }
            catch (Java.IO.IOException e)
            {
                e.PrintStackTrace();
            }

            int exifOrientation;
            int exifDegree;

            if (exif != null)
            {
                exifOrientation = exif.GetAttributeInt(ExifInterface.TagOrientation, 1);//ExifInterface.ORIENTATION_NORMAL);
                exifDegree = CameraActivity.ExifOrientationToDegrees(exifOrientation);
            }
            else
            {
                exifDegree = 0;
            }
            ImageView view = FindViewById<ImageView>(Resource.Id.resultView);
            
            Bitmap img = CameraActivity.rotate(BitmapFactory.DecodeFile(imgPath), exifDegree);

            double ratio = view.Width / (double)img.Width;
            Bitmap resizeBitmap = Bitmap.CreateScaledBitmap(img, view.Width, (int)(ratio * img.Height), true);

            Canvas c = new Canvas(resizeBitmap);
            var pt0 = positions.Last();

            var linePaint = new Paint();
            linePaint.Color = Color.Aqua;
            linePaint.SetStyle(Paint.Style.Stroke);
            linePaint.StrokeWidth = 8f;

            foreach (var pt in positions)
            {
                c.DrawLine(pt0.Item1, pt0.Item2, pt.Item1, pt.Item2, linePaint);

                pt0 = pt;
            }

            view.SetImageBitmap(resizeBitmap);

            FindViewById<Button>(Resource.Id.ocrBtn).Click += (sender, e) =>
            {
                string[] arr = new string[0];

                Thread t1 = new Thread(() =>
                {
                    arr = transfer.LoadOCR();
                });
                t1.Start();

                string str = "";

                t1.Join();

                for (int i = 0; i < arr.Length; i++)
                    str += arr[i] + "\n";

                char[] result = str.ToCharArray();
                FindViewById<TextView>(Resource.Id.ocrText).SetText(result, 0, result.Length);
            };
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
        private void save()
        {/*
            string time = new SimpleDateFormat("yyyyMMdd_HHmmss").Format(new Date());
            using (StreamWriter file = new StreamWriter("./save/" +time + ".txt"))
            {
                file.WriteLine(imgPath);

                for(int i = 0; i < 4; i++)
                {
                    file.WriteLine(positions[i].ToString());
                }
                if(FindViewById<TextView>(Resource.Id.ocrText).Text != "")
                {
                    file.WriteLine(FindViewById<TextView>(Resource.Id.ocrText).Text);
                }
            }
            using (StreamWriter file = new StreamWriter("./save/log.txt",true))
            {
                file.WriteLine(time + ".txt");
            }*/

            Finish();
        }
    }
}