using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.IO;
using static Android.App.ActionBar;

namespace DocumentScanner
{
    [Activity(Label = "ResizeActivity", Theme = "@style/ResizeTheme", ParentActivity = typeof(CameraActivity))]
    public class ResizeActivity : Activity
    {
        private PosView view;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Create your application here

            string imagePath = Intent.GetStringExtra("imagePath");

            view = new PosView(this, imagePath);
            
            SetContentView(view);

            LinearLayout linear = new LinearLayout(this);
            linear.Orientation = Android.Widget.Orientation.Horizontal;
            linear.WeightSum = 2;
            Button okBtn = new Button(this);
            okBtn.Text = "확인";

            LinearLayout.LayoutParams pm = new LinearLayout.LayoutParams(LayoutParams.WrapContent, 150);
            pm.Gravity = GravityFlags.Bottom;
            pm.Weight = 1;

            okBtn.LayoutParameters = pm;

            okBtn.Click += (s, e) => {
                Intent intent = new Intent(this, typeof(ResultActivity));
                intent.PutExtra("imagePath", imagePath);

                string[] arr = new string[8];
                ValueTuple<int, int> result; 
                for (int i = 0; i < 4; i++)
                {
                    result = view.relativePos[i];
                    arr[i] = result.Item1.ToString();
                    arr[i + 1] = result.Item2.ToString();
                }

                intent.PutStringArrayListExtra("pos", arr);

                StartActivity(intent);
            };

            Button backBtn = new Button(this);
            backBtn.Text = "취소";

            backBtn.LayoutParameters = pm;

            backBtn.Click += (s, e) => {
                this.Finish();
            };
            
            linear.AddView(okBtn);
            linear.AddView(backBtn);

            view.AddView(linear);
        }
    }

    public class PosView : ScrollView
    {
        private ValueTuple<int, int>[] originPos;
        public ValueTuple<int, int>[] relativePos;
        private Paint paint;
        public double ratio;

        private bool drawingFlag;
        private bool firstLoad;
        private Bitmap img;
        private Bitmap resizeBitmap;
        private int drawingIdx;

        public PosView(Context context) : base(context) { }

        public PosView(Context context, string imagePath) : base(context)
        {
            int count = 0;
            do {
                ImageTransfer transfer = new ImageTransfer("155.230.249.242", 11000, imagePath);

                originPos = transfer.LoadPos();

                if (originPos.Length == 4)
                    break;

                count++;
            } while (count != 3);

            paint = new Paint();
            paint.Color = Color.Green;

            ExifInterface exif = null;
            try
            {
                exif = new ExifInterface(imagePath);
            }
            catch (IOException e)
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

            img = CameraActivity.rotate(BitmapFactory.DecodeFile(imagePath), exifDegree);

            firstLoad = true;

            this.Touch += (s, e) =>
            {
                TouchEvent(e);
            };
            drawingFlag = false;
            Invalidate();
        }

        private void TouchEvent(TouchEventArgs e)
        {
            MotionEventActions action = e.Event.Action;

            switch (action)
            {
                case MotionEventActions.Down:
                    for (int i = 0; i < 4; i++)
                    {
                        int xLen = (int)Math.Pow(e.Event.GetX() - relativePos[i].Item1, 2);
                        int yLen = (int)Math.Pow(e.Event.GetY() - relativePos[i].Item2, 2);

                        if (xLen + yLen < (int)Math.Pow(30, 2))
                        {
                            drawingFlag = true;
                            drawingIdx = i;
                            break;
                        }
                    }
                    break;

                case MotionEventActions.Move:
                    if (drawingFlag)
                    {
                        relativePos[drawingIdx].Item1 = (int)e.Event.GetX();
                        relativePos[drawingIdx].Item2 = (int)e.Event.GetY();
                        Invalidate();
                    }
                    break;

                case MotionEventActions.Up:
                    drawingFlag = false;
                    break;
            }
        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);

            double ratio = canvas.Width / (double)img.Width;

            if(firstLoad)
                resizeBitmap = Bitmap.CreateScaledBitmap(img, canvas.Width, (int)(ratio * img.Height), true);

            canvas.DrawBitmap(resizeBitmap, 0, 150, null);

            if (firstLoad)
            {
                if (originPos.Length != 4)
                {
                    originPos = new ValueTuple<int, int>[4];

                    originPos[0] = new ValueTuple<int, int>(50, 50);
                    originPos[1] = new ValueTuple<int, int>(img.Width - 50, 50);
                    originPos[3] = new ValueTuple<int, int>(50, img.Height - 50);
                    originPos[2] = new ValueTuple<int, int>(img.Width - 50, img.Height - 50);
                }

                relativePos = new ValueTuple<int, int>[4];
                for (int i = 0; i < 4; i++)
                {
                    relativePos[i] = new ValueTuple<int, int>((int)(originPos[i].Item1 * ratio), (int)(originPos[i].Item2 * ratio) + 150);
                    firstLoad = false;
                }
            }

            var linePaint = new Paint();
            linePaint.Color = Color.Aqua;
            linePaint.SetStyle(Paint.Style.Stroke);
            linePaint.StrokeWidth = 8f;

            var pt0 = relativePos.Last();

            foreach (var pt in relativePos)
            {
                canvas.DrawLine(pt0.Item1, pt0.Item2, pt.Item1, pt.Item2, linePaint);

                pt0 = pt;
            }

            for (int i = 0; i < 4; i++)
            {
                canvas.DrawCircle(relativePos[i].Item1, relativePos[i].Item2, 30, paint);
            }
        }
    }
}