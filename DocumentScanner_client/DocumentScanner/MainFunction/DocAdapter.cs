using Android.Content;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;
using Java.Lang;
using System.Collections.Generic;

namespace DocumentScanner
{
    
    public class DocAdapter : BaseAdapter
    {
        private List<ListViewItem> itemList = new List<ListViewItem>();

        public DocAdapter()
        {

        }

        public override int Count => itemList.Count;

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            int pos = position;
            Context context = parent.Context;

            if(convertView == null)
            {
                LayoutInflater inflater = (LayoutInflater) context.GetSystemService(Context.LayoutInflaterService);
                convertView = inflater.Inflate(Resource.Layout.item_doc, parent, false);
            }

            TextView date = convertView.FindViewById<TextView>(Resource.Id.dateText);
            TextView name = convertView.FindViewById<TextView>(Resource.Id.nameText);

            ListViewItem listViewItem = itemList[pos];
            string str = listViewItem.getTitle();
            char[] arr = str.ToCharArray();

            date.SetText(arr, 0, arr.Length);

            return convertView;
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override Object GetItem(int position)
        {
            return itemList[position];
        }

        public void addItem(string title)
        {
            ListViewItem item = new ListViewItem();
            item.setTitle(title);

            itemList.Add(item);
        }
    }

    public class ListViewItem : Object
    {
        private Drawable iconDrawable;
        private string titleStr;
        private string descStr;

        public void setIcon(Drawable icon)
        {
            iconDrawable = icon;
        }
        public void setTitle(string title)
        {
            titleStr = title;
        }
        public void setDesc(string desc)
        {
            descStr = desc;
        }

        public Drawable getIcon()
        {
            return this.iconDrawable;
        }
        public string getTitle()
        {
            return this.titleStr;
        }
        public string getDesc()
        {
            return this.descStr;
        }
    }    
}