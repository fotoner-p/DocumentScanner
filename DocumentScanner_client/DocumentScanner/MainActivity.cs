using Android.App;
using Android.OS;
using Android.Widget;
using Android.Content;
using Android.Views;
using System.Collections.Generic;
using System.IO;

namespace DocumentScanner
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)] // Theme = "@style/AppTheme",
    public class MainActivity : Activity
    {

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            ListView listView;
            DocAdapter adapter = new DocAdapter();

            listView = FindViewById<ListView>(Resource.Id.listView);
            listView.Adapter = adapter;
            /*
            string[] lines = File.ReadAllLines("log.txt");

            foreach(var str in lines)
            {
                adapter.addItem(str);
            }*/

            adapter.addItem("adsf"); adapter.addItem("asdasdf"); adapter.addItem("asdfasdf");
        }
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater menuInflater = MenuInflater;
            menuInflater.Inflate(Resource.Menu.menu, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            Intent intent = new Intent(this, typeof(CameraActivity));

            switch (item.ItemId)
            {
                case Resource.Id.action_settings1:
                    intent.PutExtra("flag", "1");
                    StartActivity(intent);
                    break;
                case Resource.Id.action_settings2:
                    intent.PutExtra("flag", "2");
                    StartActivity(intent);
                    break;
            }

            return base.OnOptionsItemSelected(item);
        }
    }
}