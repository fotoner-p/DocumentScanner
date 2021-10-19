using Google.Cloud.Vision.V1;
using System.Collections.Generic;

namespace DocumentScanner_server
{
    class GoogleOcr
    {
        public static LinkedList<string> readingText(string path)
        {
            var gimage = Image.FromFile("./" + path);
            var client = ImageAnnotatorClient.Create();
            var response = client.DetectText(gimage);
            LinkedList<string> str = new LinkedList<string>();

            foreach (var annotation in response)
            {
                if (annotation.Description != null)
                    str.AddLast(annotation.Description);
            }

            return str;
        }
    }
}
