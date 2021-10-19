using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Widget;
using Android.Graphics;
using Android.Support.V4.Content;
using Android.Media;
using Android.Database;
using Java.Text;
using Java.Util;
using Java.IO;

namespace DocumentScanner
{
    [Activity(Label = "CameraActivity" , Theme = "@style/AppTheme", ParentActivity = typeof(MainActivity))] //Theme = "@style/AppTheme",
    public class CameraActivity : Activity
    {
        private Android.Net.Uri photoUri;
        private string curPhotoPath;
        private string mImageCaptureName;
        ImageView image;


        private string imagePath;
        private const int GALLERY_CODE = 1112;
        private const int CAMERA_CODE = 1111;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.camera_layout);


            image = FindViewById<ImageView>(Resource.Id.imgView);

            string flag = Intent.GetStringExtra("flag");

            if(flag == "1")
            {
                cameraOpen();
            }
            else if(flag == "2")
            {
                selectGallery();
            }

            FindViewById<Button>(Resource.Id.capImgBtn).Click += (sender, e) => {
                flag = "1";
                cameraOpen();
            };

            FindViewById<Button>(Resource.Id.loadBtn).Click += (sender, e) => {
                flag = "2";
                selectGallery();
            };

            FindViewById<Button>(Resource.Id.sendBtn).Click += (sender, e) => {
                if (flag == "1")
                {
                    Intent intent = new Intent(this, typeof(ResizeActivity));
                    intent.PutExtra("imagePath", curPhotoPath);
                    StartActivity(intent);
                }
                
                else if (flag == "2")
                {
                    Intent intent = new Intent(this, typeof(ResizeActivity));
                    intent.PutExtra("imagePath", imagePath);
                    StartActivity(intent);
                }
            };
        }

        private void cameraOpen()
        {
            string state = Android.OS.Environment.ExternalStorageState;

            if (Android.OS.Environment.MediaMounted.Equals(state))
            {
                Intent i = new Intent(MediaStore.ActionImageCapture);
                if (i.ResolveActivity(PackageManager) != null)
                {
                    File photoFile = null;

                    try
                    {
                        photoFile = createImageFile();
                    }
                    catch (IOException ex)
                    {

                    }

                    if (photoFile != null)
                    {
                        if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
                            photoUri = FileProvider.GetUriForFile(ApplicationContext, ApplicationContext.PackageName + ".fileprovider", photoFile);

                        else
                            photoUri = Android.Net.Uri.FromFile(photoFile);

                        i.PutExtra(MediaStore.ExtraOutput, photoUri);
                        StartActivityForResult(i, CAMERA_CODE);
                    }
                }
            }
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if(resultCode == Result.Ok)
            {
                switch (requestCode)
                {
                    case GALLERY_CODE:
                        sendPicture(data.Data);
                        break;
                    case CAMERA_CODE:
                        getPictureForPhoto();
                        break;
                    default:
                        break;
                }
            }
        }

        private File createImageFile()
        {
            try
            {
                File dir = new File(Android.OS.Environment.ExternalStorageDirectory + "/path/");

                if (!dir.Exists())
                    dir.Mkdir();

                string timeStamp = new SimpleDateFormat("yyyyMMdd_HHmmss").Format(new Date());
                mImageCaptureName = timeStamp + ".jpg";

                File storageDir = new File(Android.OS.Environment.ExternalStorageDirectory.AbsoluteFile + "/path/" + mImageCaptureName);

                curPhotoPath = storageDir.AbsolutePath;

                return storageDir;
            }
            catch
            {
                throw new IOException();
            }
        }

        private void getPictureForPhoto()
        {
            Bitmap bitmap = BitmapFactory.DecodeFile(curPhotoPath);
            ExifInterface exif = null;
            try
            {
                exif = new ExifInterface(curPhotoPath);
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
                exifDegree = ExifOrientationToDegrees(exifOrientation);
            }
            else
            {
                exifDegree = 0;
            }

            image.SetImageBitmap(rotate(bitmap, exifDegree));//이미지 뷰에 비트맵 넣기
        }

        private void sendPicture(Android.Net.Uri imgUri)
        {
            imagePath = getRealPathFromURI(imgUri); // path 경로
            ExifInterface exif = null;
            try
            {
                exif = new ExifInterface(imagePath);
            }
            catch (IOException e)
            {
                e.PrintStackTrace();
            }
            int exifOrientation = exif.GetAttributeInt(ExifInterface.TagOrientation, 1);//ExifInterface.ORIENTATION_NORMAL);
            int exifDegree = ExifOrientationToDegrees(exifOrientation);

            Bitmap bitmap = BitmapFactory.DecodeFile(imagePath);//경로를 통해 비트맵으로 전환

            image.SetImageBitmap(rotate(bitmap, exifDegree));//이미지 뷰에 비트맵 넣기
        }

        public static int ExifOrientationToDegrees(int exifOrientation)
        {
            if (exifOrientation == 6) //ExifInterface.ORIENTATION_ROTATE_90
            {
                return 90;
            }
            else if (exifOrientation == 3) //ExifInterface.ORIENTATION_ROTATE_180)
            {
                return 180;
            }
            else if (exifOrientation == 8) //ExifInterface.ORIENTATION_ROTATE_270
            {
                return 270;
            }
            return 0;
        }

        public static Bitmap rotate(Bitmap src, float degree)
        {
            // Matrix 객체 생성
            Matrix matrix = new Matrix();
            // 회전 각도 셋팅
            matrix.PostRotate(degree);
            // 이미지와 Matrix 를 셋팅해서 Bitmap 객체 생성
            return Bitmap.CreateBitmap(src, 0, 0, src.Width, src.Height, matrix, true);
        }

        private string getRealPathFromURI(Android.Net.Uri contentUri)
        {
            int column_index = 0;
            string[] proj = { MediaStore.Images.Media.InterfaceConsts.Data};
            ICursor cursor = ContentResolver.Query(contentUri, proj, null, null, null);
            if (cursor.MoveToFirst())
            {
                column_index = cursor.GetColumnIndexOrThrow(MediaStore.Images.Media.InterfaceConsts.Data);
            }

            string result = cursor.GetString(column_index);
            cursor.Close();

            return result;
        }

        private void selectGallery()
        {
            Intent i = new Intent(Intent.ActionPick);
            i.SetData(MediaStore.Images.Media.ExternalContentUri);
            i.SetType("image/*");
            StartActivityForResult(i, GALLERY_CODE);
        }
    }
}