using System.Drawing;
using System.IO;
using Shapes;
using UnityEngine;
using File = UnityEngine.Windows.File;
using Rectangle = Shapes.Rectangle;

public class Main : MonoBehaviour
{
    public string PhotoFolderPath;
        
    void Start()
    {
        DirectoryInfo root = new DirectoryInfo(PhotoFolderPath);
        int count = 0;
        foreach (FileInfo file in root.EnumerateFiles("*.jpg"))
        {
            Rectangle photo = Instantiate(NewCube.textureRectPoly, transform, false);
            photo.gameObject.SetActive(true);
            photo.gameObject.name = file.FullName;
            
            Bitmap bitmap = new Bitmap(file.FullName);

            byte[] bytes = File.ReadAllBytes(file.FullName);
            photo.LoadTexture(bytes);
                
            photo.transform.Translate(Vector3.down * count *.01f);
            photo.transform.localScale = new Vector3(bitmap.Width, 1, bitmap.Height) * .001f;
            count++;
        }
    }
}