using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Newtonsoft.Json;
using Shapes;
using UnityEngine;
using UnityEngine.InputSystem;
using File = UnityEngine.Windows.File;
using Rectangle = Shapes.Rectangle;

public class Main : MonoBehaviour
{
    public string PhotoFolderPath;
    public float SceneLongitude;
    public float ScneneAltitude;
    public float SceneLatitude;

    Vector3 sceneOffset => new(SceneLongitude, ScneneAltitude, SceneLatitude);

    string jsonPath => Path.Combine(PhotoFolderPath, "positions.json");

    readonly Dictionary<string, GameObject> pics = new();
    readonly Dictionary<string, DateTime> picsByDate = new();

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

            photo.transform.Translate(Vector3.down * count * .01f);
            photo.transform.localScale = new Vector3(bitmap.Width, 1, bitmap.Height) * .001f;

            photo.AddCollider();

            pics.Add(file.FullName, photo.gameObject);
            picsByDate.Add(file.FullName, file.CreationTime.ToUniversalTime());
            count++;
        }

        if (File.Exists(jsonPath))
        {
            string json = System.IO.File.ReadAllText(jsonPath);
            Dictionary<string, PositionAndRotation> fromJsonPics =
                JsonConvert.DeserializeObject<Dictionary<string, PositionAndRotation>>(json);

            foreach (KeyValuePair<string, PositionAndRotation> positionAndRotation in fromJsonPics)
            {
                pics[positionAndRotation.Key].gameObject.transform.localPosition =
                    positionAndRotation.Value.positionVector3;
                pics[positionAndRotation.Key].gameObject.transform.localRotation =
                    positionAndRotation.Value.rotationQuaternion;
            }
        }
    }

    void Update()
    {
        if (Keyboard.current.f1Key.wasPressedThisFrame)
        {
            Serializer serializer = new Serializer(jsonPath);
            serializer.SerializeCartesian(pics);
        }

        if (Keyboard.current.f2Key.wasPressedThisFrame)
        {
            // get exiftool here: https://exiftool.org/
            // run this command
            // perl C:\Image-ExifTool-12.54\exiftool.pl -csv="C:\Users\john\Desktop\TOWER_HOUSE\exterior-original\geo.csv" C:\Users\john\Desktop\TOWER_HOUSE\exterior-original
            Serializer serializer = new Serializer(Path.Combine(PhotoFolderPath, "geo.csv"));
            serializer.SerializeGeoTag(pics, sceneOffset, picsByDate);
        }
    }
}